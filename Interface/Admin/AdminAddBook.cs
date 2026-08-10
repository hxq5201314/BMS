using BookMS;
using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace BMS.Interface
{
    public partial class AdminAddBook : Form
    {
        private readonly Dao _dao = new Dao();

        public AdminAddBook()
        {
            InitializeComponent();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // 基本输入校验
            if (string.IsNullOrWhiteSpace(textBox1.Text) ||
                string.IsNullOrWhiteSpace(textBox2.Text) ||
                string.IsNullOrWhiteSpace(textBox3.Text) ||
                string.IsNullOrWhiteSpace(textBox4.Text) ||
                string.IsNullOrWhiteSpace(textBox5.Text) ||
                string.IsNullOrWhiteSpace(textBox6.Text) ||
                string.IsNullOrWhiteSpace(textBox7.Text))
            {
                MessageBox.Show("请完整填写所有信息");
                return;
            }
            if (!int.TryParse(textBox7.Text, out int bookId) || bookId <= 0)
            {
                MessageBox.Show("ID不能为0以下的数");
                return;
            }
            if (!int.TryParse(textBox5.Text, out int total) || total < 0)
            {
                MessageBox.Show("库存数量必须为非负整数");
                return;
            }
            if (!int.TryParse(textBox6.Text, out int remain) || remain < 0)
            {
                MessageBox.Show("剩余数量必须为非负整数");
                return;
            }
            if (remain > total)
            {
                MessageBox.Show("剩余数量不能大于总库存");
                return;
            }

            try
            {
                string sql = @"INSERT INTO books (BookID, ISBN, Title, Author, publisher, Total, Remain)
                               VALUES (@BookID, @ISBN, @Title, @Author, @publisher, @Total, @Remain)";

                var parameters = new[]
                {
                    new MySqlParameter("@BookID", bookId),
                    new MySqlParameter("@ISBN", textBox1.Text),
                    new MySqlParameter("@Title", textBox2.Text),
                    new MySqlParameter("@Author", textBox3.Text),
                    new MySqlParameter("@Publisher", textBox4.Text),
                    new MySqlParameter("@Total", total),
                    new MySqlParameter("@Remain", remain)
                };

                int n = _dao.ExecuteNonQuery(sql, parameters);
                if (n > 0)
                {
                    MessageBox.Show("添加成功");
                    // 添加成功后关闭本窗体，返回 Admin2 并自动刷新列表
                    this.Close();
                }
                else
                {
                    MessageBox.Show("添加失败，请检查数据");
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"数据库错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 清空所有文本框
        /// </summary>
        private void ClearTextBoxes()
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is TextBox txt)
                    txt.Text = "";
            }
        }

        /// <summary>
        /// 取消按钮：关闭窗体返回上一页（模态窗体必须用 Close，不能用 Hide）
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            // 对于 ShowDialog 打开的模态窗体，Close() 会让 ShowDialog 正常返回，
            // 随后 FormHelper 的 finally 会 Dispose 窗体并执行回调刷新列表
            this.Close();
        }
    }
}
