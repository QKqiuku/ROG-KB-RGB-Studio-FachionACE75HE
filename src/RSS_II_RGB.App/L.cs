using RSS_II_RGB.Core.Localization;

namespace RSS_II_RGB.App;

/// <summary>
/// UI string table. The language is chosen once at startup from the OS and never
/// changes during a session, so each string is just a language switch — no
/// ResourceManager, satellite assemblies, or reflection (AOT-safe). Brand names
/// (e.g. "Strix Scope II RGB") are intentionally not translated.
/// </summary>
internal static partial class L
{
    /// <summary>Set once at startup (see <see cref="Program"/>).</summary>
    public static AppLanguage Language { get; set; } = AppLanguage.English;

    private static string S(string en, string traditional, string simplified) => Language switch
    {
        AppLanguage.TraditionalChinese => traditional,
        AppLanguage.SimplifiedChinese => simplified,
        _ => en,
    };


    // ----- Lighting workbench -----
    public static string WorkbenchTitle => AppConstants.ProductName;
    public static string GlobalBrightnessLabel => S("Base effect brightness", "基礎效果亮度", "基础灯效亮度");
    public static string KeyboardSelectionTitle => S("Keyboard selection", "鍵盤選區", "键盘选区");
    public static string KeyboardSelectionHint => S("Click keys, or drag from empty space to select a group.", "點選按鍵，或從空白處拖曳框選。", "点选按键，或从空白处拖动框选。");
    public static string LivePreview => S("Live preview", "即時預覽", "实时预览");
    public static string LiveFpsFormat => S("Live · {0:0.0} FPS", "即時 · {0:0.0} FPS", "实时 · {0:0.0} FPS");
    public static string TargetFpsFormat => S("Target {0} FPS", "目標 {0} FPS", "目标 {0} FPS");
    public static string TargetFrameRateLabel => S("Target FPS", "目標幀率", "目标帧率");
    public static string PresetModeHint => S("Preset only · custom layers paused", "預設模式 · 自訂圖層已暫停", "预设模式 · 自定义层已暂停");
    public static string CustomModeHint => S("Custom · base effect + layers", "自訂模式 · 基礎效果＋圖層", "自定义模式 · 基础光效＋图层");
    public static string LightingEffects => S("Lighting effects", "燈光效果", "灯光效果");
    public static string RegularEffects => S("Presets", "常規燈效", "常规灯效");
    public static string AnimatedEffects => S("Animations / GIF", "動效 / GIF", "动效 / GIF");
    public static string CustomMode => S("Custom", "自訂", "自定义");
    public static string EditCustomLayers => S("Edit layers", "編輯圖層", "编辑图层");
    public static string EffectParameters => S("Effect parameters", "效果參數", "光效参数");
    public static string SpeedLabel => S("Speed", "速度", "速度");
    public static string WidthLabel => S("Width / range", "寬度／範圍", "宽度／范围");
    public static string DensityLabel => S("Density", "密度", "密度");
    public static string DirectionLabel => S("Direction", "方向", "方向");
    public static string SaturationLabel => S("Saturation", "飽和度", "饱和度");
    public static string RainbowColorsLabel => S("Rainbow colours", "彩虹配色", "彩虹配色");
    public static string PaletteLabel => S("Colour palette", "調色盤", "调色板");
    public static string CustomBaseLabel => S("Base preset", "基礎預設效果", "基础预设光效");
    public static string BaseBrightnessHint => S("Brightness affects the base effect; custom layers keep their own intensity.", "亮度影響基礎效果；自訂圖層保留自己的強度。", "亮度影响基础光效；自定义层保留自己的强度。");
    public static string FrameRateHint => S("Target rate is not hardware throughput; live preview shows completed frames per second.", "目標不代表硬體吞吐量；即時預覽顯示完成傳送的幀率。", "目标帧率不代表硬件吞吐量；实时预览显示完成发送的实际帧率。");
    public static string PreviewWaiting => S("Not synced", "未同步", "未同步");
    public static string PreviewHint => S("Shows the final colours sent by this app, including all overlays. No per-LED hardware readback.", "顯示本程式送出的最終顏色，包含所有疊加效果；不支援逐燈硬體回讀。", "显示本软件发送的最终颜色，包含所有叠加效果；不支持逐灯硬件回读。");
    public static string EditSelectionTitle => S("Edit selection", "編輯選區", "编辑选区");
    public static string SelectedKeyCountFormat => S("{0} keys selected", "已選擇 {0} 個按鍵", "已选择 {0} 个按键");
    public static string InvalidHexColor => S("Enter a six-digit RGB colour, such as #4CE6B5.", "請輸入六位 RGB 色碼，例如 #4CE6B5。", "请输入六位 RGB 色值，例如 #4CE6B5。");
    public static string UpdateZone => S("Update zone", "更新區域", "更新分区");
    public static string EditZone => S("Edit", "編輯", "编辑");
    public static string RemoveZone => S("Remove zone", "移除區域", "移除分区");
    public static string NewZone => S("New zone", "新增區域", "新建分区");
    public static string ApplyHint => S("Zones overlay the global effect.", "區域會疊加在全局效果之上。", "分区会叠加在全局灯效之上。");
    public static string EmptyZonesHint => S("No zones yet. Select keys or strip segments, then apply to selection.", "尚無區域。選擇按鍵或燈段後，點選「套用至選取」。", "还没有分区。选择按键或灯段后，点击“应用到选区”。");
    public static string AdvancedHeader => S("Advanced · Audio / GIF / System metrics", "進階功能 · 音樂 / GIF / 硬體資料", "高级功能 · 音乐响应 / GIF 动画 / 硬件数据");
    public static string AutoSavedText => S("Settings save automatically", "設定自動儲存", "设置自动保存");
    public static string ZoneAdjustLabel => S("Adjust region effect", "區域燈效調整", "区域灯效调整");
    public static string ZoneParameterHint => S("Independent parameters for this region. Apply to selection to save; Rainbow restores the preset palette.", "此區域使用獨立參數；套用至選取後儲存，勾選彩虹可恢復預設配色。", "此区域使用独立参数；应用到选区后保存，勾选彩虹可恢复预设配色。");
    public static string DecorationRequiresCustom => S("Available in Custom mode", "在自訂模式中開啟", "在自定义模式中开启");
    public static string ResetCurrentEffect => S("Restore current effect defaults", "恢復目前燈效預設值", "恢复当前灯效默认设置");
    public static string UserPresetsLabel => S("My lighting presets", "我的燈效", "我的灯效");
    public static string SaveCompositionLabel => S("Save custom lighting", "儲存自訂編排", "保存自定义编排");
    public static string UserPresetNameHint => S("Name (1–48 characters)", "名稱（1–48 字）", "名称（1–48 字）");
    public static string SaveUserPresetLabel => S("Save", "儲存", "保存");
    public static string SaveUserPresetAsLabel => S("Save as new", "另存為", "另存为");
    public static string RestoreSavedComposition => S("Restore saved composition", "恢復已儲存編排", "恢复已保存编排");
    public static string UserPresetHint => S("Save updates the loaded preset; use a new name and Save as new for a copy. FPS and audio source stay unchanged.", "儲存會更新目前載入的燈效；修改名稱後可另存副本。幀率與音源維持目前設定。", "保存会更新当前载入的灯效；修改名称后可另存副本。帧率与音频来源保持当前设置。");
    public static string UserPresetInvalidName => S("Enter a name of 1–48 characters without line breaks.", "請輸入 1–48 字的名稱，不可換行。", "请输入 1–48 字的名称，不可换行。");
    public static string UserPresetDuplicateName => S("That name already exists for this keyboard. Choose another name.", "此鍵盤已有同名燈效，請使用其他名稱。", "此键盘已有同名灯效，请使用其它名称。");
    public static string UserPresetSaved => S("Saved to My lighting presets.", "已儲存至「我的燈效」。", "已保存到“我的灯效”。");
    public static string UserPresetLoaded => S("Loaded. Edit freely, then Save to update or Save as new for a copy.", "已載入；編輯後可儲存更新，或另存副本。", "已载入；编辑后可保存更新，或另存副本。");
    public static string UserPresetSaveFailed => S("Could not save. The previous saved preset is unchanged.", "儲存失敗，先前已儲存的燈效未變更。", "保存失败，之前已保存的灯效未改变。");
    public static string DeleteUserPresetLabel => S("Delete", "刪除", "删除");
    public static string UserPresetDeleted => S("Preset deleted; current lighting is unchanged.", "燈效已刪除，目前燈光保持不變。", "灯效已删除，当前灯光保持不变。");
    public static string UserPresetDeleteFailed => S("Could not delete. The saved preset is unchanged.", "刪除失敗，已儲存的燈效未變更。", "删除失败，已保存的灯效未改变。");
    public static string UserPresetLoadFailed => S("The saved composition could not be loaded.", "無法載入此編排。", "无法载入此编排。");
    public static string OverallEffectLabel => S("Overall effect · final opacity", "總體效果 · 最終透明度", "总体效果 · 最终透明度");
    public static string OverallHint => S("Final output for keys, interactions, strip and eye. 0% = off; 100% = original.", "控制合成後的按鍵、互動、燈條與眼睛；0% 熄滅，100% 保留原效果。", "控制合成后的按键、交互、灯条与眼睛；0% 熄灭，100% 保留原效果。");
    public static string OverallConstant => S("Constant", "常亮", "常亮");
    public static string OverallBreathing => S("Breathing", "呼吸", "呼吸");
    public static string OverallBeat => S("Bass beat flash", "節拍閃光（低音）", "节拍闪光（低音）");
    public static string OverallOpacityLabel => S("Maximum opacity", "最高透明度", "最高透明度");
    public static string OverallMinimumLabel => S("Minimum opacity", "最低透明度", "最低透明度");
    public static string DecorationSegmentedLabel => S("Segment studio", "分段編排", "分段编排");
    public static string DecorationSegmentedHint => S("Select strip segments in the zones panel below, then assign colours or effects. Unassigned segments stay off.", "在下方分區選擇燈段後，配置顏色或效果；未分配燈段熄滅。", "在下方分区选择灯段后，配置颜色或效果；未分配灯段熄灭。");
    public static string StripSegmentsLabel => S("Strip segments · left to right", "燈條分段 · 由左至右", "灯条分段 · 从左到右");
    public static string StripSelectionHint => S("Select one or more segments (1–12), then assign with the selection editor. Keys and segments may share a zone.", "選擇一個或多個燈段（1–12），再從左側配置；可與按鍵共用分區。", "选择一个或多个灯段（1–12），再从左侧配置；可与按键共用分区。");
    public static string SelectedLightCountFormat => S("{0} lights selected", "已選 {0} 個燈位", "已选 {0} 个灯位");
    public static string DecorationMusicLabel => S("Music · left to right", "音樂律動 · 由左至右", "音乐律动 · 从左到右");
    public static string DecorationRainbowLabel => S("Flowing rainbow", "流動彩虹漸變", "流动彩虹渐变");
    public static string DecorationCustomHint => S("Click the strip or eye in the preview to select a region. Both save independent effects; music uses the selected audio source.", "點選預覽燈條或眼睛選擇區域；兩區分別儲存效果，音樂使用已選音源。", "点击预览灯条或眼睛选择区域；两区分别保存效果，音乐使用已选音源。");
    public static string LightBarSettingsLabel => S("Light bar", "燈條", "灯条");
    public static string EyeSettingsLabel => S("ROG eye", "ROG 眼睛", "ROG 眼睛");
    public static string DecorationFollow => S("Follow keys below", "跟隨下方按鍵", "跟随下方按键");
    public static string DecorationSpatial => S("Independent position", "獨立位置算法", "独立位置算法");
    public static string DecorationCustom => S("Custom", "自訂", "自定义");
    public static string DecorationModeHint => S("Independent mode samples above the keys; ATK/GIF use the top edge. Click the strip or eye to edit its custom region.", "獨立模式依鍵盤上方位置計算燈效；ATK/GIF 取上邊緣。點選燈條或眼睛可編輯自訂區域。", "独立模式按键盘上方位置计算灯效；ATK/GIF 取上边缘。点击灯条或眼睛可编辑自定义区域。");
    public static string LightBarLabel => S("ROG light bar · 12 segments + eye", "ROG 燈條 · 12 段 + 眼睛", "ROG 灯条 · 12 段 + 眼睛");
    public static string LightBarHint => S("Preview shows the last successfully sent bar/eye colours.", "預覽顯示燈條與眼睛最後成功傳送的顏色。", "预览显示灯条与眼睛最后成功发送的颜色。");
    public static string RestoreAndExit => S("Exit and restore", "結束並還原", "退出并恢复");
    public static string BaseColorLabel => S("Global colour", "全局顏色", "全局颜色");
    public static string EditingHint => S("Editing", "編輯中", "编辑中");

