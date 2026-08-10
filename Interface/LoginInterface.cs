using BookMS;
using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace BMS
{
    public partial class LoginInterface : Form
    {
        private readonly Dao _dao = new Dao();

        public LoginInterface()
        {
            InitializeComponent();
            this.AcceptButton = button1;  // button1 登录按钮
        }

        /// <summary>
        /// 登录验证：通过 Dao 统一访问数据库（不再自建连接）
        /// </summary>
        public bool Login(string username, string password, string role, out string userId, out string userName)
        {
            userId = "";
            userName = "";
            try
            {
                string table = (role == "user") ? "users" : "admins";
                string sql = $"SELECT id, name FROM {table} WHERE username = @username AND password = @password";

                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@password", password)
                };

                using (var reader = _dao.QueryReader(sql, parameters))
                {
                    if (reader.Read())
                    {
                        userId = reader["id"]?.ToString() ?? "";
                        userName = reader["name"]?.ToString() ?? "";
                        return true;
                    }
                    return false;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"数据库错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码");
                return;
            }

            string role = radioButtonUser.Checked ? "user" : "admin";
            bool success = Login(username, password, role, out string uid, out string uname);

            if (success)
            {
                Data.UID = uid;
                Data.UName = uname;
                Data.Role = role;

                // 使用 FormHelper 统一处理窗体切换，消除重复代码
                FormHelper.ShowModal(
                    owner: this,
                    createChild: () => role == "admin" ? (Form)new Admin1() : new User1(),
                    errorTitle: "打开主界面"
                );
            }
            else
            {
                MessageBox.Show("用户名或密码错误");
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
        }
    }
}
