using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS
{
    /// <summary>
    /// 窗体辅助工具：统一封装窗体切换的通用模式，消除重复代码。
    /// 支持异步回调（例如返回时异步刷新数据）。
    /// </summary>
    internal static class FormHelper
    {
        /// <summary>
        /// 模态打开子窗体：隐藏当前窗体 → 显示子窗体 → 关闭后等待回调 → 恢复当前窗体。
        /// onReturnAsync: 子窗体关闭后、恢复当前窗体前执行的异步回调（如刷新列表）
        /// </summary>
        /// <param name="owner">当前窗体（调用者）</param>
        /// <param name="createChild">创建子窗体的委托（确保每次都使用新实例）</param>
        /// <param name="onReturnAsync">子窗体关闭后异步执行的回调（可选）</param>
        /// <param name="onReturn">同步回调（兼容性保留，无异步场景用）</param>
        /// <param name="errorTitle">异常时的标题前缀</param>
        public static async Task ShowModal(
            Form owner,
            Func<Form> createChild,
            Func<Task> onReturnAsync = null,
            Action onReturn = null,
            string errorTitle = "打开界面")
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (createChild == null) throw new ArgumentNullException(nameof(createChild));

            Form child = null;
            try
            {
                child = createChild();
                owner.Hide();
                child.ShowDialog(owner);  // WinForms ShowDialog 本身同步，但消息泵仍在
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorTitle}失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                child?.Dispose();
                if (!owner.IsDisposed) owner.Show();
                return;
            }

            // 子窗体正常关闭后先执行回调（此时 child 还未 Disposed，owner 也未 Disposed）。
            // 之前先 Dispose 再回调 → 嵌套 ShowModal 场景下，外层回调会访问到被父层 finally 提前 Dispose 的窗体。
            // 顺序调整：回调执行完成 → Dispose child → 恢复 owner 显示
            if (!owner.IsDisposed)
            {
                if (onReturnAsync != null)
                {
                    try { await onReturnAsync(); }
                    catch (Exception ex) { MessageBox.Show($"{errorTitle}后回调失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
                onReturn?.Invoke();
            }

            child?.Dispose();
            if (!owner.IsDisposed) owner.Show();
        }
    }
}
