using BMS.Models;
using BMS.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS.Interface.Admin
{
    public partial class EditBook : Form
    {
        private readonly BookService _bookService = new BookService();
        private readonly Book _book;  // 原始图书（用其 BookID 作为 UPDATE 的 WHERE，防止误改主键）

        /// <summary>
        /// 构造函数：接收选中图书实体，填充到对应 TextBox
        /// </summary>
        public EditBook(Book book)
        {
            InitializeComponent();
            _book = book;

            // 填充数据到控件
            textBox7.Text = book.BookID.ToString();
            textBox1.Text = book.ISBN;
            textBox2.Text = book.Title;
            textBox3.Text = book.Author;
            textBox4.Text = book.Publisher;
            textBox5.Text = book.Total.ToString();
            textBox6.Text = book.Remain.ToString();

            // ID 是主键，不允许修改
            textBox7.ReadOnly = true;
            textBox7.BackColor = System.Drawing.Color.LightGray;
        }

        /// <summary>
        /// 修改图书按钮：校验 → 用文本框当前值构建更新实体 → 异步 UPDATE → 关闭
        /// </summary>
        private async void BtnSave_Click(object sender, EventArgs e)
        {
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

            // 用文本框当前值构建更新实体，主键用原始 _book.BookID
            var updated = new Book
            {
                BookID = _book.BookID,
                ISBN = textBox1.Text.Trim(),
                Title = textBox2.Text.Trim(),
                Author = textBox3.Text.Trim(),
                Publisher = textBox4.Text.Trim(),
                Total = total,
                Remain = remain
            };

            try
            {
                int n = await _bookService.UpdateBookAsync(updated);
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
            catch (ServiceException ex)
            {
                MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
