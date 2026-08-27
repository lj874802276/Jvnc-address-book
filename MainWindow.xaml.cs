using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace UvncAddressBook
{
    /// <summary>
    /// 主窗口：左侧分组树 + 右侧主机列表。纯本地运维工具，无任何网络请求。
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string KeyUvncPath = "UvncPath";
        private const string KeyWinLeft = "WinLeft";
        private const string KeyWinTop = "WinTop";
        private const string KeyWinWidth = "WinWidth";
        private const string KeyWinHeight = "WinHeight";
        private const string KeyWinState = "WinState";
        private const string KeyGridCols = "GridColWidths"; // 列宽记忆：DisplayName|Ip|Port|Mode

        // 建议的 UltraVNC Viewer 安装目录（仅作为首次选择框的初始目录，不强制）。
        private const string SuggestedUvncDir = @"D:\Program Files\uvnc bvba\UltraVNC";

        private int _currentGroupId = -1; // -1 = 全部主机
        private Host _dragHost;
        private List<Host> _allHosts = new List<Host>(); // 当前分组全部主机（未过滤）

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        // ---------- 窗口状态 ----------

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RestoreWindowState();
            EnsureUvncPathConfigured();
            LoadTree();
            LoadHosts(-1);
            RestoreColumnWidths();
        }

        /// <summary>
        /// 首次启动（或之前配置的路径失效）时，弹出文件选择框让用户手动选择 uvncviewer.exe。
        /// 选择后立即保存到 Settings，无需进入设置页即可生效；换电脑/非默认盘也能手动指定。
        /// </summary>
        private void EnsureUvncPathConfigured()
        {
            var path = Db.GetSetting(KeyUvncPath);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return; // 已配置且文件存在，跳过

            MessageBox.Show(
                "尚未配置 UltraVNC Viewer（uvncviewer.exe）路径，或之前的路径已失效。\n" +
                "点击“确定”后请在弹出的窗口中选择 uvncviewer.exe，保存后即可直接发起连接。",
                "配置 UltraVNC Viewer", MessageBoxButton.OK, MessageBoxImage.Information);

            var dlg = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择 uvncviewer.exe",
                InitialDirectory = SuggestedUvncDir
            };
            if (dlg.ShowDialog() == true)
            {
                Db.SetSetting(KeyUvncPath, dlg.FileName);
            }
        }

        private void RestoreWindowState()
        {
            try
            {
                var left = Db.GetSetting(KeyWinLeft, "");
                var top = Db.GetSetting(KeyWinTop, "");
                var w = Db.GetSetting(KeyWinWidth, "");
                var h = Db.GetSetting(KeyWinHeight, "");
                var state = Db.GetSetting(KeyWinState, "Normal");
                if (double.TryParse(left, out var l) && double.TryParse(top, out var t)
                    && double.TryParse(w, out var wd) && double.TryParse(h, out var ht)
                    && wd > 0 && ht > 0)
                {
                    Left = l; Top = t; Width = wd; Height = ht;
                }
                if (state == "Maximized") WindowState = WindowState.Maximized;
            }
            catch { /* 忽略，使用默认尺寸 */ }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Db.SetSetting(KeyWinState, WindowState == WindowState.Maximized ? "Maximized" : "Normal");
                if (WindowState == WindowState.Normal)
                {
                    Db.SetSetting(KeyWinLeft, Left.ToString());
                    Db.SetSetting(KeyWinTop, Top.ToString());
                    Db.SetSetting(KeyWinWidth, Width.ToString());
                    Db.SetSetting(KeyWinHeight, Height.ToString());
                }
                SaveColumnWidths();
            }
            catch { }
        }

        // ---------- 树（分组） ----------

        private void LoadTree()
        {
            var groups = Db.GetAllGroups();
            var nodes = new Dictionary<int, GroupNode>();
            foreach (var g in groups)
                nodes[g.Id] = new GroupNode { Id = g.Id, Name = g.GroupName };

            var rootChildren = new ObservableCollection<GroupNode>();
            foreach (var g in groups)
            {
                if (g.ParentId == -1 || !nodes.ContainsKey(g.ParentId))
                    rootChildren.Add(nodes[g.Id]);
                else
                    nodes[g.ParentId].Children.Add(nodes[g.Id]);
            }

            var root = new GroupNode { Id = -1, Name = "全部主机", IsRoot = true, Children = rootChildren };
            Tree.ItemsSource = new ObservableCollection<GroupNode> { root };

            Tree.UpdateLayout();
            if (Tree.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem tvi)
                tvi.IsSelected = true;
        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Tree.SelectedItem is GroupNode node)
            {
                _currentGroupId = node.Id;
                LoadHosts(_currentGroupId);
            }
        }

        private GroupNode CurrentNode => Tree.SelectedItem as GroupNode;

        /// <summary>
        /// 读取指定分组的全部主机（含“全部主机”根节点），保存到 _allHosts 后按搜索框过滤展示。
        /// </summary>
        private void LoadHosts(int groupId)
        {
            _allHosts = Db.GetHosts(groupId);
            ApplyFilter();
        }

        /// <summary>
        /// 按搜索关键字（名称/IP/备注）过滤当前分组主机并刷新表格与状态栏。
        /// </summary>
        private void ApplyFilter()
        {
            var kw = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            var filtered = string.IsNullOrWhiteSpace(kw)
                ? _allHosts
                : _allHosts.Where(h =>
                    (h.DisplayName ?? "").ToLowerInvariant().Contains(kw) ||
                    (h.IpAddress ?? "").ToLowerInvariant().Contains(kw) ||
                    (h.Comment ?? "").ToLowerInvariant().Contains(kw)).ToList();

            Grid.ItemsSource = filtered;
            StatusText.Text = string.IsNullOrWhiteSpace(kw)
                ? "共 " + _allHosts.Count + " 台主机"
                : "匹配 " + filtered.Count + " / " + _allHosts.Count + " 台主机";
            EmptyHint.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateHostButtons();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// 根据是否有选中主机，刷新主机操作按钮的可用状态；同时更新状态栏选中计数。
        /// </summary>
        private void UpdateHostButtons()
        {
            bool has = Grid.SelectedItem is Host;
            BtnEditHost.IsEnabled = has;
            BtnDeleteHost.IsEnabled = has;
            BtnConnectFull.IsEnabled = has;
            BtnConnectView.IsEnabled = has;
            int n = Grid.SelectedItems.Count;
            SelInfo.Text = n > 0 ? "已选 " + n + " 台" : "";
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateHostButtons();
        }

        private void MenuNewGroup_Click(object sender, RoutedEventArgs e)
        {
            int parentId = (CurrentNode != null && !CurrentNode.IsRoot) ? CurrentNode.Id : -1;
            var name = InputBox.Show("新建分组", "请输入分组名称：", "新分组");
            if (string.IsNullOrWhiteSpace(name)) return;
            Db.AddGroup(name.Trim(), parentId);
            LoadTree();
            StatusText.Text = "已新建分组：" + name;
        }

        private void MenuRenameGroup_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNode == null || CurrentNode.IsRoot)
            {
                MessageBox.Show("请先在左侧选择一个具体分组进行重命名。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var name = InputBox.Show("重命名分组", "请输入新的分组名称：", CurrentNode.Name);
            if (string.IsNullOrWhiteSpace(name)) return;
            Db.UpdateGroup(CurrentNode.Id, name.Trim());
            LoadTree();
            StatusText.Text = "已重命名分组：" + name;
        }

        private void MenuDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (  CurrentNode == null || CurrentNode.IsRoot)
            {
                MessageBox.Show("请先在左侧选择一个具体分组进行删除。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var result = MessageBox.Show(
                "确定要删除分组“" + CurrentNode.Name + "”吗？\n该分组及其所有子分组、分组下的全部主机都会被删除，且不可恢复。",
                "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            Db.DeleteGroupRecursive(CurrentNode.Id);
            LoadTree();
            StatusText.Text = "已删除分组";
        }

        private void MenuNewSubGroup_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNode == null || CurrentNode.IsRoot)
            {
                MessageBox.Show("请先选择一个分组，再在其下新建子分组。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var name = InputBox.Show("新建子分组", "请输入子分组名称：", "新分组");
            if (string.IsNullOrWhiteSpace(name)) return;
            Db.AddGroup(name.Trim(), CurrentNode.Id);
            LoadTree();
            StatusText.Text = "已新建子分组：" + name;
        }

        // ---------- 主机 ----------

        private void BtnAddHost_Click(object sender, RoutedEventArgs e)
        {
            int groupId = (_currentGroupId == -1) ? -1 : _currentGroupId;
            if (groupId == -1 && Db.GetAllGroups().Count == 0)
            {
                MessageBox.Show("请先创建至少一个分组，再添加主机。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var host = new Host { GroupId = groupId, Port = 5900, DefaultMode = "FullControl" };
            var dlg = new HostEditWindow(host, false);
            if (dlg.ShowDialog() == true)
            {
                Db.AddHost(host);
                LoadHosts(_currentGroupId);
                StatusText.Text = "已添加主机：" + host.DisplayName;
            }
        }

        private void BtnEditHost_Click(object sender, RoutedEventArgs e) => EditSelectedHost();
        private void CtxEditHost_Click(object sender, RoutedEventArgs e) => EditSelectedHost();

        private void EditSelectedHost()
        {
            if (Grid.SelectedItem is not Host host)
            {
                MessageBox.Show("请先选择一台主机。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dlg = new HostEditWindow(host, true);
            if (dlg.ShowDialog() == true)
            {
                Db.UpdateHost(host);
                LoadHosts(_currentGroupId);
                StatusText.Text = "已更新主机：" + host.DisplayName;
            }
        }

        private void BtnDeleteHost_Click(object sender, RoutedEventArgs e) => DeleteSelectedHost();
        private void CtxDeleteHost_Click(object sender, RoutedEventArgs e) => DeleteSelectedHost();

        private void DeleteSelectedHost()
        {
            if (Grid.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择要删除的主机。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var hosts = Grid.SelectedItems.Cast<Host>().ToList();
            var msg = hosts.Count == 1
                ? "确定要删除主机“" + hosts[0].DisplayName + "”吗？"
                : "确定要删除选中的 " + hosts.Count + " 台主机吗？";
            if (MessageBox.Show(msg, "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            foreach (var h in hosts) Db.DeleteHost(h.Id);
            LoadHosts(_currentGroupId);
            StatusText.Text = "已删除 " + hosts.Count + " 台主机";
        }

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Grid.SelectedItem is Host host)
                Connect(host, false); // 双击 = 完全控制连接
        }

        private void BtnConnectFull_Click(object sender, RoutedEventArgs e) => ConnectSelected(false);
        private void CtxConnectFull_Click(object sender, RoutedEventArgs e) => ConnectSelected(false);
        private void BtnConnectView_Click(object sender, RoutedEventArgs e) => ConnectSelected(true);
        private void CtxConnectView_Click(object sender, RoutedEventArgs e) => ConnectSelected(true);

        private void ConnectSelected(bool viewOnly)
        {
            if (Grid.SelectedItem is not Host host)
            {
                MessageBox.Show("请先选择一台主机。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Connect(host, viewOnly);
        }

        private void Connect(Host host, bool viewOnly)
        {
            string uvncPath = Db.GetSetting(KeyUvncPath, "");
            try
            {
                VncLauncher.Launch(uvncPath, host, viewOnly);
                StatusText.Text = "已发起连接：" + host.DisplayName + (viewOnly ? "（仅查看）" : "（完全控制）");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "无法启动连接", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- 导出 / 导入 ----------

        private void MenuExportAll_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON 备份文件 (*.json)|*.json",
                FileName = "uvnc_backup_" + DateTime.Now.ToString("yyyyMMdd") + ".json",
                Title = "导出全部主机与分组"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                ImportExport.ExportToFile(dlg.FileName, Db.GetAllGroups(), Db.GetHosts(-1));
                StatusText.Text = "已导出：" + dlg.FileName;
                MessageBox.Show("已导出全部主机与分组到：\n" + dlg.FileName, "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CtxExportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择要导出的主机（可多选）。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dlg = new SaveFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json",
                FileName = "uvnc_selected.json",
                Title = "导出选中的主机"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var selected = Grid.SelectedItems.Cast<Host>().ToList();
                ImportExport.ExportSelectedToFile(dlg.FileName, selected);
                StatusText.Text = "已导出 " + selected.Count + " 台主机";
                MessageBox.Show("已导出 " + selected.Count + " 台主机到：\n" + dlg.FileName, "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON 备份文件 (*.json)|*.json",
                Title = "导入主机与分组备份"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var data = ImportExport.ImportFromFile(dlg.FileName);
                var result = MessageBox.Show(
                    "导入将覆盖当前所有分组与主机数据，是否继续？",
                    "导入确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
                Db.ReplaceAll(data);
                LoadTree();
                LoadHosts(-1);
                StatusText.Text = "已导入备份：" + dlg.FileName;
                MessageBox.Show("导入完成。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入失败：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- 设置 / 关于 / 退出 ----------

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsWindow(Db.GetSetting(KeyUvncPath, ""));
            if (dlg.ShowDialog() == true)
            {
                Db.SetSetting(KeyUvncPath, dlg.UvncPath);
                StatusText.Text = "已保存 UltraVNC Viewer 路径";
            }
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "UVNC 地址簿管理器 v1.0\n\n" +
                "UltraVNC Viewer 外壳管理工具，仅用于局域网内网运维。\n" +
                "· 所有数据存储于本机 SQLite，不联网、不上报。\n" +
                "· 绝不保存 VNC 密码，连接时由 UltraVNC 自行弹出密码框。",
                "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

        // ---------- 拖拽归类 ----------

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Grid.SelectedItems.Count == 1)
                _dragHost = Grid.SelectedItem as Host;
        }

        private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _dragHost != null)
            {
                var data = new DataObject("uvnc_host", _dragHost);
                DragDrop.DoDragDrop(Grid, data, DragDropEffects.Move);
                _dragHost = null;
            }
        }

        private void Tree_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (_dragHost != null)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void Tree_Drop(object sender, DragEventArgs e)
        {
            if (_dragHost == null) return;
            var node = (e.OriginalSource as FrameworkElement)?.DataContext as GroupNode;
            if (node == null || node.IsRoot)
            {
                MessageBox.Show("请拖拽到具体的分组节点，不能放到“全部主机”。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Db.UpdateHostGroup(_dragHost.Id, node.Id);
            StatusText.Text = "已将主机移动到：" + node.Name;
            LoadHosts(_currentGroupId);
        }

        /// <summary>
        /// 在分组树上右键时，先选中被右键的节点，保证右键菜单操作作用于正确分组。
        /// </summary>
        private void Tree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is TreeViewItem))
                dep = VisualTreeHelper.GetParent(dep);
            if (dep is TreeViewItem tvi) tvi.IsSelected = true;
        }

        // ---------- 列宽记忆 ----------

        private void RestoreColumnWidths()
        {
            var saved = Db.GetSetting(KeyGridCols, "");
            if (string.IsNullOrWhiteSpace(saved)) return;
            var parts = saved.Split('|');
            var cols = new[] { ColDisplayName, ColIp, ColPort, ColMode };
            for (int i = 0; i < cols.Length && i < parts.Length; i++)
            {
                if (double.TryParse(parts[i], out var w) && w > 10)
                    cols[i].Width = new DataGridLength(w, DataGridLengthUnitType.Pixel);
            }
        }

        private void SaveColumnWidths()
        {
            var cols = new[] { ColDisplayName, ColIp, ColPort, ColMode };
            var vals = string.Join("|", cols.Select(c => c.ActualWidth.ToString("F0")));
            Db.SetSetting(KeyGridCols, vals);
        }
    }
}