    // ----- Main window -----
    public static string EffectLabel => S("Effect", "效果", "全局灯效");
    public static string OverlaysLabel => S("Overlays", "疊加層", "叠加效果");
    public static string KeyBrightnessLabel => S("Intensity", "強度", "强度");
    public static string KeyEffectsLabel => S("Key interaction", "按鍵互動", "按键交互");
    public static string KeyEffectHint => S("React to real key presses. Choosing a preset pauses this layer.", "由實際按鍵觸發，切換預設燈效會暫停此層。", "由真实按键触发，切换预设灯效会暂停此层。");
    public static string KeyRainbowLabel => S("Rainbow", "彩虹色", "彩虹色");
    public static string KeyPaletteLabel => S("Effect palette", "使用效果配色", "使用效果配色");
    public static string MonetHint => S("Local primary-screen colours; sampling stops when this effect is inactive. No screen images are saved or uploaded.", "本機主螢幕取色，停用此效果即停止取色，不保存或上傳畫面。", "本地主屏幕取色，停用此效果即停止取色，不保存或上传画面。");
    public static string ReactiveOverlay => S("Reactive — keypress flare + ripple", "反應 — 按鍵閃光 + 漣漪", "按键响应 · 闪光与涟漪");
    public static string StarlightOverlay => S("Starlight — twinkling stars over your effect", "星光 — 在效果之上閃爍的星星", "星光 · 叠加闪烁星点");
    public static string AudioOverlay => S("Audio reactive", "音訊反應", "音乐响应");
    public static string AudioLayoutLabel => S("Audio layout", "音訊佈局", "音乐布局");
    public static string AudioBarsBassFormat => S("Bass multiplier: {0:0.0}×", "低音倍率：{0:0.0}×", "低音倍率：{0:0.0}×");
    public static string AudioBarsMidFormat => S("Mid multiplier: {0:0.0}×", "中音倍率：{0:0.0}×", "中音倍率：{0:0.0}×");
    public static string AudioBarsTrebleFormat => S("Treble multiplier: {0:0.0}×", "高音倍率：{0:0.0}×", "高音倍率：{0:0.0}×");
    public static string PriorityHint =>
        S("Starlight layers just above the base; reactive above audio; system metrics on top.", "星光位於基礎效果之上；反應位於音訊之上；系統指標位於最上層。", "星光位于基礎效果之上；反应位于音频之上；系统指标位于最上层。");
    public static string BrightnessFormat => S("Base effect brightness: {0:0}%", "基礎效果亮度：{0:0}%", "基础灯效亮度：{0:0}%");
    public static string AudioSensitivityFormat => S("Audio sensitivity: {0:0.0}×", "音訊靈敏度：{0:0.0}×", "音乐灵敏度：{0:0.0}×");
    public static string ColourLabel => S("Colour", "顏色", "颜色");
    public static string ColorRed => S("Red", "紅", "红");
    public static string ColorGreen => S("Green", "綠", "绿");
    public static string ColorBlue => S("Blue", "藍", "蓝");
    public static string ColorCyan => S("Cyan", "青", "青");
    public static string ColorMagenta => S("Magenta", "洋紅", "洋红");
    public static string ColorWhite => S("White", "白", "白");
    public static string ZoneEditorButton => S("Zone editor…", "區域編輯器…", "分区编辑器…");

