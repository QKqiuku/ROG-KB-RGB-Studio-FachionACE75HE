namespace RSS_II_RGB.App;

internal static partial class L
{
    public static string ConnectionMonitor => S("Connection monitor", "連線監測", "连接监测");
    public static string ConnectionConflict => S("Lighting control conflict", "燈光控制衝突", "灯光控制冲突");
    public static string ConnectionUnavailable => S("Keyboard found; cannot open", "已找到鍵盤，但無法開啟", "已找到键盘，但无法打开");
    public static string ConnectionFailed => S("Keyboard communication failed", "鍵盤通訊失敗", "键盘通信失败");
    public static string ConnectionStreaming => S("Sending lighting frames", "正在傳送燈光", "正在发送灯光");
    public static string ConnectionStateLabel => S("State", "狀態", "状态");
    public static string ConnectionFirmwareLabel => S("Last detected firmware", "最近識別的韌體", "最近识别的固件");
    public static string ConnectionAttemptLabel => S("Last connection attempt", "最近連線嘗試", "最近连接尝试");
    public static string ConnectionFrameLabel => S("Last successful lighting frame", "最近成功傳送燈光", "最近成功发送灯光");
    public static string ConnectionFramesLabel => S("Frames sent this connection", "本次連線已傳送幀數", "本次连接已发送帧数");
    public static string ConnectionErrorLabel => S("Current error", "目前錯誤", "当前错误");
    public static string ConnectionProcessesLabel => S("Detected lighting programs", "偵測到的燈光程式", "检测到的灯光程序");
    public static string ConnectionProcessesHint => S("Their presence does not prove they own the keyboard. Refresh to scan again.", "程式正在執行不代表它佔用了鍵盤，點重新檢查可再次掃描。", "程序正在运行不代表它占用了键盘，点重新检查可再次扫描。");
    public static string ConnectionRecovery => S("If control fails: exit other lighting tools, then reconnect the USB cable and click Reconnect. An earlier abnormal exit can leave software mode active. This app will not force control or stop other programs.", "若無法控制：退出其他燈光工具，拔插 USB 後點重新連線。上次異常退出也可能留下軟體模式。本程式不會強制接管或關閉其他程式。", "若无法控制：退出其它灯光工具，拔插 USB 后点重新连接。上次异常退出也可能留下软件模式。本软件不会强制接管或关闭其它程序。");
    public static string ConnectionRetry => S("Reconnect", "重新連線", "重新连接");
    public static string ConnectionCopy => S("Copy diagnostics", "複製診斷資訊", "复制诊断信息");
    public static string ConnectionRefresh => S("Check again", "重新檢查", "重新检查");
    public static string ConnectionNone => S("None", "無", "无");
    public static string ConnectionNotChecked => S("Not checked", "尚未檢查", "尚未检查");
    public static string ConnectionProcessCheckFailed => S("Cannot check running programs", "無法檢查執行中的程式", "无法检查运行中的程序");
    public static string ConnectionCopied => S("Diagnostics copied", "已複製診斷資訊", "已复制诊断信息");
    public static string ConnectionCopyFailed => S("Clipboard unavailable; select and copy the text above.", "剪貼簿無法使用，請選取上方文字複製。", "剪贴板不可用，请选取上方文字复制。");
}
