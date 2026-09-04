using BMS.Interface.User;
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
            this.AcceptButton = button1;
            this.Text = "图书管理系统 - 登录";
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

        private async void Button2_Click(object sender, EventArgs e)
        {
            AddUser addUserForm = null;
            await FormHelper.ShowModal(
                owner: this,
                createChild: () => { addUserForm = new AddUser(); return addUserForm; },
                onReturn: () =>
                {
                    if (addUserForm != null && addUserForm.RegisteredSuccess && !string.IsNullOrEmpty(addUserForm.RegisteredUserName))
                    {
                        textBox1.Text = addUserForm.RegisteredUserName;
                        radioButtonUser.Checked = true;
                        textBox2.SelectAll();
                        textBox2.Focus();
                    }
                },
                errorTitle: "打开注册界面"
            );
        }
    }
}
