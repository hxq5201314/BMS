using System.Collections.Generic;
using System.Windows.Forms;
using BMS.Models;

namespace BMS
{
    /// <summary>
    /// DataGridView 图书列表绑定辅助
    /// </summary>
    internal static class BookGridBinder
    {
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
        /// 绑定图书列表到 DataGridView，自动套用中文列头
        /// </summary>
        public static void Bind(DataGridView grid, IList<Book> books, bool readOnly = false)
        {
            grid.DataSource = books;
            grid.AllowUserToAddRows = false;

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
