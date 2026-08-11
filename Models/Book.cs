namespace BMS.Models
{
    /// <summary>
    /// 图书实体类：对应 books 表的一行。
    /// 各层（Service / UI）通过它传递图书数据，不再散落 7 个参数
    /// </summary>
    public class Book
    {
        public int BookID { get; set; }
        public string ISBN { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public int Total { get; set; }
        public int Remain { get; set; }
    }
}
