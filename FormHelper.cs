using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BMS
{
    /// <summary>
    /// 窗体切换辅助：隐藏当前 → 模态显示子窗体 → 关闭后恢复
    /// </summary>
    internal static class FormHelper
    {
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
                child.ShowDialog(owner);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorTitle}失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                child?.Dispose();
                if (!owner.IsDisposed) owner.Show();
                return;
            }

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
