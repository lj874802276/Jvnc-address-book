using System.Windows;
using Microsoft.Win32;

namespace UvncAddressBook
{
    /// <summary>
    /// 设置：配置 UltraVNC Viewer 可执行文件路径。
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public string UvncPath { get; private set; }

        public SettingsWindow(string currentPath)
        {
            InitializeComponent();
            TxtPath.Text = currentPath;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择 uvncviewer.exe"
            };
            if (dlg.ShowDialog() == true)
                TxtPath.Text = dlg.FileName;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            UvncPath = TxtPath.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
