# 下载与版本选择 / Downloads

[中文首页](../README.md) · [English](../README.en.md)

## 中文

在当前仓库的 **Releases** 下载，公开预览版本会标记 Pre-release。实际压缩包大小请看附件，不要按开发目录大小估算。

| 附件 | 用途 |
| --- | --- |
| `ROG-KB-RGB-Studio-win-x64-full.zip` | 完整版：包括新 GIF 导入及转换组件 |
| `ROG-KB-RGB-Studio-win-x64-lite.zip` | 精简版：不含 `gifbakehost`，禁用新 GIF 导入；保留已有 `.kbanim` 和 ATK 播放 |
| `ROG-KB-RGB-Studio-source.zip` | 与二进制配套的源码，用于检查、修改和构建，不是可直接运行的软件 |
| `SHA256SUMS.txt` | 两个程序包及源码包的 SHA-256 校验值 |

两版均为 Windows 10/11 x64 自包含包，不需要安装 .NET，均支持进程音频、按键灯效、灯条/眼睛、区域编辑与命名编排。仅支持已声明的有线 75HE ANSI83 配列。Windows 进程音频需要 build 20348+；旧版本可选系统音频。

解压完整目录后运行 `Start-Studio.cmd` 或主程序，手动确认 UAC。需要新 GIF 转换时请下载 Full。两版用户配置目录相同，切换版本前先正常退出当前程序，不要同时运行。

可在 PowerShell 检查文件完整性，将输出与同一 Release 的 `SHA256SUMS.txt` 对照：

```powershell
Get-FileHash -LiteralPath .\ROG-KB-RGB-Studio-win-x64-full.zip -Algorithm SHA256
```

将文件名替换成你实际下载的 Lite 或源码包。校验值检查传输完整性，不代表程序已代码签名。不要从未知来源接受新的校验文件。

## English

Download from **Releases** in this repository. Preview versions are marked Pre-release. Read actual asset sizes there.

| Asset | Purpose |
| --- | --- |
| `ROG-KB-RGB-Studio-win-x64-full.zip` | Full: includes new GIF import and its conversion helper |
| `ROG-KB-RGB-Studio-win-x64-lite.zip` | Lite: omits `gifbakehost` and disables new GIF import; retains existing `.kbanim` and ATK playback |
| `ROG-KB-RGB-Studio-source.zip` | Corresponding source for inspection, modification and builds; not a runnable app |
| `SHA256SUMS.txt` | SHA-256 hashes for both application archives and source |

Both application packages are self-contained Windows 10/11 x64 builds with the same process audio, key effects, strip/eye lighting, region editing and named compositions. Only the declared wired 75HE ANSI83 layout is supported. Process audio requires Windows build 20348+; older Windows can use system audio.

Extract the entire directory and launch `Start-Studio.cmd` or the application, confirming UAC manually. Download Full to convert new GIFs. Both variants share the same user settings location: exit one normally before switching and never run them together.

Run the PowerShell hash command above, substituting your downloaded filename, and compare with `SHA256SUMS.txt` from the same release. Hashes check file integrity; the program remains unsigned.
