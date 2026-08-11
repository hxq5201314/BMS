using BMS.Interface.Admin;
using BMS.Models;
using BMS.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS.Interface
{
    public partial class Admin2 : Form
    {
        private readonly BookService _bookService = new BookService();

        // 查询状态：记住上次关键词、匹配行索引列表、当前定位游标
        private readonly List<int> _matchedRowIndices = new List<int>();
        private string _lastKeyword = null;
        private int _currentMatchCursor = -1;

        public Admin2()
        {
            InitializeComponent();
        }

        private async void Admin2_Load(object sender, EventArgs e)
        {
            await RefreshBookListAsync();
        }

        /// <summary>
        /// 异步重新从数据库加载图书列表并绑定到 DataGridView
        /// </summary>
        private async Task RefreshBookListAsync()
        {
            // 防御：如果窗体或核心控件已释放（如嵌套 ShowDialog 期间父窗体被关），
            // 直接返回不抛 ObjectDisposedException
            if (this.IsDisposed || dataGridView1.IsDisposed || label2.IsDisposed) return;

            try
            {
                List<Book> books = await _bookService.GetAllBooksAsync();
                if (this.IsDisposed || dataGridView1.IsDisposed) return; // await 期间仍可能被 Dispose
                BookGridBinder.Bind(dataGridView1, books);
                // 刷新数据后必须重置查询状态，否则匹配的行索引/高亮会错位
                ResetMatchState();
                if (!label2.IsDisposed) label2.Text = "";
            }
            catch (ServiceException ex)
            {
                if (!this.IsDisposed)
                    MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed)
                    MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 添加图书按钮事件：使用异步回调刷新
        /// </summary>
        private async void Button1_Click(object sender, EventArgs e)
        {
            await FormHelper.ShowModal(
                owner: this,
                createChild: () => new AdminAddBook(),
                onReturnAsync: RefreshBookListAsync,
                errorTitle: "打开添加图书界面"
            );
        }

        /// <summary>
        /// 删除图书按钮事件
        /// </summary>
        private async void button2_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确认删除？", "信息提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            if (!TryGetSelectedBook(out Book book)) return;

            try
            {
                int n = await _bookService.DeleteBookAsync(book.BookID);
                if (n > 0)
                {
                    MessageBox.Show("删除成功");
                    await RefreshBookListAsync();
                }
                else
                {
                    MessageBox.Show("删除失败");
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
        /// 修改图书点击事件：异步回调刷新
        /// </summary>
        private async void button3_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedBook(out Book book)) return;

            await FormHelper.ShowModal(
                owner: this,
                createChild: () => new EditBook(book),
                onReturnAsync: RefreshBookListAsync,
                errorTitle: "打开修改图书界面"
            );
        }

        /// <summary>
        /// 点击单元格事件：更新底部选中行信息（纯内存操作，无需异步）
        /// </summary>
        private void dataGridView1_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedBook(out Book book)) return;
            label2.Text = book.BookID + "  书名：" + book.Title;
        }

        /// <summary>
        /// 尝试获取当前选中行对应的图书实体（删除/修改共用）
        /// </summary>
        private bool TryGetSelectedBook(out Book book)
        {
            book = null;
            if (dataGridView1.SelectedRows.Count == 0) return false;
            book = dataGridView1.SelectedRows[0].DataBoundItem as Book;
            return book != null;
        }

        /// <summary>
        /// 查询点击事件（纯内存操作，无需异步）
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

            _currentMatchCursor = (_currentMatchCursor + 1) % _matchedRowIndices.Count;
            int targetRowIndex = _matchedRowIndices[_currentMatchCursor];

            dataGridView1.ClearSelection();
            dataGridView1.Rows[targetRowIndex].Selected = true;
            if (targetRowIndex < dataGridView1.Rows.Count)
                dataGridView1.CurrentCell = dataGridView1.Rows[targetRowIndex].Cells[0];
            if (dataGridView1.FirstDisplayedScrollingRowIndex != targetRowIndex)
                dataGridView1.FirstDisplayedScrollingRowIndex = targetRowIndex;
        }

        #region 查询辅助方法（高亮 + 状态重置 + 行内容匹配）

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

        private void ClearMatchHighlight()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                row.DefaultCellStyle.BackColor = Color.Empty;
                row.DefaultCellStyle.SelectionBackColor = Color.Empty;
            }
        }

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
