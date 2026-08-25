using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BookMS
{
    /// <summary>
    /// 数据访问层：统一封装所有数据库操作，避免重复代码。
    /// 提供同步 + 异步两套 API，UI 层优先使用异步方法避免阻塞 UI 线程。
    /// ────────────────────────────────────────────────────────────────────
    /// 连接字符串来源：
    ///   优先从 App.config 的 <connectionStrings> 节点读取（name = "BookDb"）。
    ///   这样部署时只需改 .config，无需重新编译。
    ///   如果 .config 没有配置，会回退到 DefaultConnectionString（仅作兜底，不推荐）。
    /// </summary>
    class Dao
    {
        /// <summary>
        /// 仅当 App.config 忘记配置时使用，避免程序直接崩。
        /// 生产环境务必在 App.config 中配置，不要依赖这个默认值。
        /// </summary>
        
        private const string DefaultConnectionString =
            "server=localhost;port=3306;database=BookDB;user id=root;password=1111;";

        /// <summary>
        /// 从 App.config 读取连接字符串（<connectionStrings name="BookDb">）。
        /// 读取失败则回退到 DefaultConnectionString，并通过 Debug 打印提示。
        /// 为什么用 Lazy<T>：静态字段延迟到第一次访问才初始化，
        /// 避免静态构造阶段抛异常导致整个类型加载失败（难调试）。
        /// </summary>
        private static readonly Lazy<string> _connectionString = new Lazy<string>(() =>
        {
            try
            {
                // System.Configuration.ConfigurationManager 需要在项目中引用
                // System.Configuration 程序集（本项目 BMS.csproj 已引用）
                ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["BookDb"];

                if (setting != null && !string.IsNullOrWhiteSpace(setting.ConnectionString))
                    return setting.ConnectionString;
            }
            catch (ConfigurationErrorsException)
            {
                // App.config 格式错误时，ConfigurationManager 会抛这个异常
                // 这里不抛出，让兜底连接串生效，至少能看到报错而非直接崩溃
                MessageBox.Show("数据库未连接，请连接数据库后尝试。");
            }

            // 走到这里说明 .config 没配好
            return DefaultConnectionString;
        });

        /// <summary>
        /// 当前实际使用的数据库连接字符串（只读属性，对外暴露读取路径）
        /// </summary>
        private static string ConnectionString => _connectionString.Value;

        #region 同步方法

        /// <summary>
        /// 执行增删改
        /// </summary>
        public int ExecuteNonQuery(string sql, MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                conn.Open();
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 执行查询并返回 DataTable
        /// </summary>
        public DataTable QueryDataTable(string sql, MySqlParameter[] parameters = null)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                using (var adapter = new MySqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        /// <summary>
        /// 执行查询并返回 DataReader（读取完自动关闭连接）
        /// </summary>
        public MySqlDataReader QueryReader(string sql, MySqlParameter[] parameters = null)
        {
            var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            var cmd = new MySqlCommand(sql, conn);
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        #endregion

        #region 异步方法（UI 层推荐使用，不阻塞 UI 线程）

        /// <summary>
        /// 异步执行增删改
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string sql, MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// 异步执行查询并返回 DataTable
        /// </summary>
        public async Task<DataTable> QueryDataTableAsync(string sql, MySqlParameter[] parameters = null)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        /// <summary>
        /// 在一个数据库连接 + 事务中顺序执行多个写入操作。
        /// 全部成功则 Commit；worker 抛任何异常则 Rollback 并原样抛出。
        /// 用于需要跨多条 SQL 保持一致性的场景（借阅/归还：UPDATE books + INSERT/UPDATE borrow_records）
        /// </summary>
        /// <param name="worker">
        /// 使用者在委托内拿到已打开的 conn 和已开启的 tran，用它们构造 MySqlCommand(sql, conn, tran)
        /// 并调用 ExecuteNonQueryAsync / ExecuteReaderAsync。委托的返回值即本方法的返回值。
        /// </param>
        public async Task<T> RunInTransactionAsync<T>(Func<MySqlConnection, MySqlTransaction, Task<T>> worker)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));

            using (var conn = new MySqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                var tran = conn.BeginTransaction();
                try
                {
                    T result = await worker(conn, tran);
                    tran.Commit();
                    return result;
                }
                catch
                {
                    // 回滚本身失败时不要吞掉原始异常（上层要知道业务操作失败）
                    try { tran.Rollback(); } catch { /* best-effort */ }
                    throw;
                }
                finally
                {
                    tran.Dispose();
                }
            }
        }

        #endregion
    }
}
