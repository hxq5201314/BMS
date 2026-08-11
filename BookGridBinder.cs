using System.Collections.Generic;
using System.Windows.Forms;
using BMS.Models;

namespace BMS
{
    /// <summary>
    /// DataGridView 绑定图书列表的 UI 辅助：统一列头中文映射，避免各窗体重复配置。
    /// 模型层保持纯净，UI 显示文本集中在此
    /// </summary>
    internal static class BookGridBinder
    {
        /// <summary>Book 属性名 → DataGridView 列标题的映射</summary>
        private static readonly Dictionary<string, string> _headers = new Dictionary<string, string>
        {
            { "BookID", "ID" },
            { "ISBN", "书码" },
            { "Title", "书名" },
            { "Author", "作者" },
            { "Publisher", "来源" },
            { "Total", "库存" },
            { "Remain", "剩余" }
        };

        /// <summary>
        /// 绑定图书列表到 DataGridView：自动生成列 → 套用中文表头 → 调整列宽
        /// </summary>
        /// <param name="readOnly">是否只读（用户端查看应传 true）</param>
        public static void Bind(DataGridView grid, IList<Book> books, bool readOnly = false)
        {
            grid.DataSource = books;
            grid.AllowUserToAddRows = false;

            // 先套中文表头再自适应列宽，避免表头被截断
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (_headers.TryGetValue(col.Name, out string header))
                    col.HeaderText = header;
            }
            grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            if (readOnly)
            {
                grid.ReadOnly = true;
                grid.AllowUserToDeleteRows = false;
            }
        }
    }
}
