using BookMS;
using BMS.Interface.Admin;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace BMS.Interface
{
    public partial class Admin2 : Form
    {
        // 图书查询 SQL 常量（列别名必须与下面取单元格的列名一致）
        private const string BookQuerySql = "SELECT BookID AS ID, ISBN AS 书码, Title AS 书名, Author AS 作者, publisher AS 来源, Total AS 库存, Remain AS 剩余  FROM books";

        private readonly Dao _dao = new Dao();

        // 查询状态：记住上次关键词、匹配行索引列表、当前定位游标
        private readonly List<int> _matchedRowIndices = new List<int>();
        private string _lastKeyword = null;
        private int _currentMatchCursor = -1;

        public Admin2()
        {
            InitializeComponent();
        }

        private void Admin2_Load(object sender, EventArgs e)
        {
            RefreshBookList();
        }

        /// <summary>
        /// 重新从数据库加载图书列表并绑定到 DataGridView
        /// </summary>
        private void RefreshBookList()
        {
            try
            {
                DataTable dt = _dao.QueryDataTable(BookQuerySql);
                dataGridView1.DataSource = dt;
                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                // 刷新数据后必须重置查询状态，否则匹配的行索引/高亮会错位
                ResetMatchState();
                label2.Text = "";
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
        /// 添加图书按钮事件
        /// </summary>
        private void Button1_Click(object sender, EventArgs e)
        {
            FormHelper.ShowModal(
                owner: this,
                createChild: () => new AdminAddBook(),
                onReturn: RefreshBookList,
                errorTitle: "打开添加图书界面"
            );
        }

        /// <summary>
        /// 删除图书按钮事件
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确认删除？", "信息提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            if (!TryGetSelectedBookId(out int bookId)) return;

            try
            {
                string sql = "DELETE FROM books WHERE BookID = @ID";
                var parameters = new[] { new MySqlParameter("@ID", bookId) };
                int n = _dao.ExecuteNonQuery(sql, parameters);

                if (n > 0)
                {
                    MessageBox.Show("删除成功");
                    RefreshBookList();
                }
                else
                {
                    MessageBox.Show("删除失败");
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
        /// 修改图书点击事件：获取选中图书的7个字段 → 传入 EditBook 构造函数 → 打开窗体 → 返回后刷新
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            // 从选中行提取所有字段（带完整校验）
            if (!TryGetSelectedBook(out int bookId, out string isbn, out string title,
                                     out string author, out string publisher,
                                     out int total, out int remain))
            {
                return;
            }

            // 使用 FormHelper 打开修改窗体，返回后自动刷新列表
            FormHelper.ShowModal(
                owner: this,
                createChild: () => new EditBook(bookId, isbn, title, author, publisher, total, remain),
                onReturn: RefreshBookList,
                errorTitle: "打开修改图书界面"
            );
        }

        /// <summary>
        /// 点击单元格事件：更新底部选中行信息
        /// </summary>
        private void dataGridView1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0) return;

            var row = dataGridView1.SelectedRows[0];
            var idCell = row.Cells[0];
            var nameCell = row.Cells[2];

            if (idCell.Value == null || nameCell.Value == null) return;

            label2.Text =idCell.Value + "  书名：" + nameCell.Value;
        }

        #region 辅助方法：抽取选中行校验，消除重复代码

        /// <summary>
        /// 尝试获取选中行的 BookID（删除用）
        /// </summary>
        private bool TryGetSelectedBookId(out int bookId)
        {
            bookId = 0;
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选中要操作的行");
                return false;
            }
            var idCell = dataGridView1.SelectedRows[0].Cells["ID"];
            if (idCell.Value == null || idCell.Value == DBNull.Value)
            {
                MessageBox.Show("选中行数据无效");
                return false;
            }
            bookId = Convert.ToInt32(idCell.Value);
            return true;
        }

        /// <summary>
        /// 尝试获取选中行的全部7个图书字段（修改用）
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
                MessageBox.Show("请先选中要修改的图书");
                return false;
            }

            var row = dataGridView1.SelectedRows[0];

            // 辅助函数：取指定列的值并转 string，DBNull 视为 null
            string GetCellStr(string colName)
            {
                var cell = row.Cells[colName];
                return (cell.Value == null || cell.Value == DBNull.Value) ? null : cell.Value.ToString();
            }

            // 按 SQL 别名取列：ID / 书码 / 书名 / 作者 / 来源 / 库存 / 剩余
            string idStr      = GetCellStr("ID");
            isbn              = GetCellStr("书码");
            title             = GetCellStr("书名");
            author            = GetCellStr("作者");
            publisher         = GetCellStr("来源");
            string totalStr   = GetCellStr("库存");
            string remainStr  = GetCellStr("剩余");

            // 校验关键字段
            if (idStr == null || !int.TryParse(idStr, out bookId))
            {
                MessageBox.Show("选中行的图书ID无效");
                return false;
            }
            if (isbn == null || title == null || author == null || publisher == null)
            {
                MessageBox.Show("选中行数据不完整，无法修改");
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

        #endregion

        /// <summary>
        /// 查询点击事件：关键词变 → 扫描匹配行 + 高亮；再点 → 依次跳到下一条匹配行并置顶（类似 Ctrl+F 查找下一个）
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            string keyword = textBox1.Text?.Trim();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                MessageBox.Show("请输入查询关键词");
                ResetMatchState();
                return;
            }

            // 关键词变化 → 重新扫描 DataGridView 收集所有匹配行索引
            if (keyword != _lastKeyword)
            {
                _matchedRowIndices.Clear();
                _lastKeyword = keyword;
                _currentMatchCursor = -1;

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    DataGridViewRow row = dataGridView1.Rows[i];
                    if (row.IsNewRow) continue;
                    if (RowContainsKeyword(row, keyword))
                    {
                        _matchedRowIndices.Add(i);
                    }
                }

                // 先清除旧高亮，再给新匹配行涂色
                ClearMatchHighlight();
                if (_matchedRowIndices.Count == 0)
                {
                    MessageBox.Show($"未找到包含 \"{keyword}\" 的图书");
                    label2.Text = "";
                    return;
                }
                foreach (int idx in _matchedRowIndices)
                {
                    dataGridView1.Rows[idx].DefaultCellStyle.BackColor = Color.Bisque;
                }
            }

            // 依次跳到下一条匹配行（循环）→ 游标 +1，越界回到 0
            _currentMatchCursor = (_currentMatchCursor + 1) % _matchedRowIndices.Count;
            int targetRowIndex = _matchedRowIndices[_currentMatchCursor];

            // 选中该行 + 滚动到 DataGridView 顶部（真正的"置顶"）
            dataGridView1.ClearSelection();
            dataGridView1.Rows[targetRowIndex].Selected = true;
            if (targetRowIndex < dataGridView1.Rows.Count)
                dataGridView1.CurrentCell = dataGridView1.Rows[targetRowIndex].Cells[0];
            if (dataGridView1.FirstDisplayedScrollingRowIndex != targetRowIndex)
                dataGridView1.FirstDisplayedScrollingRowIndex = targetRowIndex;

          
        }

        #region 查询辅助方法（高亮 + 状态重置 + 行内容匹配）

        /// <summary>
        /// 扫描单行所有单元格是否包含关键词（忽略大小写）
        /// </summary>
        private bool RowContainsKeyword(DataGridViewRow row, string keyword)
        {
            StringComparison comp = StringComparison.OrdinalIgnoreCase;
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.Value == null || cell.Value == DBNull.Value) continue;
                if (cell.Value.ToString().IndexOf(keyword, comp) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 清除所有行的匹配高亮（刷表或换关键词时调用）
        /// </summary>
        private void ClearMatchHighlight()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                row.DefaultCellStyle.BackColor = Color.Empty;
                row.DefaultCellStyle.SelectionBackColor = Color.Empty;
            }
        }

        /// <summary>
        /// 重置整个查询状态（数据刷新或清空关键词时调用，防止行索引错位）
        /// </summary>
        private void ResetMatchState()
        {
            _matchedRowIndices.Clear();
            _lastKeyword = null;
            _currentMatchCursor = -1;
            ClearMatchHighlight();
        }

        #endregion
    }
}
