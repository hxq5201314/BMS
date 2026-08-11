using BMS.Models;
using BMS.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS.Interface
{
    public partial class AdminAddBook : Form
    {
        private readonly BookService _bookService = new BookService();

        public AdminAddBook()
        {
            InitializeComponent();
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
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

            // 由 UI 输入构建实体，业务层负责落库
            var book = new Book
            {
                BookID = bookId,
                ISBN = textBox1.Text,
                Title = textBox2.Text,
                Author = textBox3.Text,
                Publisher = textBox4.Text,
                Total = total,
                Remain = remain
            };

            try
            {
                int n = await _bookService.AddBookAsync(book);
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
        /// 取消按钮：关闭窗体返回上一页
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
