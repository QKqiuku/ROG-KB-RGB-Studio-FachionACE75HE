# 75HE 公开预览 / 75HE public preview

## 中文

准备日期：2026-11-12。构建脚本不会自动上传；标签工作流按仓库配置创建预发布。

- 面向有线 Falchion Ace 75 HE（`0B05:1C7D`），ANSI83；包含 83 键、12 段灯条及眼睛灯区。
- Full 和 Lite 均为 Windows x64 自包含、未裁剪构建，无需安装 .NET。Full 包含 GIF 转换；Lite 仅移除转换组件并禁用新 GIF 导入，保留已有 `.kbanim`、保存的可用动画和 ATK 播放。其它功能相同。
- 常规、按键与音乐效果；区域独立参数、灯条/眼睛编辑、命名编排、匹配配列的纯逐帧 ATK 广场。
- 启动默认 75HE，管理员 UAC 需用户手动确认；没有 75HE 开机自启动。
- 项目标记采用自有键帽图形，眼睛采用通用图形；设备品牌名称用于说明兼容性。保留 GPL-3.0 上游来源和第三方许可资料。
- 正弦分区的多色旧项迁移至合并效果；删除预设有兼容回退。已有配置与命名编排继续使用用户数据目录。

过去用户测试确认其本机的键位映射、灯条/眼睛输出、静态/波浪效果及示例 ATK 动画；管理员启动在其本机恢复了《无畏契约》的按键交互。这不替代本次 Full/Lite 的最终实体键盘、固件和无 .NET 系统验证。程序未签名；强制终止、拔线和断电可能中断恢复。

附件、实际大小与校验值见 Release：[版本选择](DOWNLOADS.md)。源码与二进制应同时提供；标签所指提交必须包含所有新增文件，不能只引用旧 `a838070` 基线。[发布步骤](GITHUB-PUBLISH.md)。

## English

Prepared 2026-11-12. The packaging script does not upload; the tag workflow creates a prerelease when configured/enabled in the repository.

- Wired Falchion Ace 75 HE (`0B05:1C7D`), ANSI83: 83 keys, 12 strip segments and eye lighting.
- Full and Lite are self-contained, untrimmed Windows x64 builds without an installed .NET requirement. Full includes GIF conversion. Lite removes that helper and disables new GIF import while retaining existing `.kbanim`, saved animations with available assets, and ATK playback. Other functionality is the same.
- Built-in, key-reactive and music effects; independent region parameters; strip/eye editing; named compositions; matching-layout pure-keyframe ATK gallery.
- Default 75HE selection; manual administrator UAC confirmation; no 75HE automatic startup.
- Custom keycap project mark and generic eye graphic; device brands identify compatibility. Upstream GPL-3.0 attribution and third-party notices are retained.
- Legacy multicolor sine sectors migrate into the merged effect; removed presets have compatibility fallbacks. Existing settings and named compositions retain their user data location.

Previous user tests covered key mapping, strip/eye output, static/wave effects and a sample ATK animation on that user's hardware. Elevation restored VALORANT key reactions there. This does not replace final Full/Lite physical-keyboard, firmware and clean-machine tests. The application remains unsigned; forced termination, unplugging and power loss can interrupt restoration.

See the Release for actual asset sizes and hashes and the [download guide](DOWNLOADS.md) for variant selection. Supply source beside binaries and commit every new file before tagging; the old `a838070` baseline alone does not represent this release. Follow the [publishing guide](GITHUB-PUBLISH.md).
