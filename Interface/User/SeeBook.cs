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

        /// <summary>
        /// 无参构造：异步加载全部图书
        /// </summary>
        public SeeBook()
        {
            InitializeComponent();
            this.Load += SeeBook_Load;
        }

        /// <summary>
        /// Load 事件中异步加载数据（构造函数中不建议 await）
        /// </summary>
        private async void SeeBook_Load(object sender, EventArgs e)
        {
            // 无参构造 → 加载全部；带参构造 → DataSource 已在构造中设置
            if (dataGridView1.DataSource == null)
            {
                await LoadAllBooksAsync();
            }
        }

        /// <summary>
        /// 带参构造：注入一本图书实体，在 DataGridView 中仅显示该本详情（只读）
        /// </summary>
        public SeeBook(Book book)
        {
            InitializeComponent();
            this.Load += SeeBook_Load;
            BookGridBinder.Bind(dataGridView1, new List<Book> { book }, readOnly: true);
        }

        /// <summary>
        /// 通过业务层异步加载全部图书（只读模式）
        /// </summary>
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
