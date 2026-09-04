using BMS.Interface;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS
{
    public partial class Admin1 : Form
    {
        public Admin1()
        {
            InitializeComponent();
            label1.Text = "欢迎管理员" + Data.UName;
        }

        private void Admin1_Load(object sender, EventArgs e)
        {
            CenterWelcomeLabel();
        }

        private void Admin1_Resize(object sender, EventArgs e)
        {
            CenterWelcomeLabel();
        }

        private void CenterWelcomeLabel()
        {
            label1.Left = (ClientSize.Width - label1.Width) / 2;
            label1.Top = (ClientSize.Height - label1.Height) / 2;
        }

        private async void 图书管理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await FormHelper.ShowModal(
                owner: this,
                createChild: () => new Admin2(),
                errorTitle: "打开图书管理界面"
            );
        }

        private void 退出登录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "确认退出当前登录账号，返回登录界面？",
                "退出登录",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            Data.UID = null;
            Data.UName = null;
            Data.Role = null;

            this.Close();
        }
    }
}
