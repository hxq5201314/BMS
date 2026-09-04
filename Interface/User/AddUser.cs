using BMS.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS.Interface.User
{
    public partial class AddUser : Form
    {
        private readonly AuthService _authService = new AuthService();

        public bool RegisteredSuccess { get; private set; }
        public string RegisteredUserName { get; private set; }

        public AddUser()
        {
            InitializeComponent();
            InitButtons();
            this.Text = "用户注册";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AcceptButton = button1;
            this.CancelButton = button2;
        }

        private void InitButtons()
        {
            button1.Click -= BtnRegister_Click;
            button1.Click += BtnRegister_Click;

            button2.Click -= BtnCancel_Click;
            button2.Click += BtnCancel_Click;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            RegisteredSuccess = false;
            RegisteredUserName = null;
            this.Close();
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text?.Trim() ?? "";
            string password = textBox2.Text ?? "";
            string confirm  = textBox3.Text ?? "";

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("请输入用户名", "注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入密码", "注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                return;
            }
            if (string.IsNullOrEmpty(confirm))
            {
                MessageBox.Show("请再次输入密码", "注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.Focus();
                return;
            }
            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                MessageBox.Show("两次输入的密码不一致", "注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.SelectAll();
                textBox3.Focus();
                return;
            }

            RegisterResult result;
            try
            {
                result = await _authService.RegisterUserAsync(username, password);
            }
            catch (ServiceException ex)
            {
                MessageBox.Show(ex.Message, "注册失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!result.Success)
            {
                MessageBox.Show(result.Message, "注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result.Message.Contains("已被占用"))
                {
                    textBox1.SelectAll();
                    textBox1.Focus();
                }
                return;
            }

            RegisteredSuccess = true;
            RegisteredUserName = username;
            MessageBox.Show($"注册成功！请使用用户名\"{username}\"登录。", "注册成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
