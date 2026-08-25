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
    /// 借阅业务层：封装 borrow_records 表的所有操作 + 借阅时联动更新 books.Remain。
    /// 借阅规则：同一用户对同一本书只能存在一条"借阅中"记录；归还后可再次借阅
    /// </summary>
    public class BorrowService
    {
        private readonly Dao _dao = new Dao();

        /// <summary>
        /// 首次使用时确保借阅记录表存在（幂等，可重复调用）。
        /// 在 User1_Load 中调用一次即可
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
        /// 借阅图书：去重检查（同事务内 SELECT ... FOR UPDATE 行锁防并发）→ 减库存 → 插记录。
        /// 三条 SQL 共用连接 + 事务，任意一步异常整批回滚，进程崩溃也不会残留半写数据。
        /// 失败情况：账号无效 / 库存不足 / 已借过未还 / DB 写失败
        /// </summary>
        public async Task<BorrowResult> BorrowAsync(string userId, int bookId)
        {
            // 输入校验（在事务外快速失败，避免占用连接）
            if (string.IsNullOrWhiteSpace(userId))
                return BorrowResult.Fail("账号信息无效，请确认后再操作");
            if (bookId <= 0)
                return BorrowResult.Fail("图书编号无效");

            try
            {
                return await _dao.RunInTransactionAsync(async (conn, tran) =>
                {
                    // 1. 去重检查：SELECT ... FOR UPDATE 锁住命中行，避免并发下重复插入"借阅中"记录
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

                    // 2. 减库存：WHERE Remain > 0 + 行锁防止超借
                    const string decreaseSql = "UPDATE books SET Remain = Remain - 1 WHERE BookID = @id AND Remain > 0";
                    int affected;
                    using (var decCmd = new MySqlCommand(decreaseSql, conn, tran))
                    {
                        decCmd.Parameters.AddWithValue("@id", bookId);
                        affected = await decCmd.ExecuteNonQueryAsync();
                    }
                    if (affected == 0)
                        return BorrowResult.Fail("库存不足，借阅失败");

                    // 3. 插借阅记录（和前两步同一事务，失败时由 RunInTransactionAsync 自动 Rollback，库存一起恢复）
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
        /// 查询指定用户当前"借阅中"的图书数量（COUNT，轻量，不用 JOIN/反序列化列表）
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
        /// 查询指定用户当前"借阅中"的图书列表（JOIN books 取书名/ISBN）
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
        /// 归还图书：定位"借阅中"记录 → 标记为已归还并填归还时间 → books.Remain + 1
        /// 两步 SQL 共用连接 + 事务，任意异常整批回滚，数据绝对一致。
        /// 失败情况：账号无效 / 图书编号无效 / 记录不存在或已归还 / DB 写失败
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
                    // 1. 关闭借阅记录：改状态+填归还时间；WHERE status='借阅中' + FOR UPDATE 防并发双重归还
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

                    // 2. 恢复库存：books.Remain = Remain + 1
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

        /// <summary>
        /// 判断指定用户是否已借某书且未归还（去重依据）
        /// </summary>
        private async Task<bool> IsBorrowingAsync(string userId, int bookId)
        {
            const string sql = "SELECT COUNT(*) FROM borrow_records WHERE user_id = @uid AND book_id = @bid AND status = '借阅中'";
            DataTable dt = await _dao.QueryDataTableAsync(sql, new[]
            {
                new MySqlParameter("@uid", userId),
                new MySqlParameter("@bid", bookId)
            });
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }
    }

    /// <summary>
    /// 借阅操作结果：成功时 Success=true，失败时携带 Message
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
