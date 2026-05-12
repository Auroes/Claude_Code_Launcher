# Claude Code Launcher

一键启动 Claude Code 的 Windows 桌面工具。双击 `Claude_Code_Launcher.exe` 自动打开 PowerShell 7 并进入 Claude Code，同时自动信任当前文件夹。

## 功能

- **智能路径** — exe 放在哪个文件夹，就在哪个文件夹启动 Claude Code
- **自动信任** — 启动前自动将当前文件夹标记为已信任，跳过安全确认弹窗
- **圆形图标** — 深色圆底角色图标，桌面/任务栏清晰可辨

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
- **运行**：PowerShell 7（`pwsh.exe` 需在 PATH 中）+ Claude Code CLI

## 编译

```powershell
.\build.ps1
```

产出 `Claude_Code_Launcher.exe`。

## 使用

1. 将 `Claude_Code_Launcher.exe` 复制到任意项目文件夹
2. 双击运行
3. PowerShell 窗口打开，自动执行 `claude`

可创建桌面快捷方式或固定到任务栏方便日常使用。

## 技术要点

- C# 编译为 Windows GUI 程序（`/target:winexe`），无黑窗闪烁
- `System.Web.Script.Serialization.JavaScriptSerializer` 直接读写 `.claude.json`，在 pwsh 启动前完成信任注入
- 图标通过 `/win32icon` 嵌入 PE 资源段
- 信任写入失败不阻塞 Claude Code 启动
