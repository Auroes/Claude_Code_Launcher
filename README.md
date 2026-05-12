# Claude Code Launcher

一键启动 Claude Code 的 Windows 桌面工具。双击 `Claude_Code_Launcher.exe`，自动信任当前文件夹并进入 Claude Code。

## 功能

- **智能路径** — exe 放在哪个文件夹，就在哪个文件夹启动 Claude Code
- **自动信任** — 启动前通过 Python 直接修改 `%USERPROFILE%\.claude.json`，写入 `hasTrustDialogAccepted: true`，跳过安全确认弹窗
- **终端适配** — 优先 Windows Terminal（`wt.exe`），其次 PowerShell 7，最后回退 Windows PowerShell / ISE
- **圆形图标** — 深色圆底角色图标

## 工作原理

```
双击 exe
  ├─ ① FindPython() — 跳过 Microsoft Store 空壳，找真 Python（conda 等）
  ├─ ② 写临时 .py 脚本 → 运行 → 修改 .claude.json
  │      projects["当前路径"] = {"hasTrustDialogAccepted": True}
  │      同时写反斜杠和正斜杠两种格式
  ├─ ③ FindShell() — pwsh → powershell → ise → 裸名兜底
  └─ ④ wt.exe <shell> -NoExit -Command claude
```

## 项目结构

```
Claude_Code_Launcher/
├── src/
│   └── launcher.cs      # C# 源码
├── assets/
│   └── cc_logo.ico      # 图标文件
├── build.ps1             # 编译脚本
├── .gitignore
└── README.md
```

## 依赖

- **编译**：.NET Framework 4.x（Windows 自带 `csc.exe`）
- **运行**：Python（推荐 conda）+ PowerShell + Claude Code CLI

## 编译

```powershell
.\build.ps1
```

## 使用

1. 将 `Claude_Code_Launcher.exe` 复制到任意项目文件夹
2. 双击运行
3. 自动信任 → Windows Terminal 打开 → Claude Code 就绪

可创建桌面快捷方式或固定到任务栏。

## 技术要点

- C# 编译为 Windows GUI 程序（`/target:winexe`），无黑窗闪烁
- `FindPython()` 跳过 WindowsApps 的 Microsoft Store 空壳，支持 conda 等已知路径兜底
- Python 脚本写入临时文件后执行，避免 `-c` 模式的行长度和转义限制
- `json.dump(indent=2)` 保留 `.claude.json` 原有格式，不损坏其他数据
- 双格式路径（`\` 和 `/`）同时写入，兼容 Claude Code 的正则匹配
- 诊断日志输出到 exe 所在目录的 `launcher_debug.log`
