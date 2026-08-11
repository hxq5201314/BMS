using BMS.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS
{
    public partial class LoginInterface : Form
    {
        private readonly AuthService _authService = new AuthService();

        public LoginInterface()
        {
            InitializeComponent();
            this.AcceptButton = button1;  // button1 登录按钮
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码");
                return;
            }

            string role = radioButtonUser.Checked ? "user" : "admin";
            LoginResult result;
            try
            {
                result = await _authService.LoginAsync(username, password, role);
            }
            catch (ServiceException ex)
            {
                MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (result.Success)
            {
                Data.UID = result.UserId;
                Data.UName = result.UserName;
                Data.Role = role;

                // 使用 FormHelper 统一处理窗体切换
                await FormHelper.ShowModal(
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
