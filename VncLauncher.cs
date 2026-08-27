using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace UvncAddressBook
{
    /// <summary>
    /// 调用本地 UltraVNC Viewer 启动会话。
    /// 关键约束：绝不把密码写入配置；每次连接由 UltraVNC 自行弹出密码输入框。
    /// </summary>
    public static class VncLauncher
    {
        /// <summary>
        /// 生成临时 .vnc 配置并启动 uvncviewer.exe。
        /// </summary>
        /// <param name="uvncPath">用户在设置中配置的 UltraVNC Viewer 可执行文件路径</param>
        /// <param name="host">目标主机</param>
        /// <param name="viewOnly">是否仅查看</param>
        public static void Launch(string uvncPath, Host host, bool viewOnly)
        {
            if (string.IsNullOrWhiteSpace(uvncPath))
                throw new InvalidOperationException(
                    "尚未配置 UltraVNC Viewer 路径，请先到“设置”中指定 uvncviewer.exe 的位置。");

            if (!File.Exists(uvncPath))
                throw new FileNotFoundException(
                    "找不到 UltraVNC Viewer 程序：\n" + uvncPath + "\n请在“设置”中确认路径是否正确。", uvncPath);

            // 在系统临时目录生成临时 .vnc 配置；绝不写入 password 字段
            string tmpFile = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "uvnc_" + Guid.NewGuid().ToString("N") + ".vnc");

            // UltraVNC 的 .vnc 配置格式：连接信息与选项；ViewOnly 控制仅查看模式
            string content =
                "[Connection]\n" +
                "Host=" + host.IpAddress + "\n" +
                "Port=" + host.Port + "\n" +
                "\n" +
                "[Options]\n" +
                "ViewOnly=" + (viewOnly ? 1 : 0) + "\n";

            System.IO.File.WriteAllText(tmpFile, content);

            var psi = new ProcessStartInfo
            {
                FileName = uvncPath,
                Arguments = "-config \"" + tmpFile + "\"",
                UseShellExecute = false
            };

            Process proc;
            try
            {
                proc = Process.Start(psi);
            }
            catch (Exception ex)
            {
                try { System.IO.File.Delete(tmpFile); } catch { }
                throw new InvalidOperationException("启动 UltraVNC Viewer 失败：\n" + ex.Message);
            }

            // 进程退出后再删除临时配置文件（后台等待，避免阻塞 UI）
            if (proc != null)
            {
                Task.Run(() =>
                {
                    try { proc.WaitForExit(); } catch { }
                    try { System.IO.File.Delete(tmpFile); } catch { }
                });
            }
            else
            {
                try { System.IO.File.Delete(tmpFile); } catch { }
            }
        }
    }
}
