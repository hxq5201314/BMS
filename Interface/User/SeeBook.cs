using BMS.Models;
using BMS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS.Interface.User
{
    public partial class SeeBook : Form
    {
        private readonly BookService _bookService = new BookService();

        public SeeBook()
        {
            InitializeComponent();
            this.Load += SeeBook_Load;
        }

        private async void SeeBook_Load(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource == null)
            {
                await LoadAllBooksAsync();
            }
        }

        public SeeBook(Book book)
        {
            InitializeComponent();
            this.Load += SeeBook_Load;
            BookGridBinder.Bind(dataGridView1, new List<Book> { book }, readOnly: true);
        }

        private async Task LoadAllBooksAsync()
        {
            try
            {
                List<Book> books = await _bookService.GetAllBooksAsync();
                BookGridBinder.Bind(dataGridView1, books, readOnly: true);
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
    }
}
