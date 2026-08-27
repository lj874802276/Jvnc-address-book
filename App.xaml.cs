using System;
using System.Windows;

namespace UvncAddressBook
{
    /// <summary>
    /// 应用程序入口。无任何启动联网逻辑，仅初始化本地数据库后进入主窗口。
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // 首次启动自动建库建表；数据库文件位于 exe 同目录
                Db.Init();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "初始化本地数据库失败：\n" + ex.Message,
                    "UVNC 地址簿管理器",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