    // ----- Shared animations -----
    public static string MusicCanvasTitle => S("Live continuous effect", "即時連續燈效", "实时连续灯效");
    public static string WaveColor1Label => S("Wave 1 colour", "波 1 顏色", "波 1 颜色");
    public static string WaveColor2Label => S("Wave 2 colour", "波 2 顏色", "波 2 颜色");
    public static string MusicCanvasSoundWaveHint => S("Bass-driven waves with independent colours or a flowing rainbow.", "雙波隨低音律動，可分別配色或使用流動彩虹。", "双波随低音律动，可分别配色或使用流动彩虹。");
    public static string MusicCanvasHint => S("655 × 240 live pixels · same audio and base-effect parameters · before key sampling; custom overlays excluded.", "655 × 240 即時像素 · 同一音訊與基礎燈效參數 · 未經按鍵取樣，不含自訂疊加層。", "655 × 240 实时像素 · 同一音频与基础灯效参数 · 未经过按键取样，不含自定义叠层。");
    public static string MusicCategory => S("Music reactive", "音樂律動", "音乐律动");
    public static string MusicAudioSource => S("Audio source", "音訊來源", "音频来源");
    public static string MusicSystemAudio => S("System output", "系統聲音", "系统声音");
    public static string MusicRefreshSources => S("Refresh applications", "重新整理程式", "刷新进程");
    public static string MusicSourceHint => S("Selected application includes its child processes, not a single browser tab.", "擷取所選程式及子程序，並非單一瀏覽器分頁。", "采集所选进程及子进程，不区分浏览器标签页。");
    public static string MusicResponse => S("Audio response", "音訊反應", "音频响应");
    public static string MusicSensitivity => S("Sensitivity", "靈敏度", "灵敏度");
    public static string MusicAttack => S("Attack (seconds)", "上升時間（秒）", "上升时间（秒）");
    public static string MusicRelease => S("Decay (seconds)", "回落時間（秒）", "回落时间（秒）");
    public static string MusicNoiseGate => S("Noise gate", "靜音門限", "静音门限");
    public static string MusicAutoGain => S("Automatic gain", "自動增益", "自动增益");
    public static string MusicBass => S("Bass emphasis", "低音強度", "低音强度");
    public static string MusicWaiting => S("Waiting for the selected application", "等待所選程式", "等待所选进程");
    public static string MusicCapturing => S("Capturing audio", "正在擷取音訊", "正在采集音频");
    public static string MusicStopped => S("Audio capture paused", "音訊擷取已暫停", "音频采集已暂停");
    public static string MusicUnavailable => S("Audio service unavailable; retrying", "音訊服務無法連線，正在重試", "音频服务未连接，正在重试");
    public static string MusicProcessUnsupported => S("Process capture requires Windows build 20348 or later.", "程式擷取需要 Windows 組建 20348 以上。", "进程采集需要 Windows 内部版本 20348 或更高。");
    public static string MusicSourceMissing => S("Reselect the saved application", "請重新選擇已存程式", "请重新选择已保存的进程");
    public static string AtkAudioSupported => S("Audio / mixed scene", "音訊／混合場景", "音频／混合场景");
    public static string AtkKeyframes => S("Keyframe animation", "逐幀動畫", "逐帧动画");
    public static string Favorite => S("Favorite current", "收藏目前效果", "收藏当前效果");
    public static string Unfavorite => S("Unfavorite current", "取消收藏", "取消收藏");
    public static string Favorites => S("Favorites", "收藏", "收藏");
    public static string Recent => S("Recently used", "最近使用", "最近使用");
    public static string RandomEffect => S("Random effect", "隨機效果", "随机效果");
    public static string MirrorHorizontal => S("Mirror horizontally", "水平鏡像", "水平镜像");
    public static string MirrorVertical => S("Mirror vertically", "垂直鏡像", "垂直镜像");
    public static string RotateEffect => S("Rotate 90°", "旋轉90°", "旋转90°");
    public static string AtkUnsupported => S("Unsupported effect or module", "暫不支援的效果或模組", "暂不支持的效果或模块");
    public static string AtkGallery => S("ATK gallery", "ATK 燈效廣場", "ATK 灯效广场");
    public static string AtkGalleryHint => S("Public ATK works · Only frame-by-frame animations are supported; other works are hidden. Preview before applying.", "ATK 公開作品 · 僅支援逐幀動畫，其餘作品已隱藏。請先預覽再套用至鍵盤。", "ATK 公开作品 · 仅支持逐帧动画，其它作品已隐藏。请先预览再应用到键盘。");
    public static string AtkNone => S("No ATK animation imported.", "尚未匯入 ATK 動畫。", "尚未导入 ATK 动画。");
    public static string AtkLoadFailed => S("The saved ATK animation could not be loaded. Import it again from the gallery.", "已存 ATK 動畫無法載入，請從廣場重新匯入。", "已保存的 ATK 动画无法加载，请从广场重新导入。");
    public static string AtkHot => S("Popular", "熱門", "热门");
    public static string AtkLatest => S("Latest", "最新", "最新");
    public static string AtkSearch => S("Search / refresh", "搜尋／重新整理", "搜索／刷新");
    public static string AtkSearchHint => S("Search animation titles", "搜尋動畫名稱", "搜索动画名称");
    public static string AtkLoading => S("Loading public works…", "正在載入公開作品…", "正在加载公开作品…");
    public static string AtkNetworkError => S("Could not load the gallery. Check your connection and retry.", "無法載入廣場，請檢查網路後重試。", "无法加载广场，请检查网络后重试。");
    public static string AtkPreviewHint => S("Screen preview only; your keyboard keeps its current effect until you apply.", "僅畫面預覽；套用前鍵盤維持目前效果。", "仅屏幕预览；应用前键盘保持当前效果。");
    public static string AtkSelect => S("Select a keyframe animation from the list.", "請從清單選取逐幀動畫。", "请从列表选择逐帧动画。");
    public static string AtkFit => S("Fit to keyboard", "鋪滿鍵盤", "铺满键盘");
    public static string AtkKeepGrid => S("Keep grid positions", "保留網格位置", "保留网格位置");
    public static string AtkLayoutHint => S("Keyboard layouts differ. Use the preview to choose a mapping.", "鍵盤佈局不同，請依預覽選擇適配方式。", "键盘布局不同，请根据预览选择适配方式。");
    public static string AtkApply => S("Import and apply", "匯入並套用", "导入并应用");
    public static string AtkImportError => S("Import failed. Your current animation was kept.", "匯入失敗，已保留目前動畫。", "导入失败，已保留当前动画。");
    public static string AtkLayoutFilterFormat => S("Only confirmed {0} sources · {1}. Other and unknown layouts are hidden.", "僅顯示可確認的 {0} 來源 · {1}。其他與未知配列已隱藏。", "仅显示可确认的 {0} 来源 · {1}。其它与未知配列已隐藏。");
    public static string AtkCatalogueLoadingFormat => S("Loading all works · Checked {0}/{1} · {2} matching", "載入全部作品中 · 已檢查 {0}/{1} · 匹配 {2} 個", "正在加载全部作品 · 已检查 {0}/{1} · 匹配 {2} 个");
    public static string AtkCatalogueCompleteFormat => S("All works loaded · {0} matching · {1} excluded or duplicate", "全部載入完成 · 匹配 {0} 個 · 排除或重複 {1} 個", "全部加载完成 · 匹配 {0} 个 · 排除或重复 {1} 个");
    public static string AtkCatalogueIncompleteFormat => S("Incomplete load · {0} matches available. Search again to retry.", "尚未完整載入 · 已找到 {0} 個匹配作品，請重新搜尋以重試。", "尚未完整加载 · 已找到 {0} 个匹配作品，请重新搜索以重试。");
    public static string AtkNoMatchingLayout => S("No matching keyframe animations for this keyboard.", "沒有符合目前鍵盤配列的逐幀動畫。", "没有符合当前键盘配列的逐帧动画。");
    public static string AtkLayoutMismatch => S("The source layout is unknown or differs from your keyboard.", "來源配列未知或與目前鍵盤不同。", "来源配列未知或与当前键盘不同。");
    public static string AtkKeyboardChanged => S("The keyboard changed. Reopen the gallery to refresh layout matching.", "鍵盤已變更，請重新開啟廣場以更新配列篩選。", "键盘已变更，请重新打开广场以更新配列筛选。");
    public static string FormFactorName(RSS_II_RGB.Core.Layout.KeyboardFormFactor factor) => factor switch
    {
        RSS_II_RGB.Core.Layout.KeyboardFormFactor.Compact60 => "60%",
        RSS_II_RGB.Core.Layout.KeyboardFormFactor.Compact65 => "65%",
        RSS_II_RGB.Core.Layout.KeyboardFormFactor.Compact75 => "75%",
        RSS_II_RGB.Core.Layout.KeyboardFormFactor.Tenkeyless => "80% / TKL",
        RSS_II_RGB.Core.Layout.KeyboardFormFactor.Compact96 => "96%",
        RSS_II_RGB.Core.Layout.KeyboardFormFactor.FullSize => "100%",
        _ => S("unknown layout", "未知配列", "未知配列"),
    };
    public static string AtkFramesFormat => S("{0} frames · Original timing preserved", "{0} 幀 · 保留原始播放時序", "{0} 帧 · 保留原始播放时序");
    // ----- GIF animation -----
    public static string GifLabel => S("GIF animation", "GIF 動畫", "GIF 动画");
    public static string GifComponentMissing => S("This package has no GIF converter. Use Full to import GIFs; saved animations and ATK playback still work.", "此版本不含 GIF 轉換元件。匯入 GIF 請使用完整版；已有動畫與 ATK 仍可播放。", "此版本不含 GIF 转换组件。导入 GIF 请使用完整版；已有动画与 ATK 仍可播放。");
    public static string GifImportButton => S("Import GIF…", "匯入 GIF…", "导入 GIF…");
    public static string GifNone => S("No GIF imported.", "尚未匯入 GIF。", "尚未导入 GIF。");
    public static string GifImportedFormat => S("GIF: {0} ({1} frames)", "GIF：{0}（{1} 幀）", "GIF：{0}（{1} 帧）");
    public static string GifProcessing => S("Processing GIF… please wait.", "正在處理 GIF…請稍候。", "正在处理 GIF…请稍候。");
    public static string GifReadyNote =>
        S(" — baked to the keyboard; the original GIF is no longer needed.", "（已烘焙到鍵盤；原始 GIF 已不再需要）", "（已转换为键盘动画，不再依赖原始 GIF）");
    public static string GifImportFailedFormat => S("GIF import failed: {0}", "GIF 匯入失敗：{0}", "GIF 汇入失敗：{0}");
    public static string GifPickTitle => S("Choose a GIF", "選擇 GIF", "选择 GIF");
    public static string GifCropTitle => S("Select GIF region", "選取 GIF 區域", "选择 GIF 区域");
    public static string GifCropHint => S("Drag a region; its proportions follow the connected keyboard.", "拖曳選取範圍；比例依照目前鍵盤。", "拖动选择区域，比例按当前键盘锁定。");
    public static string GifCropOk => S("Use this region", "使用此區域", "使用此分区");
    public static string GifCropCancel => S("Cancel", "取消", "取消");
    public static string StartWithWindows =>
        S("Start with Windows (minimised to tray)", "開機自動啟動（最小化到系統匣）", "开机启动并最小化到托盘");
    public static string SystemMetricsHeader => S("System metrics (overlay)", "系統指標（疊加）", "硬件数据");
    public static string ShowMetricBars => S("Show metric bars on the keyboard", "在鍵盤上顯示指標列", "在键盘上显示指标列");
    public static string LayoutLabel => S("Layout", "版面", "版面");
    public static string UtilisationThresholds =>
        S("Utilisation % thresholds (→ 1 / 2 / 3 / 4 cells)", "使用率 % 門檻（→ 1 / 2 / 3 / 4 格）", "使用率 % 门槛（→ 1 / 2 / 3 / 4 格）");
    public static string GpuTempThresholds => S("GPU temperature °C thresholds", "GPU 溫度 °C 門檻", "GPU 温度 °C 门槛");
    public static string MetricMappingHint =>
        S("CPU%→first group, Mem%→second, GPU%→third, GPU temp→last.", "CPU%→第一組，記憶體%→第二組，GPU%→第三組，GPU 溫度→最後。", "CPU%→第一組，记忆体%→第二組，GPU%→第三組，GPU 温度→最後。");
    public static string MainTip =>
        S("Tip: tick 'Reactive' and type — each key flares and ripples on top of your effect.", "提示：勾選「反應」並打字 — 每個按鍵會在你的效果之上閃光並擴散漣漪。", "开启按键响应后，按下的键会闪光并向外扩散涟漪。");

