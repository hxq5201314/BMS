using BMS.Interface.User;
using BMS.Models;
using BMS.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS
{
    public partial class User1 : Form
    {
        private readonly BookService _bookService = new BookService();
        private readonly BorrowService _borrowService = new BorrowService();

        // 底部状态栏：显示当前用户正在借阅数量；动态创建，不依赖 Designer.cs
        private readonly StatusStrip _statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel _borrowCountLabel = new ToolStripStatusLabel();

        public User1()
        {
            InitializeComponent();
            InitStatusStrip();
            InitExitMenuHandler();
            InitReturnMenuHandler();
        }

        /// <summary>
        /// 在窗体底部动态添加 StatusStrip，用于显示"正在借阅 X 本"等实时数据
        /// </summary>
        private void InitStatusStrip()
        {
            _borrowCountLabel.Text = "正在借阅：- 本";
            _statusStrip.Items.Add(_borrowCountLabel);
            // StatusStrip 自带 Dock = Bottom，按添加顺序追加到已有控件后
            this.Controls.Add(_statusStrip);
        }

        /// <summary>
        /// 为"系统 → 退出"菜单动态绑定 Click 事件（Designer.cs 未绑定，避免改动设计器代码）
        /// </summary>
        private void InitExitMenuHandler()
        {
            // 菜单名由 Designer.cs 生成：系统ToolStripMenuItem 下包含 退出ToolStripMenuItem
            var exitItem = FindMenuItemByName(menuStrip1.Items, "退出ToolStripMenuItem");
            if (exitItem != null)
            {
                exitItem.Click -= 退出ToolStripMenuItem_Click;
                exitItem.Click += 退出ToolStripMenuItem_Click;
            }
        }

        /// <summary>
        /// 为"借阅信息"菜单下的"归还图书"绑定/注入 Click 事件
        /// - 如果 Designer.cs 已声明"归还图书ToolStripMenuItem"（方法体已在本文件生成空壳）则直接挂事件
        /// - 若不存在则动态插入新菜单项（放在"借阅"之后），避免修改 Designer.cs
        /// </summary>
        private void InitReturnMenuHandler()
        {
            var returnItem = FindMenuItemByName(menuStrip1.Items, "归还图书ToolStripMenuItem");
            if (returnItem == null)
            {
                var parent = FindMenuItemByName(menuStrip1.Items, "借阅信息ToolStripMenuItem");
                if (parent == null) return;

                var borrowItem = FindMenuItemByName(menuStrip1.Items, "借阅ToolStripMenuItem");
                int insertIdx = borrowItem != null
                    ? parent.DropDownItems.IndexOf(borrowItem) + 1
                    : parent.DropDownItems.Count;

                returnItem = new ToolStripMenuItem("归还图书") { Name = "归还图书ToolStripMenuItem", Size = new Size(270, 34) };
                parent.DropDownItems.Insert(insertIdx, returnItem);
            }

            returnItem.Click -= 归还图书ToolStripMenuItem_Click;
            returnItem.Click += 归还图书ToolStripMenuItem_Click;
        }

        /// <summary>
        /// 轻量输入对话框：替代 Microsoft.VisualBasic.Interaction.InputBox，不额外加程序集引用
        /// </summary>
        /// <returns>用户输入的字符串；取消或关闭返回 null</returns>
        private static string ShowInputDialog(string title, string prompt, string defaultValue = "")
        {
            using (var form = new Form())
            using (var lbl = new Label { AutoSize = true, Left = 12, Top = 15, Text = prompt })
            using (var txt = new TextBox { Left = 12, Top = 40, Width = 320, Text = defaultValue })
            using (var okBtn = new Button { Text = "确定", Left = 150, Width = 80, Height = 30, DialogResult = DialogResult.OK })
            using (var cancelBtn = new Button { Text = "取消", Left = 252, Width = 80, Height = 30, DialogResult = DialogResult.Cancel })
            {
                okBtn.Top = cancelBtn.Top = 72;

                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ClientSize = new Size(344, 118);
                form.Controls.AddRange(new Control[] { lbl, txt, okBtn, cancelBtn });
                form.AcceptButton = okBtn;
                form.CancelButton = cancelBtn;

                return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
            }
        }

        /// <summary>
        /// 递归查找 ToolStripItem 集合中指定 Name 的菜单项
        /// </summary>
        private static ToolStripMenuItem FindMenuItemByName(ToolStripItemCollection items, string name)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem mi)
                {
                    if (mi.Name == name) return mi;
                    var found = FindMenuItemByName(mi.DropDownItems, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// 退出当前用户：二次确认 → 清空全局登录态 → 关闭本窗体 → 回到 LoginInterface
        /// </summary>
        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "确认退出当前登录账号，返回登录界面？",
                "退出登录",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            Data.UID = null;
            Data.UName = null;
            Data.Role = null;

            this.Close();
        }

        private async void User1_Load(object sender, EventArgs e)
        {
            // 首次进入时确保借阅记录表存在（幂等）
            try { await _borrowService.EnsureTableExistsAsync(); }
            catch (ServiceException ex) { MessageBox.Show(ex.Message, "初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

            // 并行：图书列表和借阅计数互不依赖，同时发起减少总等待
            Task loadBooks = LoadBookListAsync();
            Task refreshCount = RefreshBorrowCountAsync();
            await Task.WhenAll(loadBooks, refreshCount);
        }

        /// <summary>
        /// 通过业务层异步加载图书列表并绑定
        /// </summary>
        private async Task LoadBookListAsync()
        {
            if (this.IsDisposed || dataGridView1.IsDisposed) return;

            try
            {
                List<Book> books = await _bookService.GetAllBooksAsync();
                if (this.IsDisposed || dataGridView1.IsDisposed) return;
                BookGridBinder.Bind(dataGridView1, books);
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
        /// 异步刷新底部状态栏的"正在借阅 N 本"计数
        /// </summary>
        private async Task RefreshBorrowCountAsync()
        {
            if (this.IsDisposed || _borrowCountLabel.IsDisposed) return;
            try
            {
                int count = await _borrowService.GetBorrowingCountAsync(Data.UID);
                if (!_borrowCountLabel.IsDisposed)
                    _borrowCountLabel.Text = $"正在借阅：{count} 本";
            }
            catch
            {
                // 状态栏只是辅助信息，失败不弹框干扰主流程，直接显示未知
                if (!_borrowCountLabel.IsDisposed)
                    _borrowCountLabel.Text = "正在借阅：- 本";
            }
        }

        /// <summary>
        /// 图书查看事件：获取选中图书实体 → 注入 SeeBook 打开详情
        /// </summary>
        private async void 查看ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedBook(out Book book))
            {
                MessageBox.Show("请先在列表中选中要查看的图书");
                return;
            }

            await FormHelper.ShowModal(
                owner: this,
                createChild: () => new SeeBook(book),
                errorTitle: "打开图书详情"
            );
        }

        /// <summary>
        /// 借阅事件：根据选中行借书，同一本书未归还不可重复借阅；
        /// 借阅成功 → books.Remain 减一（列表已验证刷新）、用户正在借阅图书计数真实 +1
        /// </summary>
        private async void 借阅ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. 选中行校验
            if (!TryGetSelectedBook(out Book book))
            {
                MessageBox.Show("请先在列表中选中要借阅的图书");
                return;
            }

            // 2. 二次确认
            DialogResult dr = MessageBox.Show(
                $"确认借阅《{book.Title}》？",
                "借阅确认",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            // 3. 借阅前快照：正在借阅数量 + 当前 Remain
            int countBefore = 0;
            int remainBefore = book.Remain;
            try { countBefore = await _borrowService.GetBorrowingCountAsync(Data.UID); }
            catch { /* 查询失败时用 0，最终仍可用 countAfter - countBefore 判断变化 */ }

            // 4. 调用业务层执行借阅
            try
            {
                BorrowResult result = await _borrowService.BorrowAsync(Data.UID, book.BookID);
                if (result.Success)
                {
                    // 借阅成功后再查一次真实数据，避免写死文字
                    int countAfter = 0;
                    try { countAfter = await _borrowService.GetBorrowingCountAsync(Data.UID); }
                    catch { /* 忽略，仅影响显示 */ }

                    var sb = new StringBuilder();
                    sb.AppendLine("借阅成功！");
                    sb.AppendLine();
                    sb.AppendLine($"书名：《{book.Title}》");
                    sb.AppendLine($"剩余可借库存：{remainBefore} → {remainBefore - 1}（-1）");
                    sb.Append($"您正在借阅的图书：{countBefore} → {countAfter}（{(countAfter - countBefore >= 0 ? "+" : "")}{countAfter - countBefore}）");

                    MessageBox.Show(sb.ToString(), "借阅结果",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 刷新列表（Remain 列）和底部计数
                    await LoadBookListAsync();
                    await RefreshBorrowCountAsync();
                }
                else
                {
                    MessageBox.Show(result.Message, "借阅失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        /// 正在借阅事件：查询当前用户借阅中图书列表，由 MessageBox 显示
        /// </summary>
        private async void 正在借阅ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                List<BorrowRecord> records = await _borrowService.GetBorrowingAsync(Data.UID);

                if (records.Count == 0)
                {
                    MessageBox.Show("您当前没有正在借阅的图书", "正在借阅", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 拼装列表文本：序号 + 书名 + 借阅时间
                var sb = new StringBuilder();
                sb.AppendLine($"您正在借阅 {records.Count} 本图书：");
                sb.AppendLine(new string('-', 40));
                for (int i = 0; i < records.Count; i++)
                {
                    BorrowRecord r = records[i];
                    sb.AppendLine($"{i + 1}. 《{r.BookTitle}》");
                    sb.AppendLine($"   书码：{r.BookIsbn}");
                    sb.AppendLine($"   借阅时间：{r.BorrowDate:yyyy-MM-dd HH:mm}");
                }
                MessageBox.Show(sb.ToString(), "正在借阅", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 用户点完"正在借阅"后，底部状态栏同步到最新数量（可能别处有归还操作）
                await RefreshBorrowCountAsync();
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
        /// 获取选中行对应的图书实体
        /// </summary>
        private bool TryGetSelectedBook(out Book book)
        {
            book = null;
            if (dataGridView1.SelectedRows.Count == 0) return false;
            book = dataGridView1.SelectedRows[0].DataBoundItem as Book;
            return book != null;
        }

        /// <summary>
        /// 归还图书：列出借阅中的图书 → 用户输入序号 → 二次确认 → 归还 → 刷新
        /// </summary>
        private async void 归还图书ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. 查询当前借阅中图书列表
            List<BorrowRecord> records;
            try
            {
                records = await _borrowService.GetBorrowingAsync(Data.UID);
            }
            catch (ServiceException ex)
            {
                MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (records.Count == 0)
            {
                MessageBox.Show("您当前没有正在借阅的图书，无需归还", "归还图书",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. 拼装借阅中图书预览 + 序号
            var sb = new StringBuilder();
            sb.AppendLine("请输入要归还的图书序号：");
            sb.AppendLine(new string('-', 30));
            for (int i = 0; i < records.Count; i++)
            {
                BorrowRecord r = records[i];
                sb.AppendLine($"{i + 1}. 《{r.BookTitle}》");
                sb.AppendLine($"   书码：{r.BookIsbn}");
                sb.AppendLine($"   借阅时间：{r.BorrowDate:yyyy-MM-dd HH:mm}");
            }
            // 先弹一次提示让用户看清列表，再弹 InputBox（InputBox 没有多行提示区域）
            MessageBox.Show(sb.ToString(), "归还图书 - 选择要归还的序号",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            string input = ShowInputDialog("归还图书", "请输入序号（从 1 开始）：", "1");
            if (input == null) return; // 用户取消

            if (!int.TryParse(input, out int idx) || idx < 1 || idx > records.Count)
            {
                MessageBox.Show($"序号无效，必须输入 1 ~ {records.Count} 之间的整数", "归还图书",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BorrowRecord selected = records[idx - 1];

            // 3. 快照 + 二次确认
            int countBefore = 0;
            int remainBefore = 0;
            try { countBefore = await _borrowService.GetBorrowingCountAsync(Data.UID); } catch { /* 忽略，仅影响显示 */ }
            try { remainBefore = (await _bookService.GetBookRemainAsync(selected.BookId)) ?? 0; } catch { /* 忽略 */ }

            DialogResult dr = MessageBox.Show(
                $"确认归还《{selected.BookTitle}》？",
                "归还确认",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            // 4. 调用业务层执行归还
            try
            {
                BorrowResult result = await _borrowService.ReturnBookAsync(Data.UID, selected.BookId);
                if (result.Success)
                {
                    int countAfter = 0;
                    int remainAfter = 0;
                    try { countAfter = await _borrowService.GetBorrowingCountAsync(Data.UID); } catch { /* 忽略 */ }
                    try { remainAfter = (await _bookService.GetBookRemainAsync(selected.BookId)) ?? 0; } catch { /* 忽略 */ }

                    var summary = new StringBuilder();
                    summary.AppendLine("归还成功！");
                    summary.AppendLine();
                    summary.AppendLine($"书名：《{selected.BookTitle}》");
                    summary.AppendLine($"剩余可借库存：{remainBefore} → {remainAfter}（+{remainAfter - remainBefore}）");
                    summary.Append($"您正在借阅的图书：{countBefore} → {countAfter}（{countAfter - countBefore}）");

                    MessageBox.Show(summary.ToString(), "归还结果",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    await LoadBookListAsync();
                    await RefreshBorrowCountAsync();
                }
                else
                {
                    MessageBox.Show(result.Message, "归还失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
    }
}
