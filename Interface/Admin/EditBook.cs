using BookMS;
using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace BMS.Interface.Admin
{
    public partial class EditBook : Form
    {
        private readonly Dao _dao = new Dao();
        private readonly int _originalBookId;  // 原始主键（用于 WHERE，防止修改到其他行）

        /// <summary>
        /// 构造函数：接收选中图书的7个字段，填充到对应 TextBox
        /// </summary>
        public EditBook(int bookId, string isbn, string title, string author, string publisher, int total, int remain)
        {
            InitializeComponent();

            _originalBookId = bookId;

            // 填充数据到控件（用户说的"把数据传递到labelbox中"，实际是 TextBox 以便编辑）
            textBox7.Text = bookId.ToString();
            textBox1.Text = isbn;
            textBox2.Text = title;
            textBox3.Text = author;
            textBox4.Text = publisher;
            textBox5.Text = total.ToString();
            textBox6.Text = remain.ToString();

            // ID 是主键，不允许修改（设为只读 + 灰底提示用户）
            textBox7.ReadOnly = true;
            textBox7.BackColor = System.Drawing.Color.LightGray;

        }

        /// <summary>
        /// 修改图书按钮：校验 → UPDATE 数据库 → 关闭
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 输入校验（和 AdminAddBook 保持一致）
            if (string.IsNullOrWhiteSpace(textBox1.Text) ||
                string.IsNullOrWhiteSpace(textBox2.Text) ||
                string.IsNullOrWhiteSpace(textBox3.Text) ||
                string.IsNullOrWhiteSpace(textBox4.Text) ||
                string.IsNullOrWhiteSpace(textBox5.Text) ||
                string.IsNullOrWhiteSpace(textBox6.Text))
            {
                MessageBox.Show("请完整填写所有信息");
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
                // UPDATE 时 WHERE 用原始 _originalBookId，避免用户手动尝试改 ID 文本框造成误更新
                string sql = @"UPDATE books 
                               SET ISBN = @ISBN, Title = @Title, Author = @Author, 
                                   publisher = @publisher, Total = @Total, Remain = @Remain
                               WHERE BookID = @OriginalBookID";

                var parameters = new[]
                {
                    new MySqlParameter("@ISBN", textBox1.Text.Trim()),
                    new MySqlParameter("@Title", textBox2.Text.Trim()),
                    new MySqlParameter("@Author", textBox3.Text.Trim()),
                    new MySqlParameter("@publisher", textBox4.Text.Trim()),
                    new MySqlParameter("@Total", total),
                    new MySqlParameter("@Remain", remain),
                    new MySqlParameter("@OriginalBookID", _originalBookId)
                };

                int n = _dao.ExecuteNonQuery(sql, parameters);
                if (n > 0)
                {
                    MessageBox.Show("修改成功");
                    this.Close();  // 修改成功后关闭窗体，返回 Admin2 并自动刷新列表
                }
                else
                {
                    MessageBox.Show("数据未变更");
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
        /// 取消按钮：直接关闭窗体
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
