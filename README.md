# ROG KB RGB Studio

<img src="docs/images/app-icon.png" width="88" alt="ROG KB RGB Studio icon">

[English](README.en.md) · [下载与版本选择](docs/DOWNLOADS.md) · [使用指南](docs/USER-GUIDE.md) · [发布到 GitHub](docs/GITHUB-PUBLISH.md)

面向有线 **ROG Falchion Ace 75 HE，ANSI 83 键，VID:PID `0B05:1C7D`** 的非官方 Windows 灯效工作台。控制 83 键、顶部 12 段灯条及眼睛灯区。基于 [bbfox0703/RSS_II_RGB](https://github.com/bbfox0703/RSS_II_RGB) 扩展，沿用 GPL-3.0；与 ASUS、ROG、ATK 无隶属或背书关系。应用使用自有键帽标记和通用眼睛图形；设备名称仅用于说明兼容性，未宣称获得品牌授权。

## 下载与启动

在你正在浏览的仓库打开 **Releases**，选择 Full 或 Lite：两者均为 Windows 10/11 x64 自包含包，无需安装 .NET。Full 包含 GIF 转换组件；Lite 不包含 GIF 转换组件，禁用新 GIF 导入，但仍播放已有 `.kbanim` 和 ATK 动画。其它灯效、进程音频、按键、灯条及自定义编排功能相同。文件大小以实际 Release 附件为准。

完整解压后，退出其它控制键盘灯光的软件，运行 `Start-Studio.cmd` 或 `RSS_II_RGB.App.exe`。程序默认选择 75HE，启动需要你手动确认管理员 UAC 提示。点 × 或最小化会隐藏至托盘；真正退出请使用“退出并恢复”或托盘 Exit。当前没有 75HE 开机自启动。升级前先正常退出旧版。

## 平台支持

| 平台 / 架构 | 当前状态 |
| --- | --- |
| Windows 10/11 x64 | 当前提供 Full / Lite 预览包；验证范围见下方说明 |
| Windows ARM64 | 暂未提供原生包，未验证 x64 模拟运行 |
| Windows x86（32 位） | 暂未适配或提供包；当前工程及原生依赖按 x64 构建 |
| macOS / Linux | 暂不支持，未提供安装包 |

Avalonia 界面可跨平台，但本项目的 HID 设备通信、按键监听、音频采集和管理员启动目前使用 Windows 实现。其它平台需要完成平台适配、原生依赖检查和实机测试，不能只更换打包目标。未承诺其它平台的发布时间。.NET 10 本身支持 Windows x86；本项目没有 x86 包不代表运行时不支持该架构（[官方平台说明](https://learn.microsoft.com/en-us/dotnet/core/install/windows#supported-versions)）。

## 功能与边界

- 常规、按键交互、音乐联动、GIF/ATK 动画；区域独立参数、灯条/眼睛编排、命名保存整套自定义灯效。
- 预设模式播放所选预设；进入自定义时以当前效果开始编辑。15/30/45/60 FPS 可选；实际发送速度受设备与负载影响。
- ATK 广场只显示当前已确认配列的纯逐帧作品，保留作者信息；预览不会改动实体灯光。远端接口变化或分页中断可能影响结果。
- 系统音频及目标进程音频在本机分析，不保存录音。指定进程采集需要 Windows build 20348+；受保护音频可能无法采集。
- 简体中文、繁体中文、英文随系统语言选择。预览显示最后成功发送的数据，并非硬件回读。

配置位于 `%LOCALAPPDATA%\RSS_II_RGB\settings-falchion75.json`，素材位于同目录 `anims`、`user-presets`，日志位于 `Logs`。覆盖软件目录不清除这些数据。按键监听只用于灯效位置与触发，不保存输入文字；其它键盘也可能触发对应位置。

此版本是**未签名的公开预览**。已有用户测试仅覆盖其本机键盘、固件和环境；不代表所有固件、游戏或电脑都验证过。管理员启动曾在用户本机恢复《无畏契约》按键特效，兼容性不作普遍保证。断开设备、强制结束、断电或系统强制关机可能中断恢复；正常退出会尝试恢复原灯效。程序不修改磁轴触发、回报率或固件。每个新发行包仍需实体键盘及无 .NET 环境验证。

## 构建与分发

需要 .NET 10 SDK，打包需要 PowerShell 7.2+：

```powershell
dotnet restore ROG-KB-RGB-Studio.slnx -p:PublishAot=false
dotnet build ROG-KB-RGB-Studio.slnx -t:Rebuild -c Release -p:PublishAot=false --no-restore
dotnet test ROG-KB-RGB-Studio.slnx -c Release --no-build
pwsh -File tools/Publish-Studio.ps1
```

脚本生成 Full、Lite、对应源码和 SHA-256 校验文件；拒绝覆盖已有输出，不会自动上传。实际发行采用自包含、未裁剪构建；旧 Native AOT 脚本仅作参考。

分发时保留 [LICENSE](LICENSE)、[NOTICE.md](NOTICE.md)、第三方许可资料，同时提供对应源码。仅沿用原仓库的基础工程、程序集和共享命名空间保留 `RSS_II_RGB`；新解决方案、应用测试和发布产物使用项目新名称。用户数据沿用旧目录以兼容已有配置。GitHub 自动源码只包含标签所指提交：新文件和未提交修改必须先完整提交。[发布指南](docs/GITHUB-PUBLISH.md) 提供新建独立仓库、提交、上传及 Release 步骤。

详细资料：[架构](docs/ARCHITECTURE.md)、[ATK](docs/ATK-GALLERY.md)、[音乐](docs/MUSIC-AUDIO.md)、[75HE 协议](docs/FALCHION75.md)、[更新记录](docs/RELEASE-NOTES.md)。
