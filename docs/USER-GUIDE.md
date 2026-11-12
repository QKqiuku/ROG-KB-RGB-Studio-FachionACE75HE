# 使用指南 / User guide

## 中文

### 第一次运行

1. 确认键盘为有线 ROG Falchion Ace 75 HE ANSI83（`0B05:1C7D`），使用 Windows x64。
2. 根据[下载说明](DOWNLOADS.md)选 Full 或 Lite，解压完整目录。Full 的 `gifbakehost`、两版的 `sensorshost` 都必须保留。
3. 退出其它灯光控制程序，运行 `Start-Studio.cmd` 或 `RSS_II_RGB.App.exe`，手动确认管理员 UAC 提示。此权限用于提高管理员游戏里的按键触发兼容性，不保证所有游戏都可用。
4. 查看连接状态。预览为最后成功发送的帧，不是灯珠回读；显示器颜色与实体不同属正常现象。断开时显示未同步。

### 编排灯光

点击常规、音乐或动画预设开始播放。进入“自定义”以当前效果开始编排；它不会自动找回以前未保存的编排。选择或框选按键，分配区域效果；使用“区域灯效调整”修改适用参数。灯条与眼睛可跟随按键或独立编排；灯条可逐段分配。总体效果调节最终透明度。

在“我的灯效”填写名称并保存整套编排；“另存”建立新项，“加载”恢复保存内容，修改后再次保存可更新当前项。删除保存项不会立即改变当前播放。想保留重要编排，变更前先保存。

### 音乐、ATK 与 GIF

音乐来源可选系统或目标进程；进程采集需要 Windows build 20348+。目标未运行、受保护音频或系统策略可能导致等待或无信号。音乐仅在本机分析，不生成录音。

ATK 广场筛选已确认的当前配列、纯逐帧作品。先预览，再导入应用；预览不向键盘发送灯光。标题、作者与作品 ID 会保留，下载作品不等于获得再分发权利。

Full 可以导入 GIF、裁剪并转换。输入上限 64 MiB，转换有资源和时间限制，超限会失败。Lite 禁用新 GIF 导入；仍可播放已有 `.kbanim` 和已保存的 GIF 动画编排，以及 ATK 动画。Lite 缺少素材时不会凭空恢复素材；切换 Full/Lite 应保留用户数据目录。

### 退出、备份与排错

点 × 或最小化会进入托盘。要结束控制并尝试恢复原灯效，请使用“退出并恢复”或托盘 Exit，升级前也先退出。拔线、强制结束、断电或系统强制关闭可能打断恢复；当前没有 75HE 自动开机启动。

备份 `%LOCALAPPDATA%\RSS_II_RGB` 中的 `settings-falchion75.json`、`anims` 和 `user-presets`。不要只拷贝设置文件而丢失它引用的素材。分享配置前检查其中的本地文件路径、目标进程信息与作品再分发权利。日志在 `Logs`，反馈问题前检查其中可能包含的本地信息。

断开或效果异常时先检查 USB 连接、其它灯光软件和连接状态；音频异常时检查来源及信号。报告 Full/Lite、Windows 版本、键盘固件、复现步骤和相关日志。未签名预览包需在你的实体键盘及游戏环境测试；已有验证不覆盖全部硬件和固件。

## English

### First launch

1. Use a wired ROG Falchion Ace 75 HE ANSI83 (`0B05:1C7D`) on Windows x64.
2. Choose Full or Lite using the [download guide](DOWNLOADS.md) and extract everything. Preserve `sensorshost` in both versions and `gifbakehost` in Full.
3. Close other keyboard-lighting software, launch `Start-Studio.cmd` or `RSS_II_RGB.App.exe`, and confirm the administrator UAC prompt manually. Elevation improves key-trigger compatibility with elevated games but does not guarantee every game works.
4. Check connection status. Preview is the last successfully transmitted frame, not hardware readback; screen and physical colors can differ. A disconnected device is shown as unsynchronized.

### Compose lighting

Choose a regular, music or animation preset. Enter Custom to edit from the current effect; it does not automatically recover an old unsaved composition. Select or drag-select keys, assign a region effect and adjust its supported parameters in the region panel. Strip and eye lighting can follow keys or use independent effects; strip segments can be assigned individually. The overall effect controls final opacity.

Name and save a complete composition under My Effects. Save As creates another entry, Load restores it, and Save updates the current entry after edits. Deleting a saved entry does not immediately change playback. Save important work before replacing it.

### Audio, ATK and GIF

Select system audio or a target process. Process capture requires Windows build 20348+. Missing processes, protected audio and system policy may prevent capture. Analysis is local without recording.

The ATK gallery filters for confirmed matching layouts and pure keyframe works. Preview first, then import to apply; preview sends no hardware lighting. Titles, creators and work IDs remain. Downloading a work does not grant redistribution rights.

Full imports, crops and converts GIFs with a 64 MiB input cap plus resource and time limits. Lite disables new GIF import, but plays existing `.kbanim`, saved GIF compositions with available assets, and ATK animations. Preserve user assets when switching variants.

### Exit, backup and troubleshooting

Close/minimize hides the app to the tray. Use Exit and Restore or tray Exit to stop and attempt restoration, including before upgrades. Unplugging, forced termination, power loss or forced shutdown may interrupt restoration. Automatic startup is not provided for 75HE.

Back up `settings-falchion75.json`, `anims` and `user-presets` under `%LOCALAPPDATA%\RSS_II_RGB` together: settings reference asset files. Before sharing, review local paths, process information and artwork redistribution rights. Rotating logs are in `Logs`; review them for local information before posting.

Check USB connection, competing lighting software and status for output problems; check source and signal for audio problems. Report variant, Windows version, keyboard firmware, steps and relevant logs. This unsigned preview still requires testing on your own keyboard and game environment.
