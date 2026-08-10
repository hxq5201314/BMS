using BookMS;
using BMS.Interface.User;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace BMS
{
    public partial class User1 : Form
    {
        private readonly Dao _dao = new Dao();
        private const string BookQuerySql = "SELECT BookID AS ID, ISBN AS 书码, Title AS 书名, Author AS 作者, publisher AS 来源, Total AS 库存, Remain AS 剩余 FROM books";

        public User1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体加载：加载全部图书列表到 DataGridView
        /// </summary>
        private void User1_Load(object sender, EventArgs e)
        {
            LoadBookList();
        }

        /// <summary>
        /// 查库并绑定图书列表
        /// </summary>
        private void LoadBookList()
        {
            try
            {
                DataTable dt = _dao.QueryDataTable(BookQuerySql);
                dataGridView1.DataSource = dt;
                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"加载图书列表失败: {ex.Message}", "数据库错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 图书查看事件：获取选中图书信息 → 打开 SeeBook 界面，并通过构造函数注入数据
        /// </summary>
        private void 查看ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedBook(out int bookId, out string isbn, out string title,
                                     out string author, out string publisher,
                                     out int total, out int remain))
            {
                return;
            }

            // 注入 7 个字段打开 SeeBook 详情窗体
            FormHelper.ShowModal(
                owner: this,
                createChild: () => new SeeBook(bookId, isbn, title, author, publisher, total, remain),
                onReturn: null,  // 查看只读，返回不需要刷新
                errorTitle: "打开图书详情"
            );
        }

        /// <summary>
        /// 从选中行提取全部 7 个图书字段（与 Admin2 逻辑保持一致）
        /// </summary>
        private bool TryGetSelectedBook(out int bookId, out string isbn, out string title,
                                         out string author, out string publisher,
                                         out int total, out int remain)
        {
            bookId = 0;
            isbn = title = author = publisher = null;
            total = remain = 0;

            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先在列表中选中要查看的图书");
                return false;
            }

            var row = dataGridView1.SelectedRows[0];

            string GetCellStr(string colName)
            {
                var cell = row.Cells[colName];
                return (cell.Value == null || cell.Value == DBNull.Value) ? null : cell.Value.ToString();
            }

            string idStr      = GetCellStr("ID");
            isbn              = GetCellStr("书码");
            title             = GetCellStr("书名");
            author            = GetCellStr("作者");
            publisher         = GetCellStr("来源");
            string totalStr   = GetCellStr("库存");
            string remainStr  = GetCellStr("剩余");

            if (idStr == null || !int.TryParse(idStr, out bookId))
            {
                MessageBox.Show("选中行的图书ID无效");
                return false;
            }
            if (isbn == null || title == null || author == null || publisher == null)
            {
                MessageBox.Show("选中行数据不完整，无法查看");
                return false;
            }
            if (totalStr == null || !int.TryParse(totalStr, out total))
            {
                MessageBox.Show("选中行的库存数据无效");
                return false;
            }
            if (remainStr == null || !int.TryParse(remainStr, out remain))
            {
                MessageBox.Show("选中行的剩余数据无效");
                return false;
            }
            return true;
        }
    }
}
