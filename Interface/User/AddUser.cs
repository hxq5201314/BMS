using BMS.Services;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS.Interface.User
{
    public partial class AddUser : Form
    {
        private readonly AuthService _authService = new AuthService();

        /// <summary>注册成功后为 true；由 LoginInterface.Button2_Click 回调判断是否回填用户名</summary>
        public bool RegisteredSuccess { get; private set; }
        /// <summary>注册成功时写入的用户名（供 LoginInterface 回填到登录文本框）</summary>
        public string RegisteredUserName { get; private set; }

        public AddUser()
        {
            InitializeComponent();
            InitButtons();
            this.Text = "用户注册";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AcceptButton = button1;  // 回车触发注册
            this.CancelButton = button2;  // Esc 触发取消
        }

       

        /// <summary>
        /// 绑定注册/取消按钮 Click 事件（Designer.cs 未绑定，避免修改设计器文件）
        /// </summary>
        private void InitButtons()
        {
            button1.Click -= BtnRegister_Click;
            button1.Click += BtnRegister_Click;

            button2.Click -= BtnCancel_Click;
            button2.Click += BtnCancel_Click;
        }

        /// <summary>
        /// 取消：直接关闭，不记录任何状态
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            RegisteredSuccess = false;
            RegisteredUserName = null;
            this.Close();
        }

        /// <summary>
        /// 注册按钮：UI 字段校验 → 调用 RegisterUserAsync → 成功提示并 Close；失败仅提示，窗体保持以便修改
        /// </summary>
        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            // 1. 字段 Trim + 基础校验（两次密码一致性、长度、空值——长度等进一步校验在 Service 层再做一层兜底）
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

            // 2. 调用业务层注册
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
                // 用户名冲突时自动选中用户名输入框方便修改
                if (result.Message.Contains("已被占用"))
                {
                    textBox1.SelectAll();
                    textBox1.Focus();
                }
                return;
            }

            // 3. 注册成功：记录结果 → 提示 → 关闭窗体
            RegisteredSuccess = true;
            RegisteredUserName = username;
            MessageBox.Show($"注册成功！请使用用户名\"{username}\"登录。", "注册成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
