using System;

namespace BMS.Models
{
    /// <summary>
    /// 借阅记录实体
    /// </summary>
    public class BorrowRecord
    {
        public int BorrowId { get; set; }
        public string UserId { get; set; }
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public string Status { get; set; }

        // 关联显示字段（JOIN books 表填充）
        public string BookTitle { get; set; }
        public string BookIsbn { get; set; }
    }
}
