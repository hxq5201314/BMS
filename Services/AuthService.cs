using BookMS;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace BMS.Services
{
    /// <summary>
    /// 登录 / 注册业务逻辑
    /// </summary>
    public class AuthService
    {
        private readonly Dao _dao = new Dao();

        /// <summary>
        /// 异步登录验证
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password, string role)
        {
            try
            {
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
                throw new ServiceException("数据库未连接" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 注册普通用户（仅写入 users 表）
        /// </summary>
        public async Task<RegisterResult> RegisterUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username)) return RegisterResult.Fail("用户名不能为空");
            if (username.Length < 2 || username.Length > 50)
                return RegisterResult.Fail("用户名长度必须在 2 ~ 50 个字符之间");
            if (string.IsNullOrWhiteSpace(password)) return RegisterResult.Fail("密码不能为空");
            if (password.Length < 4 || password.Length > 50)
                return RegisterResult.Fail("密码长度必须在 4 ~ 50 个字符之间");
            string name = username;

            try
            {
                const string checkSql = "SELECT COUNT(*) FROM users WHERE username = @uname";
                DataTable checkDt = await _dao.QueryDataTableAsync(checkSql,
                    new[] { new MySqlParameter("@uname", username) });
                if (Convert.ToInt32(checkDt.Rows[0][0]) > 0)
                    return RegisterResult.Fail($"用户名\"{username}\"已被占用，请换一个");

                const string insertSql = "INSERT INTO users (username, password, name) VALUES (@uname, @pwd, @name)";
                int inserted = await _dao.ExecuteNonQueryAsync(insertSql, new[]
                {
                    new MySqlParameter("@uname", username),
                    new MySqlParameter("@pwd",   password),
                    new MySqlParameter("@name",  name)
                });

                if (inserted <= 0) return RegisterResult.Fail("注册失败：数据库未写入任何记录");
                return RegisterResult.Ok();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                    return RegisterResult.Fail($"用户名\"{username}\"已被占用，请换一个");
                throw new ServiceException("注册失败: " + ex.Message, ex);
            }
        }
    }

    /// <summary>
    /// 登录结果
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

    /// <summary>
    /// 注册操作结果
    /// </summary>
    public class RegisterResult
    {
        public bool Success { get; }
        public string Message { get; }

        private RegisterResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static RegisterResult Ok() => new RegisterResult(true, "注册成功");
        public static RegisterResult Fail(string reason) => new RegisterResult(false, reason);
    }
}
