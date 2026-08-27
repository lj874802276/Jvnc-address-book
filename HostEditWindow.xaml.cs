using System;
using System.Windows;
using System.Windows.Controls;

namespace UvncAddressBook
{
    /// <summary>
    /// 新增/编辑主机对话框。包含 IP 与端口本地校验，不写入任何密码。
    /// </summary>
    public partial class HostEditWindow : Window
    {
        public Host Result { get; private set; }

        public HostEditWindow(Host host, bool isEdit)
        {
            InitializeComponent();
            Result = host;
            TxtName.Text = host.DisplayName;
            TxtIp.Text = host.IpAddress;
            TxtPort.Text = host.Port.ToString();
            TxtComment.Text = host.Comment;
            // 根据当前默认值选中模式
            foreach (ComboBoxItem item in CboMode.Items)
            {
                if ((string)item.Tag == host.DefaultMode)
                {
                    CboMode.SelectedItem = item;
                    break;
                }
            }
            if (CboMode.SelectedItem == null) CboMode.SelectedIndex = 0;
            if (isEdit) Title = "编辑主机 - " + host.DisplayName;
            else Title = "新建主机";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtName.Text.Trim();
            string ip = TxtIp.Text.Trim();
            string portText = TxtPort.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("显示名称不能为空。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtName.Focus();
                return;
            }
            if (!Db.IsValidInternalIp(ip))
            {
                MessageBox.Show("IP 地址不合法，请输入有效的内网 IPv4 地址（如 192.168.1.10）。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtIp.Focus();
                return;
            }
            if (!int.TryParse(portText, out int port) || !Db.IsValidPort(port))
            {
                MessageBox.Show("端口不合法，请输入 1-65535 之间的整数。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPort.Focus();
                return;
            }

            Result.DisplayName = name;
            Result.IpAddress = ip;
            Result.Port = port;
            Result.Comment = TxtComment.Text.Trim();
            Result.DefaultMode = (CboMode.SelectedItem as ComboBoxItem)?.Tag as string ?? "FullControl";

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
