# UVNC Address Book

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4.svg)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF-68217A.svg)](https://learn.microsoft.com/dotnet/desktop/wpf/)

> **Radmin-style phonebook for UltraVNC — LAN-only, zero network, no saved passwords.**
> 一款纯本地、零联网的 Windows 桌面工具，作为 **UltraVNC Viewer** 的外壳管理工具，复刻 Radmin Viewer「主机电话簿」体验：只管理主机列表，不渲染远程桌面画面，仅调用本地 `uvncviewer.exe` 发起 VNC 会话。

Built for internal-network Ops — hospital check-up centers, server rooms, campus PCs — where you need a tidy, grouped host list and one-click connect, with **no cloud, no telemetry, no internet**.

---

## ✨ Features / 功能特性

- **Grouped tree** — multi-level folders (e.g. `Checkup Dept` / `Office PCs` / `Server Room`); create, rename, delete (recursive), and drag-drop hosts between groups.
- **Host management** — add / edit / delete hosts with fields: display name, LAN IP, port, default mode (Full Control / View Only), comment.
- **One-click connect** — double-click = Full Control; right-click menu offers Full Control / View Only / Edit / Delete / Export selected.
- **Temp-config launch** — writes a short-lived `*.vnc` file (IP, port, viewonly flag) to the temp dir, calls `uvncviewer.exe -config`, and deletes it after exit. **Password is never written.**
- **Import / Export** — full or selected hosts to JSON backup; import JSON to restore (great for migrating to a new machine).
- **Settings** — configure `uvncviewer.exe` path; window size/position and DataGrid column widths are remembered.
- **Validation & safety** — IP format + port range (1–65535) + private-network check; friendly prompt when `uvncviewer.exe` is missing.
- **100% local** — all data in a local SQLite file (`uvnc_addressbook.db`, next to the exe). **No HTTP / gRPC / HttpClient, no telemetry, no update checks.**

**功能特性（中文）：**
- **分组树** — 支持多级文件夹（如 `体检科` / `办公电脑` / `机房`）；可新建、重命名、删除（递归）、并把主机拖拽到不同分组。
- **主机管理** — 增 / 改 / 删主机，字段含：显示名、内网 IP、端口、默认模式（完全控制 / 仅观看）、备注。
- **一键连接** — 双击 = 完全控制；右键菜单提供「完全控制 / 仅观看 / 编辑 / 删除 / 导出选中」。
- **临时配置启动** — 向临时目录写入一个短暂存在的 `*.vnc` 文件（仅含 IP、端口、仅观看标志），调用 `uvncviewer.exe -config`，进程退出后自动删除。**绝不写入密码。**
- **导入 / 导出** — 全部或选中主机可导出为 JSON 备份；导入 JSON 即可恢复（便于迁移到新机器）。
- **设置** — 配置 `uvncviewer.exe` 路径；窗口大小/位置以及表格列宽都会被自动记住。
- **校验与安全** — IP 格式 + 端口范围（1–65535）+ 内网网段校验；找不到 `uvncviewer.exe` 时给出友好提示。
- **100% 本地** — 所有数据存于本地 SQLite 文件（`uvnc_addressbook.db`，与程序同目录）。**无 HTTP / gRPC / HttpClient，无遥测，无更新检查。**

---

## 📸 Screenshots / 截图

![Main UI](docs/screenshot-main.png)

> Main window: grouped host tree on the left, searchable host grid on the right. Double-click a host to launch UltraVNC Viewer in Full Control mode.
> 主窗口：左侧为分组主机树，右侧为可搜索的主机表格。双击某台主机即可以「完全控制」模式启动 UltraVNC Viewer。

---

## 🎯 Why this exists / 设计初衷

UltraVNC's built-in viewer has no proper "phonebook" for organizing many internal hosts. Radmin Viewer had a great one; this tool reproduces that experience as a **thin shell** — it never touches the remote framebuffer, it just manages your host list and hands off to `uvncviewer.exe`. Perfect for closed LAN environments that must not touch the internet.

**中文说明：**
UltraVNC 自带查看器没有合适的「电话簿」来管理大量内网主机；而 Radmin Viewer 曾经有一套很好的方案。本工具把这种体验复刻为一个**轻量外壳**——它从不触碰远端画面缓冲区，只负责管理你的主机列表，并把连接动作交给本地的 `uvncviewer.exe`。非常适合绝对不能联网的封闭局域网环境。

---

## 🚀 Quick Start / 快速开始

1. Open **`uvnc-address-book.sln`** in Visual Studio 2022 (or `dotnet build -c Release`).
2. On first build, NuGet restores `Microsoft.Data.Sqlite` **once** (build-time only; runtime is fully offline).
3. Run the app:
   - First launch shows a **"Select uvncviewer.exe"** dialog — point it at your local `uvncviewer.exe` (e.g. `D:\Program Files\uvnc bvba\UltraVNC\uvncviewer.exe`). Saved immediately; editable later in Settings.
   - A `uvnc_addressbook.db` is auto-created next to the exe on first run.
4. Create a group → add a host (e.g. `Phlebotomy-03`, `192.168.1.23`, port `5900`) → double-click to connect.

**中文步骤：**
1. 用 **Visual Studio 2022** 打开 `uvnc-address-book.sln`（或在命令行执行 `dotnet build -c Release`）。
2. 首次构建时，NuGet 会**仅一次**还原 `Microsoft.Data.Sqlite`（只在构建期联网；运行时完全离线）。
3. 运行程序：
   - 首次启动会弹出「选择 uvncviewer.exe」对话框——指向你本机的 `uvncviewer.exe`（例如 `D:\Program Files\uvnc bvba\UltraVNC\uvncviewer.exe`）。会立即保存，之后可在「设置」中修改。
   - 首次运行会在程序同目录自动创建 `uvnc_addressbook.db`。
4. 新建一个分组 → 添加一台主机（例如 `采血台-03`、`192.168.1.23`、端口 `5900`）→ 双击即可连接。

---

## 🖥️ Usage / 使用方法

| 任务 Task | 操作 How |
|---|---|
| 新建 / 重命名 / 删除分组 | 左侧工具栏按钮，或右键分组树（删除为递归且会确认） |
| New / rename / delete group | Left toolbar buttons, or right-click the tree (delete is recursive + confirms) |
| 添加 / 编辑 / 删除主机 | 顶部工具栏，或右键某行（删除会确认） |
| Add / edit / delete host | Top toolbar, or right-click a row (confirm on delete) |
| 主机换组 | 把主机行拖拽到目标分组节点上 |
| Re-group a host | Drag a host row onto a target group node |
| 搜索 | 右上角搜索框，按名称 / IP / 备注过滤（仅本地） |
| Search | Top-right search box filters by name / IP / comment (local only) |
| 连接 | 双击 = 完全控制；右键 → 仅观看。密码由 UltraVNC 自行弹出——**从不保存** |
| Connect | Double-click = Full Control; right-click → View Only. Password prompt is shown by UltraVNC itself — **never stored** |
| 备份 / 迁移 | 菜单 `文件 → 导出全部`（JSON）；`文件 → 导入` 恢复 |
| Backup / migrate | Menu `File → Export All` (JSON); `File → Import` to restore |

---

## 🏗️ Architecture / 架构

Clean, single-project WPF (.NET 8). No MVVM framework, no external services — just `Microsoft.Data.Sqlite`.

```
uvnc-address-book/
├── uvnc-address-book.sln        # Solution
├── uvnc-address-book.csproj     # net8.0-windows + UseWPF, only Microsoft.Data.Sqlite
├── App.xaml(.cs)                # Startup + global styles (auto-creates DB on launch)
├── MainWindow.xaml(.cs)         # Group tree + host grid, drag-drop, search, context menus
├── HostEditWindow.xaml(.cs)     # Host add/edit dialog (with validation)
├── SettingsWindow.xaml(.cs)     # uvncviewer.exe path + window-state memory
├── InputBox.xaml(.cs)           # Generic text-input dialog (group name)
├── Db.cs                        # SQLite schema bootstrap + CRUD (Groups / Hosts / Settings)
├── Models.cs                    # Host / GroupNode models
├── VncLauncher.cs               # Temp .vnc config writer + process launcher (no password)
├── ImportExport.cs              # JSON export / import
└── uvnc_addressbook.db          # Auto-generated at runtime (gitignored)
```

**Key invariants (contributors, please keep these):**
- 🚫 No network code anywhere. No `HttpClient`, no sockets, no cloud sync.
- 🔒 VNC passwords are never persisted — the temp `.vnc` file contains **only** host/port/viewonly.
- 🏠 IP validation restricts to private ranges (10/8, 172.16/12, 192.168/16, 169.254/16).

**核心约束（贡献者请务必遵守）：**
- 🚫 任何位置都不允许有联网代码。没有 `HttpClient`、没有 socket、没有云同步。
- 🔒 VNC 密码绝不持久化——临时 `.vnc` 文件**只**包含 主机 / 端口 / 仅观看。
- 🏠 IP 校验仅限私有网段（10/8、172.16/12、192.168/16、169.254/16）。

---

## 🛠️ Build & Run (developers) / 构建与运行（开发者）

```bash
# Visual Studio 2022 → open uvnc-address-book.sln → Build Solution
# or CLI (requires .NET 8 SDK):
dotnet build -c Release
dotnet run -c Release
```

- Target framework: `net8.0-windows`
- UI: WPF (`UseWPF`)
- Dependency: `Microsoft.Data.Sqlite` only
- Nothing web / network / cloud related

**构建说明（中文）：**
- 目标框架：`net8.0-windows`
- 界面：WPF（`UseWPF`）
- 依赖：仅有 `Microsoft.Data.Sqlite`
- 与 Web / 网络 / 云 完全无关

---

## 🔒 Security & Compliance / 安全与合规

- **Zero network** — no requests, telemetry, or update checks. Safe for air-gapped LANs.
- **No persisted passwords** — the VNC password is entered in UltraVNC's own prompt every time.
- **LAN-only** — private-network IP enforcement; no P2P / NAT traversal / cloud sync.
- **Your data stays yours** — everything lives in a local SQLite file; back it up yourself.

**安全说明（中文）：**
- **零联网** — 无任何请求、遥测或更新检查。可用于物理隔离的局域网。
- **不保存密码** — VNC 密码每次都由 UltraVNC 自身弹窗输入。
- **仅限内网** — 强制私有网段 IP；无 P2P / NAT 穿透 / 云同步。
- **数据归你所有** — 全部存于本地 SQLite 文件，请自行备份。

---

## 🗺️ Roadmap / 路线图

Help wanted on these (see [CONTRIBUTING.md](./CONTRIBUTING.md)):

- [ ] Multi-VNC-client support (TightVNC / RealVNC launchers)
- [ ] Bulk host import from CSV
- [ ] Group tree expand/collapse state persistence
- [ ] Optional AES-encrypted connection profiles (still no cloud)
- [ ] Light / Dark theme switch
- [ ] Keyboard shortcuts for connect actions

**欢迎认领（参见 [CONTRIBUTING.md](./CONTRIBUTING.md)）：**
- [ ] 支持多款 VNC 客户端（TightVNC / RealVNC 启动器）
- [ ] 从 CSV 批量导入主机
- [ ] 分组树的展开/折叠状态持久化
- [ ] 可选的 AES 加密连接配置（依然无云）
- [ ] 浅色 / 深色主题切换
- [ ] 连接操作的键盘快捷键

---

## 🤝 Contributing / 贡献指南

Contributions are welcome! Whether it's a bug fix, a translation, or a new feature — please read
**[CONTRIBUTING.md](./CONTRIBUTING.md)** first. Good first issues are labeled `good first issue`.

1. Fork → create a branch (`feat/...`, `fix/...`)
2. Build with VS2022 / `dotnet build`
3. Open a PR with a clear description

**中文：**
欢迎各种贡献！无论是修 bug、翻译还是新功能——请先阅读 **[CONTRIBUTING.md](./CONTRIBUTING.md)**。适合新手的 issue 带有 `good first issue` 标签。

1. Fork 仓库 → 新建分支（`feat/...`、`fix/...`）
2. 用 VS2022 / `dotnet build` 构建
3. 提交 PR 并写清描述

---

## 📄 License / 许可证

[MIT](./LICENSE) © UVNC Address Book contributors.

[MIT](./LICENSE) 许可证 © UVNC Address Book 贡献者。

---

## 🏷️ Suggested GitHub Topics / 推荐主题标签

Add these on the repo's **About** page to maximize discoverability:

`vnc` · `ultravnc` · `remote-desktop` · `wpf` · `dotnet` · `windows` · `lan` · `intranet` · `address-book` · `phonebook` · `sysadmin` · `offline` · `sqlite`

在仓库 **About** 页面添加以下主题标签可提升曝光度：

`vnc` · `ultravnc` · `remote-desktop` · `wpf` · `dotnet` · `windows` · `lan` · `intranet` · `address-book` · `phonebook` · `sysadmin` · `offline` · `sqlite`
