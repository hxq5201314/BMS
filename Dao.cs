using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BookMS
{
    /// <summary>
    /// 数据访问层：统一封装数据库操作
    /// </summary>
    class Dao
    {
        private const string DefaultConnectionString =
            "server=localhost;port=3306;database=BookDB;user id=root;password=1111;";

        private static readonly Lazy<string> _connectionString = new Lazy<string>(() =>
        {
            try
            {
                ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["BookDb"];
                if (setting != null && !string.IsNullOrWhiteSpace(setting.ConnectionString))
                    return setting.ConnectionString;
            }
            catch (ConfigurationErrorsException)
            {
                MessageBox.Show("数据库未连接，请连接数据库后尝试。");
            }
            return DefaultConnectionString;
        });

        private static string ConnectionString => _connectionString.Value;

        #region 同步方法

        /// <summary>
        /// 执行增删改，返回受影响行数
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

        #region 异步方法

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
        /// 异步查询返回 DataTable
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
        /// 在事务中执行多个操作，全部成功则 Commit，异常则 Rollback
        /// </summary>
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
                    try { tran.Rollback(); } catch { }
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
