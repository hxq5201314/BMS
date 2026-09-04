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

        private readonly StatusStrip _statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel _borrowCountLabel = new ToolStripStatusLabel();

        public User1()
        {
            InitializeComponent();
            InitStatusStrip();
            InitExitMenuHandler();
            InitReturnMenuHandler();
        }

        private void InitStatusStrip()
        {
            _borrowCountLabel.Text = "正在借阅：- 本";
            _statusStrip.Items.Add(_borrowCountLabel);
            this.Controls.Add(_statusStrip);
        }

        private void InitExitMenuHandler()
        {
            var exitItem = FindMenuItemByName(menuStrip1.Items, "退出ToolStripMenuItem");
            if (exitItem == null)
            {
                string msg = "初始化失败：未找到菜单\"系统 → 退出\"，退出登录功能无法使用。" +
                             Environment.NewLine + "请确认 User1.Designer.cs 中菜单项名称\"退出ToolStripMenuItem\"是否被重命名。";
                MessageBox.Show(msg, "菜单初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            exitItem.Click -= 退出ToolStripMenuItem_Click;
            exitItem.Click += 退出ToolStripMenuItem_Click;
        }

        private void InitReturnMenuHandler()
        {
            var returnItem = FindMenuItemByName(menuStrip1.Items, "归还图书ToolStripMenuItem");
            if (returnItem == null)
            {
                var parent = FindMenuItemByName(menuStrip1.Items, "借阅信息ToolStripMenuItem");
                if (parent == null)
                {
                    string msg = "初始化失败：未找到\"借阅信息\"主菜单，归还图书功能无法加载。" +
                                 Environment.NewLine + "请确认 User1.Designer.cs 中菜单项名称\"借阅信息ToolStripMenuItem\"是否被重命名。";
                    MessageBox.Show(msg, "菜单初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

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

        private static string ShowInputDialog(
            IWin32Window owner,
            string title,
            string prompt,
            string contentAbove = null,
            string defaultValue = "")
        {
            using (var form = new Form())
            using (var okBtn = new Button { Text = "确定", Width = 80, Height = 30, DialogResult = DialogResult.OK })
            using (var cancelBtn = new Button { Text = "取消", Width = 80, Height = 30, DialogResult = DialogResult.Cancel })
            {
                const int LeftMargin = 12;
                const int MinClientWidth = 344;
                int cursorTop = 15;
                int clientWidth = MinClientWidth;

                Label contentLbl = null;
                if (!string.IsNullOrEmpty(contentAbove))
                {
                    contentLbl = new Label
                    {
                        AutoSize = false,
                        Left = LeftMargin,
                        Top = cursorTop,
                        Width = clientWidth - LeftMargin * 2,
                        Text = contentAbove,
                        Height = 500
                    };
                    Size pref = contentLbl.PreferredSize;
                    contentLbl.Height = Math.Max(pref.Height, 20);
                    form.Controls.Add(contentLbl);
                    cursorTop = contentLbl.Bottom + 10;
                }

                var lbl = new Label
                {
                    AutoSize = true,
                    Left = LeftMargin,
                    Top = cursorTop,
                    Text = prompt
                };
                form.Controls.Add(lbl);
                cursorTop = lbl.Bottom + 6;

                var txt = new TextBox
                {
                    Left = LeftMargin,
                    Top = cursorTop,
                    Width = clientWidth - LeftMargin * 2,
                    Text = defaultValue
                };
                form.Controls.Add(txt);
                cursorTop = txt.Bottom + 12;

                cancelBtn.Left = clientWidth - LeftMargin - cancelBtn.Width;
                okBtn.Left = cancelBtn.Left - 8 - okBtn.Width;
                okBtn.Top = cancelBtn.Top = cursorTop;
                form.Controls.AddRange(new Control[] { okBtn, cancelBtn });

                int clientHeight = cancelBtn.Bottom + 12;
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ClientSize = new Size(clientWidth, clientHeight);
                form.AcceptButton = okBtn;
                form.CancelButton = cancelBtn;

                IWin32Window ownerActual = owner ?? Form.ActiveForm;
                return form.ShowDialog(ownerActual) == DialogResult.OK ? txt.Text : null;
            }
        }

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
            try { await _borrowService.EnsureTableExistsAsync(); }
            catch (ServiceException ex) { MessageBox.Show(ex.Message, "初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

            Task loadBooks = LoadBookListAsync();
            Task refreshCount = RefreshBorrowCountAsync();
            await Task.WhenAll(loadBooks, refreshCount);
        }

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
                if (!_borrowCountLabel.IsDisposed)
                    _borrowCountLabel.Text = "正在借阅：- 本";
            }
        }

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

        private async void 借阅ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedBook(out Book book))
            {
                MessageBox.Show("请先在列表中选中要借阅的图书");
                return;
            }

            DialogResult dr = MessageBox.Show(
                $"确认借阅《{book.Title}》？",
                "借阅确认",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            int countBefore = 0;
            int remainBefore = book.Remain;
            try { countBefore = await _borrowService.GetBorrowingCountAsync(Data.UID); }
            catch { }

            try
            {
                BorrowResult result = await _borrowService.BorrowAsync(Data.UID, book.BookID);
                if (result.Success)
                {
                    int countAfter = 0;
                    try { countAfter = await _borrowService.GetBorrowingCountAsync(Data.UID); }
                    catch { }

                    var sb = new StringBuilder();
                    sb.AppendLine("借阅成功！");
                    sb.AppendLine();
                    sb.AppendLine($"书名：《{book.Title}》");
                    sb.AppendLine($"剩余可借库存：{remainBefore} → {remainBefore - 1}（-1）");
                    sb.Append($"您正在借阅的图书：{countBefore} → {countAfter}（{(countAfter - countBefore >= 0 ? "+" : "")}{countAfter - countBefore}）");

                    MessageBox.Show(sb.ToString(), "借阅结果",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        private bool TryGetSelectedBook(out Book book)
        {
            book = null;
            if (dataGridView1.SelectedRows.Count == 0) return false;
            book = dataGridView1.SelectedRows[0].DataBoundItem as Book;
            return book != null;
        }

        private async void 归还图书ToolStripMenuItem_Click(object sender, EventArgs e)
        {
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

            var sb = new StringBuilder();
            sb.AppendLine("当前正在借阅的图书清单：");
            sb.AppendLine(new string('-', 32));
            for (int i = 0; i < records.Count; i++)
            {
                BorrowRecord r = records[i];
                sb.AppendLine($"{i + 1}. 《{r.BookTitle}》");
                sb.AppendLine($"   书码：{r.BookIsbn}");
                sb.AppendLine($"   借阅时间：{r.BorrowDate:yyyy-MM-dd HH:mm}");
            }

            string input = ShowInputDialog(
                owner: this,
                title: "归还图书",
                prompt: $"请输入要归还的图书序号（1 ~ {records.Count}）：",
                contentAbove: sb.ToString(),
                defaultValue: "1");
            if (input == null) return;

            if (!int.TryParse(input, out int idx) || idx < 1 || idx > records.Count)
            {
                MessageBox.Show($"序号无效，必须输入 1 ~ {records.Count} 之间的整数", "归还图书",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BorrowRecord selected = records[idx - 1];

            int? countBefore = null;
            int? remainBefore = null;
            try { countBefore = await _borrowService.GetBorrowingCountAsync(Data.UID); } catch { }
            try { remainBefore = await _bookService.GetBookRemainAsync(selected.BookId); } catch { }

            DialogResult dr = MessageBox.Show(
                $"确认归还《{selected.BookTitle}》？",
                "归还确认",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            try
            {
                BorrowResult result = await _borrowService.ReturnBookAsync(Data.UID, selected.BookId);
                if (result.Success)
                {
                    int? countAfter = null;
                    int? remainAfter = null;
                    try { countAfter = await _borrowService.GetBorrowingCountAsync(Data.UID); } catch { }
                    try { remainAfter = await _bookService.GetBookRemainAsync(selected.BookId); } catch { }

                    string FormatDelta(int? before, int? after, string label, out bool hasDeltaError)
                    {
                        hasDeltaError = before == null || after == null;
                        if (hasDeltaError)
                            return $"{label}：查询失败";
                        int d = after.Value - before.Value;
                        string deltaStr = (d >= 0 ? "+" : "") + d;
                        return $"{label}：{before.Value} → {after.Value}（{deltaStr}）";
                    }

                    string remainLine = FormatDelta(remainBefore, remainAfter, "剩余可借库存", out bool _);
                    string countLine  = FormatDelta(countBefore,  countAfter,  "您正在借阅的图书", out bool __);

                    var summary = new StringBuilder();
                    summary.AppendLine("归还成功！");
                    summary.AppendLine();
                    summary.AppendLine($"书名：《{selected.BookTitle}》");
                    summary.AppendLine(remainLine);
                    summary.Append(countLine);

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
