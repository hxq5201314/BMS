using BookMS;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace BMS.Services
{
    /// <summary>
    /// 登录 / 注册业务层：封装用户/管理员登录验证（异步）和普通用户注册，UI 层不接触 SQL
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
                throw new ServiceException("数据库未连接" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 注册普通用户（仅写入 users 表，不包含管理员注册）。
        /// 步骤：输入快速校验 → 用户名查重 → INSERT 新记录（显示名 name 默认取用户名）
        /// </summary>
        public async Task<RegisterResult> RegisterUserAsync(string username, string password)
        {
            // 基本输入校验（在数据库外快速失败，避免占用连接）
            if (string.IsNullOrWhiteSpace(username)) return RegisterResult.Fail("用户名不能为空");
            if (username.Length < 2 || username.Length > 50)
                return RegisterResult.Fail("用户名长度必须在 2 ~ 50 个字符之间");
            if (string.IsNullOrWhiteSpace(password)) return RegisterResult.Fail("密码不能为空");
            if (password.Length < 4 || password.Length > 50)
                return RegisterResult.Fail("密码长度必须在 4 ~ 50 个字符之间");
            string name = username; // 显示名默认等于用户名

            try
            {
                // 1. 用户名查重：users 表唯一约束前先查，给出友好提示（有 UNIQUE 索引也查一次，避免抛 MySqlException 23000 才提示）
                const string checkSql = "SELECT COUNT(*) FROM users WHERE username = @uname";
                DataTable checkDt = await _dao.QueryDataTableAsync(checkSql,
                    new[] { new MySqlParameter("@uname", username) });
                if (Convert.ToInt32(checkDt.Rows[0][0]) > 0)
                    return RegisterResult.Fail($"用户名\"{username}\"已被占用，请换一个");

                // 2. 插入新用户
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
                // 唯一索引冲突（MySQL 错误码 1062 = DUPLICATE_ENTRY）时给友好提示
                if (ex.Number == 1062)
                    return RegisterResult.Fail($"用户名\"{username}\"已被占用，请换一个");
                throw new ServiceException("注册失败: " + ex.Message, ex);
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

    /// <summary>
    /// 注册操作结果：成功时 Success=true，失败时携带 Message
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
