using System;

namespace BMS.Models
{
    /// <summary>
    /// 借阅记录实体：对应 borrow_records 表的一行。
    /// 关联显示字段（BookTitle / BookIsbn）由 JOIN 查询填充
    /// </summary>
    public class BorrowRecord
    {
        public int BorrowId { get; set; }
        public string UserId { get; set; }
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        /// <summary>状态："借阅中" / "已归还"</summary>
        public string Status { get; set; }

        // ===== 关联显示字段（JOIN books 表填充，非本表列）=====

        /// <summary>书名（来自 books 表）</summary>
        public string BookTitle { get; set; }

        /// <summary>ISBN（来自 books 表）</summary>
        public string BookIsbn { get; set; }
    }
}
