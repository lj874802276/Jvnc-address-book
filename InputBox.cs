using System.Windows;

namespace UvncAddressBook
{
    /// <summary>
    /// 简单的文本输入对话框（用于新建/重命名分组）。
    /// </summary>
    public partial class InputBox : Window
    {
        public string InputText { get; private set; }

        /// <summary>
        /// 便捷静态方法：弹出输入对话框，返回用户输入；取消则返回 null。
        /// </summary>
        public static string Show(string title, string prompt, string defaultText, Window owner = null)
        {
            var dlg = new InputBox(title, prompt, defaultText);
            if (owner != null) dlg.Owner = owner;
            return dlg.ShowDialog() == true ? dlg.InputText : null;
        }

        public InputBox(string title, string prompt, string defaultText)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultText;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
