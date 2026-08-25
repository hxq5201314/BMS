using BookMS;
using BMS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BMS.Services
{
    /// <summary>
    /// 图书业务层：封装所有 books 表的数据库操作（全异步）。
    /// UI 层只调用本服务、不接触 SQL；底层数据库异常统一包装为 ServiceException
    /// </summary>
    public class BookService
    {
        private readonly Dao _dao = new Dao();

        /// <summary>异步查询全部图书</summary>
        public async Task<List<Book>> GetAllBooksAsync()
        {
            try
            {
                const string sql = "SELECT BookID, ISBN, Title, Author, publisher, Total, Remain FROM books";
                DataTable dt = await _dao.QueryDataTableAsync(sql);
                return ToList(dt);
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("加载图书列表失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 异步查询下一个可用的图书 ID（当前最大 BookID + 1）；空表返回 1
        /// </summary>
        public async Task<int> GetNextBookIdAsync()
        {
            try
            {
                const string sql = "SELECT COALESCE(MAX(BookID), 0) + 1 FROM books";
                DataTable dt = await _dao.QueryDataTableAsync(sql);
                if (dt.Rows.Count == 0) return 1;
                object v = dt.Rows[0][0];
                return (v == null || v == DBNull.Value) ? 1 : Convert.ToInt32(v);
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("查询下一个图书ID失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 异步检查指定 BookID 是否已存在；存在返回 true，不存在返回 false
        /// </summary>
        public async Task<bool> ExistsBookIdAsync(int bookId)
        {
            try
            {
                const string sql = "SELECT COUNT(*) FROM books WHERE BookID = @id";
                DataTable dt = await _dao.QueryDataTableAsync(sql,
                    new[] { new MySqlParameter("@id", bookId) });
                if (dt.Rows.Count == 0) return false;
                object v = dt.Rows[0][0];
                int n = (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
                return n > 0;
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("检查图书ID是否存在失败: " + ex.Message, ex);
            }
        }

        /// <summary>异步新增图书，返回受影响行数</summary>
        public async Task<int> AddBookAsync(Book book)
        {
            try
            {
                const string sql = @"INSERT INTO books (BookID, ISBN, Title, Author, publisher, Total, Remain)
                                     VALUES (@BookID, @ISBN, @Title, @Author, @publisher, @Total, @Remain)";
                var parameters = new[]
                {
                    new MySqlParameter("@BookID", book.BookID),
                    new MySqlParameter("@ISBN", book.ISBN),
                    new MySqlParameter("@Title", book.Title),
                    new MySqlParameter("@Author", book.Author),
                    new MySqlParameter("@publisher", book.Publisher),
                    new MySqlParameter("@Total", book.Total),
                    new MySqlParameter("@Remain", book.Remain)
                };
                return await _dao.ExecuteNonQueryAsync(sql, parameters);
            }
            catch (MySqlException ex)
            {
                // MySQL 错误号 1062 = 唯一键/主键重复
                if (ex.Number == 1062)
                {
                    if (ex.Message.Contains("PRIMARY"))
                        throw new ServiceException($"添加图书失败：ID「{book.BookID}」已存在，请更换一个不同的ID", ex);
                    if (ex.Message.Contains("ISBN"))
                        throw new ServiceException($"添加图书失败：书码「{book.ISBN}」已存在，同一本书请勿重复添加", ex);
                    throw new ServiceException($"添加图书失败：数据重复（{ex.Message}）", ex);
                }
                throw new ServiceException("添加图书失败: " + ex.Message, ex);
            }
        }

        /// <summary>异步按 BookID 更新图书（主键不可改，WHERE 用 BookID）</summary>
        public async Task<int> UpdateBookAsync(Book book)
        {
            try
            {
                const string sql = @"UPDATE books
                                     SET ISBN = @ISBN, Title = @Title, Author = @Author,
                                         publisher = @publisher, Total = @Total, Remain = @Remain
                                     WHERE BookID = @BookID";
                var parameters = new[]
                {
                    new MySqlParameter("@ISBN", book.ISBN),
                    new MySqlParameter("@Title", book.Title),
                    new MySqlParameter("@Author", book.Author),
                    new MySqlParameter("@publisher", book.Publisher),
                    new MySqlParameter("@Total", book.Total),
                    new MySqlParameter("@Remain", book.Remain),
                    new MySqlParameter("@BookID", book.BookID)
                };
                return await _dao.ExecuteNonQueryAsync(sql, parameters);
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("修改图书失败: " + ex.Message, ex);
            }
        }

        /// <summary>异步按 BookID 删除图书</summary>
        public async Task<int> DeleteBookAsync(int bookId)
        {
            try
            {
                const string sql = "DELETE FROM books WHERE BookID = @ID";
                return await _dao.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@ID", bookId) });
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("删除图书失败: " + ex.Message, ex);
            }
        }

        /// <summary>异步查询单本图书的当前可借库存 Remain；找不到则返回 null</summary>
        public async Task<int?> GetBookRemainAsync(int bookId)
        {
            try
            {
                const string sql = "SELECT Remain FROM books WHERE BookID = @id";
                DataTable dt = await _dao.QueryDataTableAsync(sql,
                    new[] { new MySqlParameter("@id", bookId) });
                if (dt.Rows.Count == 0) return null;
                object v = dt.Rows[0][0];
                return (v == null || v == DBNull.Value) ? default(int?) : Convert.ToInt32(v);
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("查询库存失败: " + ex.Message, ex);
            }
        }

        /// <summary>DataTable → List<Book></summary>
        private static List<Book> ToList(DataTable dt)
        {
            var list = new List<Book>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Book
                {
                    BookID    = AsInt(row["BookID"]),
                    ISBN      = AsString(row["ISBN"]),
                    Title     = AsString(row["Title"]),
                    Author    = AsString(row["Author"]),
                    Publisher = AsString(row["publisher"]),
                    Total     = AsInt(row["Total"]),
                    Remain    = AsInt(row["Remain"])
                });
            }
            return list;
        }

        private static string AsString(object v) => (v == null || v == DBNull.Value) ? null : v.ToString();

        private static int AsInt(object v) => (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
    }
}