    // ----- Zone editor -----
    public static string ZoneEffectLabel => S("Zone effect", "區域效果", "区域灯效");
    public static string AudioZoneModeTooltip => S("Audio zone mode", "音訊區域模式", "音频分区模式");
    public static string AssignToSelection => S("Assign to selection", "套用到選取", "应用到选区");
    public static string SelectAll => S("Select all", "全選", "全选");
    public static string ClearSelection => S("Clear selection", "清除選取", "清除选择");
    public static string RemoveAllZones => S("Remove all zones", "移除所有區域", "清空分区");
    public static string ZoneEditorHint =>
        S("Click keys to toggle, or drag a box to select a range. Then pick an effect + colour and Assign.", "點擊按鍵切換，或拖曳方框選取範圍。接著選擇效果 + 顏色並套用。", "点选按键，或从空白处拖动框选；选择灯效和颜色后应用。");
    public static string ZonesHeader => S("Zones", "區域", "已建分区");

    // ----- Status (main view model) -----
    public static string StatusConnecting => S("Connecting…", "連線中…", "连线中…");
    public static string StatusConnectedFormat =>
        S("Connected — {0}, firmware {1}", "已連線 — {0}，韌體 {1}", "已连接 · 固件 {1}");
    public static string StatusSearching =>
        S("Waiting for wired keyboard…", "等待有線鍵盤連線…", "等待有线键盘连接…");

