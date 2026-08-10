using BookMS;
using System;
using System.Data;
using System.Windows.Forms;

namespace BMS.Interface.User
{
    public partial class SeeBook : Form
    {
        private readonly Dao _dao = new Dao();

        /// <summary>
        /// 无参构造：加载全部图书（用户直接打开 SeeBook 浏览全部时使用）
        /// </summary>
        public SeeBook()
        {
            InitializeComponent();
            LoadAllBooks();
        }

        /// <summary>
        /// 带参构造：注入一本图书的7个字段，在 DataGridView 中仅显示该本详情
        /// </summary>
        public SeeBook(int bookId, string isbn, string title, string author, string publisher, int total, int remain)
        {
            InitializeComponent();

            // 构建只含这一本的 DataTable，列名与 Admin2 统一（ID / 书码 / 书名 / 作者 / 来源 / 库存 / 剩余）
            DataTable dt = CreateBookSchemaTable();
            DataRow row = dt.NewRow();
            row["ID"]      = bookId;
            row["书码"]    = isbn;
            row["书名"]    = title;
            row["作者"]    = author;
            row["来源"]    = publisher;
            row["库存"]    = total;
            row["剩余"]    = remain;
            dt.Rows.Add(row);

            dataGridView1.DataSource = dt;
            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            // 只读模式，用户无法改
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
        }

        /// <summary>
        /// 查库加载全部图书（无参构造路径）
        /// </summary>
        private void LoadAllBooks()
        {
            try
            {
                string sql = "SELECT BookID AS ID, ISBN AS 书码, Title AS 书名, Author AS 作者, publisher AS 来源, Total AS 库存, Remain AS 剩余 FROM books";
                DataTable dt = _dao.QueryDataTable(sql);
                dataGridView1.DataSource = dt;
                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridView1.ReadOnly = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图书失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 构建列结构与 Admin2 完全一致的空表
        /// </summary>
        private static DataTable CreateBookSchemaTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("书码", typeof(string));
            dt.Columns.Add("书名", typeof(string));
            dt.Columns.Add("作者", typeof(string));
            dt.Columns.Add("来源", typeof(string));
            dt.Columns.Add("库存", typeof(int));
            dt.Columns.Add("剩余", typeof(int));
            return dt;
        }
    }
}
