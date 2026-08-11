using BookMS;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace BMS.Services
{
    /// <summary>
    /// 登录业务层：封装用户/管理员登录验证（异步），UI 层不接触 SQL
    /// </summary>
    public class AuthService
    {
        private readonly Dao _dao = new Dao();

        /// <summary>
        /// 异步登录验证。成功返回 true 并输出 userId / userName；失败返回 false。
        /// 仅在数据库异常时抛 ServiceException
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password, string role)
        {
            try
            {
                // 表名由 role 决定（仅 user/admin 二选一，非外部自由输入）
                string table = (role == "user") ? "users" : "admins";
                string sql = $"SELECT id, name FROM {table} WHERE username = @username AND password = @password";
                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@password", password)
                };

                var dt = await _dao.QueryDataTableAsync(sql, parameters);
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new LoginResult(
                        success: true,
                        userId: row["id"]?.ToString() ?? "",
                        userName: row["name"]?.ToString() ?? ""
                    );
                }
                return LoginResult.Fail;
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("登录验证失败: " + ex.Message, ex);
            }
        }
    }

    /// <summary>
    /// 登录结果：避免用 out 参数返回，使异步调用更整洁
    /// </summary>
    public class LoginResult
    {
        public static readonly LoginResult Fail = new LoginResult(false, "", "");

        public bool Success { get; }
        public string UserId { get; }
        public string UserName { get; }

        public LoginResult(bool success, string userId, string userName)
        {
            Success = success;
            UserId = userId;
            UserName = userName;
        }
    }
}
