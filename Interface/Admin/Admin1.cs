using BMS.Interface;
using System;
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

        /// <summary>
        /// 将欢迎标签在窗体中水平、垂直居中（消除重复代码）
        /// </summary>
        private void CenterWelcomeLabel()
        {
            label1.Left = (ClientSize.Width - label1.Width) / 2;
            label1.Top = (ClientSize.Height - label1.Height) / 2;
        }

        private void 图书管理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 使用 FormHelper 统一处理窗体切换，消除重复代码
            FormHelper.ShowModal(
                owner: this,
                createChild: () => new Admin2(),
                errorTitle: "打开图书管理界面"
            );
        }
    }
}
