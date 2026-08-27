# Contributing to UVNC Address Book

Thank you for considering a contribution! This project is a small, focused LAN tool — we keep it
**simple, local, and dependency-light**. Please read this before opening a PR.

---

## 🧭 Principles (non-negotiable)

These are the reason this tool exists. Any PR that breaks them will not be merged:

1. **No network, ever.** No `HttpClient`, no sockets, no cloud sync, no telemetry, no update checks.
2. **No persisted VNC passwords.** The temp `.vnc` file may contain only host / port / viewonly.
3. **LAN-only.** IP validation stays restricted to private ranges.
4. **Single project, minimal deps.** Keep it to WPF + `Microsoft.Data.Sqlite`. Don't add frameworks
   (no MVVM toolkit, no DI container, no web stack) unless discussed first.

---

## 🛠️ Development setup

- **IDE:** Visual Studio 2022 with the **.NET 8 Desktop (WPF)** workload.
- **SDK:** .NET 8 SDK (for `dotnet build` / `dotnet run`).
- **Open:** `uvnc-address-book.sln` → Build → Run.
- On first build, NuGet restores `Microsoft.Data.Sqlite` (build-time only).
- First app launch prompts you to locate `uvncviewer.exe`.

---

## 🌿 Branch & PR workflow

1. **Fork** the repo and clone your fork.
2. Create a branch from `main`:
   - `feat/short-description` for new features
   - `fix/short-description` for bug fixes
   - `docs/...` for documentation
3. Make your change. Keep commits focused and messages clear.
4. **Build & run** to verify nothing breaks (especially the no-network invariants).
5. Open a **Pull Request** against `main` with:
   - What changed and why
   - How you tested it
   - Screenshots if it touches the UI

---

## ✅ Before you submit

- [ ] Project builds in `Release` with no new warnings
- [ ] No network-related code added (grep for `HttpClient`, `WebRequest`, `Socket`, `http`)
- [ ] `VncLauncher` still writes **no password** to the temp `.vnc` file
- [ ] IP validation unchanged (private ranges only)
- [ ] README / docs updated if behavior changed

---

## 🐞 Reporting issues

Open an issue with: Windows version, .NET runtime version, what you did, what happened, and (if safe)
the relevant steps to reproduce. **Do not paste real IPs, hostnames, or any sensitive data.**

---

## 💡 Good first issues

Look for the `good first issue` label. Current ideas:

- Multi-VNC-client launchers (TightVNC / RealVNC)
- Bulk CSV import
- Group expand/collapse state persistence
- Light/Dark theme toggle
- Keyboard shortcuts

---

## 🗣️ Code style

- C# with `var` where obvious; clear names; short comments only where non-trivial.
- XAML: keep it readable; prefer native WPF behavior over heavy custom control templates
  (custom `TreeViewItem`/`DataGridRow` templates have caused bugs before — see issue history).
- Chinese UI strings are fine; keep code identifiers in English.

---

---

# 中文贡献指南（简版）

欢迎参与贡献！本项目是一个轻量、纯本地的局域网工具，请遵守以下原则：

**不可违背的底线**
1. 绝不引入任何网络代码（HttpClient / Socket / 云同步 / 遥测 / 更新检查）。
2. 绝不持久化 VNC 密码；临时 `.vnc` 文件只能含 host / port / viewonly。
3. 仅限内网：IP 校验保持私有网段。
4. 单项目、最小依赖：仅 WPF + Microsoft.Data.Sqlite，勿擅自加框架。

**开发环境**
- Visual Studio 2022 + .NET 8 桌面(WPF) 工作负载，或 .NET 8 SDK。
- 打开 `uvnc-address-book.sln` → 生成 → 运行。首次启动需指定 `uvncviewer.exe`。

**提交流程**
1. Fork → 从 `main` 切分支（`feat/...` / `fix/...` / `docs/...`）。
2. 改动后本地生成 Release 验证无新增警告。
3. 提交 PR 到 `main`，说明改了什么、怎么测的、UI 改动附截图。

**提交前自检**
- [ ] Release 生成无新增警告
- [ ] 未新增任何网络相关代码
- [ ] VncLauncher 仍不写密码
- [ ] IP 校验仍为私有网段
- [ ] 行为变更已同步更新 README

**Issue 反馈**：请附 Windows 版本、.NET 运行时版本、复现步骤；**勿贴真实 IP / 主机名等敏感信息**。
