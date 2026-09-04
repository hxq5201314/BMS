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
            this.Load += AdminAddBook_Load;
            textBox7.ReadOnly = true;
            textBox7.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
        }

        private async void AdminAddBook_Load(object sender, EventArgs e)
        {
            try
            {
                int nextId = await _bookService.GetNextBookIdAsync();
                if (!this.IsDisposed && !textBox7.IsDisposed)
                    textBox7.Text = nextId.ToString();
            }
            catch (ServiceException ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + "请手动填写有效的图书ID",
                    "获取下一个ID失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
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
                bool exists = await _bookService.ExistsBookIdAsync(bookId);
                if (exists)
                {
                    int nextId = await _bookService.GetNextBookIdAsync();
                    MessageBox.Show(
                        $"ID「{bookId}」已被占用，请使用系统推荐的下一个ID：{nextId}" +
                        Environment.NewLine + "（系统会在每次打开添加图书窗口时自动填充最新可用ID）",
                        "ID重复",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (ServiceException ex)
            {
                MessageBox.Show(ex.Message, "检查ID失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

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

        private void Button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
