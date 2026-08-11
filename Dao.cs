using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace BookMS
{
    /// <summary>
    /// 数据访问层：统一封装所有数据库操作，避免重复代码。
    /// 提供同步 + 异步两套 API，UI 层优先使用异步方法避免阻塞 UI 线程。
    /// </summary>
    class Dao
    {
        // 唯一的连接字符串定义点（全项目仅此一处）
        private static readonly string ConnectionString =
            "server=localhost;port=3306;database=BookDB;user=root;password=1111;";

        /// <summary>
        /// 执行增删改（无参数，已弃用：优先使用带参数的 ExecuteNonQuery）
        /// </summary>
        [Obsolete("请使用带 MySqlParameter[] 参数的 ExecuteNonQuery 以防止 SQL 注入")]
        public int Execute(string sql)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

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
