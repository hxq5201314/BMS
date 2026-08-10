using System;
using System.Windows.Forms;

namespace BMS
{
    /// <summary>
    /// 窗体辅助工具：统一封装窗体切换的通用模式，消除重复代码
    /// </summary>
    internal static class FormHelper
    {
        /// <summary>
        /// 模态打开子窗体：隐藏当前窗体 → 显示子窗体 → 关闭后恢复当前窗体
        /// </summary>
        /// <param name="owner">当前窗体（调用者）</param>
        /// <param name="createChild">创建子窗体的委托（确保每次都使用新实例）</param>
        /// <param name="onReturn">子窗体关闭后、恢复当前窗体前执行的回调（可选，用于刷新数据等）</param>
        /// <param name="errorTitle">异常时的标题前缀</param>
        public static void ShowModal(Form owner, Func<Form> createChild, Action onReturn = null, string errorTitle = "打开界面")
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
                owner.Show();
                return;
            }
            finally
            {
                child?.Dispose();
            }

            // 子窗体正常关闭后执行回调（例如刷新列表）
            onReturn?.Invoke();
            owner.Show();
        }
    }
}
