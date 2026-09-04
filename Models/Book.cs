namespace BMS.Models
{
    /// <summary>
    /// 图书实体
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
