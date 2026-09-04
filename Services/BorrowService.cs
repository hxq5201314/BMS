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
    /// 借阅业务层：借/还/查询记录
    /// </summary>
    public class BorrowService
    {
        private readonly Dao _dao = new Dao();

        /// <summary>
        /// 确保借阅记录表存在（可重复调用）
        /// </summary>
        public async Task EnsureTableExistsAsync()
        {
            const string sql = @"
                                CREATE TABLE IF NOT EXISTS borrow_records (
                                    borrow_id   INT AUTO_INCREMENT PRIMARY KEY,
                                    user_id     VARCHAR(50) NOT NULL,
                                    book_id     INT NOT NULL,
                                    borrow_date DATETIME NOT NULL,
                                    return_date DATETIME NULL,
                                    status      VARCHAR(20) NOT NULL DEFAULT '借阅中',
                                    INDEX idx_user (user_id),
                                    INDEX idx_book (book_id),
                                    INDEX idx_status (status)
                                ) CHARSET=utf8mb4;";
            try
            {
                await _dao.ExecuteNonQueryAsync(sql, null);
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("初始化借阅表失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 借阅图书：去重检查 → 减库存 → 插记录（事务内执行）
        /// </summary>
        public async Task<BorrowResult> BorrowAsync(string userId, int bookId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BorrowResult.Fail("账号信息无效，请确认后再操作");
            if (bookId <= 0)
                return BorrowResult.Fail("图书编号无效");

            try
            {
                return await _dao.RunInTransactionAsync(async (conn, tran) =>
                {
                    // 去重检查（同事务内 FOR UPDATE 行锁防并发）
                    const string checkSql = @"SELECT COUNT(*) FROM borrow_records
                                              WHERE user_id = @uid AND book_id = @bid AND status = '借阅中'
                                              FOR UPDATE";
                    using (var checkCmd = new MySqlCommand(checkSql, conn, tran))
                    {
                        checkCmd.Parameters.AddWithValue("@uid", userId);
                        checkCmd.Parameters.AddWithValue("@bid", bookId);
                        int existing = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (existing > 0)
                            return BorrowResult.Fail("您已借阅此书且尚未归还，无法重复借阅");
                    }

                    // 减库存（WHERE Remain > 0 防超借）
                    const string decreaseSql = "UPDATE books SET Remain = Remain - 1 WHERE BookID = @id AND Remain > 0";
                    int affected;
                    using (var decCmd = new MySqlCommand(decreaseSql, conn, tran))
                    {
                        decCmd.Parameters.AddWithValue("@id", bookId);
                        affected = await decCmd.ExecuteNonQueryAsync();
                    }
                    if (affected == 0)
                        return BorrowResult.Fail("库存不足，借阅失败");

                    // 插借阅记录
                    const string insertSql = @"INSERT INTO borrow_records (user_id, book_id, borrow_date, status)
                                               VALUES (@uid, @bid, @date, '借阅中')";
                    using (var insCmd = new MySqlCommand(insertSql, conn, tran))
                    {
                        insCmd.Parameters.AddWithValue("@uid", userId);
                        insCmd.Parameters.AddWithValue("@bid", bookId);
                        insCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        await insCmd.ExecuteNonQueryAsync();
                    }

                    return BorrowResult.Ok();
                });
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("借阅失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 查询指定用户当前"借阅中"的图书数量
        /// </summary>
        public async Task<int> GetBorrowingCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ServiceException("账号信息无效，无法查询借阅数量");

            try
            {
                const string sql = "SELECT COUNT(*) FROM borrow_records WHERE user_id = @uid AND status = '借阅中'";
                DataTable dt = await _dao.QueryDataTableAsync(sql,
                    new[] { new MySqlParameter("@uid", userId) });
                return Convert.ToInt32(dt.Rows[0][0]);
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("查询借阅数量失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 查询指定用户当前"借阅中"的图书列表
        /// </summary>
        public async Task<List<BorrowRecord>> GetBorrowingAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ServiceException("账号信息无效，无法查询借阅记录");

            try
            {
                const string sql = @"
SELECT br.borrow_id, br.user_id, br.book_id, br.borrow_date, br.status,
       b.Title AS BookTitle, b.ISBN AS BookIsbn
FROM borrow_records br
JOIN books b ON br.book_id = b.BookID
WHERE br.user_id = @uid AND br.status = '借阅中'
ORDER BY br.borrow_date DESC";
                DataTable dt = await _dao.QueryDataTableAsync(sql,
                    new[] { new MySqlParameter("@uid", userId) });

                var list = new List<BorrowRecord>(dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new BorrowRecord
                    {
                        BorrowId   = Convert.ToInt32(row["borrow_id"]),
                        UserId     = row["user_id"]?.ToString(),
                        BookId     = Convert.ToInt32(row["book_id"]),
                        BorrowDate = Convert.ToDateTime(row["borrow_date"]),
                        Status     = row["status"]?.ToString(),
                        BookTitle  = row["BookTitle"]?.ToString(),
                        BookIsbn   = row["BookIsbn"]?.ToString()
                    });
                }
                return list;
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("查询借阅记录失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 归还图书：关记录 → 恢复库存（事务内执行）
        /// </summary>
        public async Task<BorrowResult> ReturnBookAsync(string userId, int bookId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BorrowResult.Fail("账号信息无效，请确认后再操作");
            if (bookId <= 0)
                return BorrowResult.Fail("图书编号无效");

            try
            {
                DateTime now = DateTime.Now;
                return await _dao.RunInTransactionAsync(async (conn, tran) =>
                {
                    // 关借阅记录（LIMIT 1 + 行锁防双重归还）
                    const string closeRecordSql = @"UPDATE borrow_records
                                                    SET status = '已归还', return_date = @rdate
                                                    WHERE user_id = @uid AND book_id = @bid AND status = '借阅中'
                                                    LIMIT 1";
                    int closed;
                    using (var closeCmd = new MySqlCommand(closeRecordSql, conn, tran))
                    {
                        closeCmd.Parameters.AddWithValue("@uid", userId);
                        closeCmd.Parameters.AddWithValue("@bid", bookId);
                        closeCmd.Parameters.AddWithValue("@rdate", now);
                        closed = await closeCmd.ExecuteNonQueryAsync();
                    }
                    if (closed == 0)
                        return BorrowResult.Fail("没有找到您借阅中的该书记录，可能已归还或未借阅");

                    // 恢复库存
                    const string restoreStockSql = "UPDATE books SET Remain = Remain + 1 WHERE BookID = @id";
                    using (var restoreCmd = new MySqlCommand(restoreStockSql, conn, tran))
                    {
                        restoreCmd.Parameters.AddWithValue("@id", bookId);
                        await restoreCmd.ExecuteNonQueryAsync();
                    }

                    return BorrowResult.Returned();
                });
            }
            catch (MySqlException ex)
            {
                throw new ServiceException("归还失败: " + ex.Message, ex);
            }
        }
    }

    /// <summary>
    /// 借阅操作结果
    /// </summary>
    public class BorrowResult
    {
        public bool Success { get; }
        public string Message { get; }

        private BorrowResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static BorrowResult Ok() => new BorrowResult(true, "借阅成功");
        public static BorrowResult Returned() => new BorrowResult(true, "归还成功");
        public static BorrowResult Fail(string reason) => new BorrowResult(false, reason);
    }
}
