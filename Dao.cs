using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace BookMS
{
    /// <summary>
    /// 数据访问层：统一封装所有数据库操作，避免重复代码
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
    }
}