    // ----- Tray menu -----
    public static string TrayShow => S("Show", "顯示", "显示");
    public static string TrayExit => S("Exit", "結束", "退出并恢复");

    // ----- Zone row summary -----
    public static string ZoneSummary(string label, int count) =>
        S($"{label} on {count} key(s)", $"{label} · {count} 鍵", $"{label} · {count} 键");

    // ----- Enum display names -----
    public static string EffectName(EffectChoice e) => e switch
    {
        EffectChoice.Off => S("Off", "關閉", "关闭"),
        EffectChoice.Monet => S("Monet colours", "莫奈配色", "莫奈配色"),
        EffectChoice.Marquee => S("Marquee", "跑馬燈", "跑马灯"),
        EffectChoice.LakePixels => S("Lake pixels", "湖面像素", "湖面像素"),
        EffectChoice.LakeRipples => S("Lake ripples", "湖面漣漪", "湖面涟漪"),
        EffectChoice.LakeSurge => S("Lake swell", "湖面起伏", "湖面起伏"),
        EffectChoice.PerimeterFlow => S("Perimeter flow", "環繞流光", "环绕流光"),
        EffectChoice.RotatingRing => S("Rotating colour ring", "旋轉色環", "旋转色环"),
        EffectChoice.DoubleBeacon => S("Twin beacons", "雙燈塔", "双灯塔"),
        EffectChoice.SineSectors => S("Sine sectors", "正弦波", "正弦波"),
        EffectChoice.WindmillCycle => S("Windmill cycle", "風車循環", "风车循环"),
        EffectChoice.Caustics => S("Water caustics", "水面光斑", "水面光斑"),
        EffectChoice.Sun => S("Sun", "太陽", "太阳"),
        EffectChoice.Sakura => S("Cherry blossoms", "櫻花", "樱花"),
        EffectChoice.IceCrystal => S("Ice crystals", "冰晶", "冰晶"),
        EffectChoice.AuroraRipples => S("Aurora ripples", "極光漣漪", "极光涟漪"),
        EffectChoice.KeySpotlight => S("Key spotlight", "按鍵聚光燈", "按键聚光灯"),
        EffectChoice.KeyDarkRipple => S("Key dark ripple", "按鍵暗漣漪", "按键暗涟漪"),
        EffectChoice.KeyDoubleRing => S("Key double ring", "按鍵雙環", "按键双环"),
        EffectChoice.KeyDiamond => S("Key diamond", "按鍵菱形擴散", "按键菱形扩散"),
        EffectChoice.KeyDye => S("Key dye", "按鍵染色", "按键染色"),
        EffectChoice.KeyHeart => S("Key heart", "按鍵愛心", "按键爱心"),
        EffectChoice.KeyLightning => S("Key lightning", "按鍵閃電", "按键闪电"),
        EffectChoice.KeyFireworks => S("Key fireworks", "按鍵煙花", "按键烟花"),
        EffectChoice.Solid => S("Solid", "常亮模式", "常亮模式"),
        EffectChoice.Breathing => S("Breathing", "呼吸模式", "呼吸模式"),
        EffectChoice.Rainbow => S("Rainbow", "夢幻彩虹", "梦幻彩虹"),
        EffectChoice.Wave => S("Wave", "隨波逐流", "随波逐流"),
        EffectChoice.Reactive => S("Reactive", "反應", "反应"),
        EffectChoice.CpuTemp => S("CPU temp", "CPU 溫度", "CPU 温度"),
        EffectChoice.GpuTemp => S("GPU temp", "GPU 溫度", "GPU 温度"),
        EffectChoice.Audio => S("Audio", "音訊", "音乐响应"),
        EffectChoice.Gif => S("GIF", "GIF 動畫", "GIF 动画"),
        EffectChoice.Atk => AtkGallery,
        EffectChoice.MusicSpectrumBars => S("SpectrumBars", "频谱柱", "频谱柱"),
        EffectChoice.MusicBeatFlash => S("BeatFlash", "节拍闪光", "节拍闪光"),
        EffectChoice.MusicClassicEqualizer => S("ClassicEqualizer", "经典均衡器", "经典均衡器"),
        EffectChoice.MusicMirrorSpectrum => S("MirrorSpectrum", "镜像频谱", "镜像频谱"),
        EffectChoice.MusicNeonSpectrum => S("NeonSpectrum", "霓光频谱", "霓光频谱"),
        EffectChoice.MusicFlowNeonSpectrum => S("FlowNeonSpectrum", "霓光频谱(流彩)", "霓光频谱(流彩)"),
        EffectChoice.MusicSoftSpectrum => S("SoftSpectrum", "柔光频谱", "柔光频谱"),
        EffectChoice.MusicSoundWave => S("SoundWave", "声浪", "声浪"),
        EffectChoice.MusicSpectrumWaterfall => S("SpectrumWaterfall", "频谱瀑布", "频谱瀑布"),
        EffectChoice.MusicRhythmOrbit => S("Prism beat", "稜鏡節拍", "棱镜节拍"),
        EffectChoice.MusicShockwave => S("Shockwave", "冲击波", "冲击波"),
        EffectChoice.MusicBeatSparks => S("BeatSparks", "节拍星火", "节拍星火"),
        EffectChoice.MusicEnergyCore => S("Solar rhythm", "太陽律動", "太阳律动"),
        EffectChoice.MusicBottomEnergyCore => S("BottomEnergyCore", "底部能量核心", "底部能量核心"),
        EffectChoice.MusicVolumeBreathing => S("VolumeBreathing", "音量呼吸", "音量呼吸"),
        EffectChoice.MusicRhythmHeart => S("RhythmHeart", "律动爱心", "律动爱心"),
        EffectChoice.MusicSpectrumColors => S("SpectrumColors", "频谱彩光", "频谱彩光"),
        EffectChoice.MusicBidirectionalSpectrum => S("BidirectionalSpectrum", "双向频谱", "双向频谱"),
        EffectChoice.MusicSpectrumRing => S("SpectrumRing", "频谱圆环", "频谱圆环"),
        EffectChoice.MusicBottomSpectrumRing => S("BottomSpectrumRing", "底部频谱圆环", "底部频谱圆环"),
        EffectChoice.MusicBeatRipples => S("BeatRipples", "节拍涟漪", "节拍涟漪"),
        EffectChoice.MusicBassSurge => S("BassSurge", "低音上涌", "低音上涌"),
        EffectChoice.MusicBeatSweep => S("BeatSweep", "节拍扫光", "节拍扫光"),
        EffectChoice.MusicRhythmStars => S("RhythmStars", "律动星空", "律动星空"),
        EffectChoice.MusicBeatChecker => S("BeatChecker", "节拍棋盘", "节拍棋盘"),
        EffectChoice.MusicRhythmFire => S("RhythmFire", "律动火焰", "律动火焰"),
        EffectChoice.MusicWaveformBounce => S("WaveformBounce", "波形跳动", "波形跳动"),
        EffectChoice.MusicCentralRhythm => S("CentralRhythm", "中央律动", "中央律动"),
        EffectChoice.MusicRowBars => S("RowBars", "行音柱", "行音柱"),
        EffectChoice.MusicSoundDiffusion => S("SoundDiffusion", "音波扩散", "音波扩散"),
        EffectChoice.MusicHeartPierce => S("HeartPierce", "万键穿心", "万键穿心"),
        EffectChoice.MusicSLine => S("SLine", "S线律动", "S线律动"),
        EffectChoice.MusicPlasmaLightning => S("PlasmaLightning", "等离子闪电", "等离子闪电"),
        EffectChoice.MusicParty => S("Party", "狂欢派对", "狂欢派对"),
        EffectChoice.MusicRainbowCorridor => S("RainbowCorridor", "幻彩回廊", "幻彩回廊"),
        EffectChoice.MusicMusicPuddles => S("MusicPuddles", "音乐水洼", "音乐水洼"),
        EffectChoice.MusicFlameEye => S("FlameEye", "烈焰之眼", "烈焰之眼"),
        EffectChoice.MusicRadialSpectrum => S("RadialSpectrum", "放射频谱", "放射频谱"),

        EffectChoice.Aurora => S("Aurora", "极光", "极光"),
        EffectChoice.Ocean => S("Ocean", "海浪", "海浪"),
        EffectChoice.Nebula => S("Nebula", "星云", "星云"),
        EffectChoice.Fire => S("Fire", "火焰", "火焰"),
        EffectChoice.Candle => S("Candle", "烛光", "烛光"),
        EffectChoice.Meteor => S("Meteor", "流星", "流星"),
        EffectChoice.Radar => S("Radar", "雷达扫描", "雷达扫描"),
        EffectChoice.Heartbeat => S("Heartbeat", "心跳", "心跳"),
        EffectChoice.Checker => S("Checker", "棋盘闪烁", "棋盘闪烁"),
        EffectChoice.Snake => S("Snake", "贪吃蛇", "贪吃蛇"),
        EffectChoice.ScanBand => S("ScanBand", "扫描光带", "扫描光带"),
        EffectChoice.Wipe => S("Wipe", "色彩擦除", "色彩擦除"),
        EffectChoice.Marble => S("Marble", "大理石", "大理石"),
        EffectChoice.Silk => S("Silk", "丝绸", "丝绸"),
        EffectChoice.Dna => S("Dna", "DNA 双螺旋", "DNA 双螺旋"),
        EffectChoice.Sunset => S("Sunset", "日落", "日落"),
        EffectChoice.RainDrops => S("RainDrops", "雨滴", "雨滴"),
        EffectChoice.Lava => S("Lava", "熔岩", "熔岩"),
        EffectChoice.Lightning => S("Lightning", "雷暴", "雷暴"),
        EffectChoice.NeonPulse => S("NeonPulse", "霓虹脉冲", "霓虹脉冲"),

        EffectChoice.Starlight => S("Starlight", "星光", "星光"),
        EffectChoice.KeyTrigger => S("Key trigger", "一觸即發", "一触即发"),
        EffectChoice.Rain => S("Rain", "雨中漫步", "雨中漫步"),
        EffectChoice.Ripple => S("Key ripple", "按鍵漣漪", "按键涟漪"),
        EffectChoice.StarField => S("Stars", "繁星點點", "繁星点点"),
        EffectChoice.SineWave => S("Sine curve", "正弦曲線", "正弦曲线"),
        EffectChoice.Waterfall => S("Waterfall", "七彩瀑布", "七彩瀑布"),
        EffectChoice.Bloom => S("Bloom", "花開富貴", "花开富贵"),
        EffectChoice.BrickBreaker => S("Brick breaker", "打磚塊", "打砖块"),
        EffectChoice.CountdownFireworks => S("Countdown fireworks", "倒計時煙花", "倒计时烟花"),
        EffectChoice.Vortex => S("Colour vortex", "多彩漩渦", "多彩漩涡"),
        _ => e.ToString(),
    };

    public static string MetricLayoutName(MetricLayoutChoice m) => m switch
    {
        MetricLayoutChoice.FunctionRow => S("Function row", "功能列", "功能键"),
        MetricLayoutChoice.Numpad => S("Numpad", "數字鍵盤", "数字键盘"),
        MetricLayoutChoice.Diagonal => S("Diagonal", "對角線", "对角线"),
        _ => m.ToString(),
    };

    public static string AudioModeName(AudioZoneMode a) => a switch
    {
        AudioZoneMode.Spectrum => S("Spectrum", "頻譜", "频谱"),
        AudioZoneMode.SolidColor => S("Solid colour", "單色", "单色"),
        AudioZoneMode.SolidRainbow => S("Solid rainbow", "彩虹", "彩虹"),
        _ => a.ToString(),
    };

    public static string AudioLayoutName(AudioLayoutChoice a) => a switch
    {
        AudioLayoutChoice.Spectrum => S("Spectrum", "頻譜", "频谱"),
        AudioLayoutChoice.Bars => S("Three-region bars", "三區長條", "三段频谱"),
        _ => a.ToString(),
    };
}
