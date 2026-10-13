using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ProbablyStolenTrainer
{
    public static class L
    {
        static string language = "zh-CN";
        public static string Language { get { return language; } }
        public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProbablyStolenTrainer", "language.txt");
        static readonly Dictionary<string, string> english = new Dictionary<string, string>
        {
            { "Probably Stolen · 金钱 / 时间 / 声望 / 鉴定", "Probably Stolen · Money / Time / Reputation / Appraisal" },
            { "试玩版修改器  /  金钱 · 时间 · 势力声望 · 鉴定价值", "Demo trainer  /  Money · Time · Reputation · Appraisal" },
            { "未连接  ·  先启动游戏并读入存档", "Not connected · Start the game and load a save" },
            { "选择进程…", "Select process…" },
            { "金钱", "Money" },
            { "当前：—", "Current: —" },
            { "设置金钱", "Set money" },
            { "天数", "Day" },
            { "设置天数", "Set day" },
            { "租金倒计时", "Rent due" },
            { "设置剩余天数", "Set rent days" },
            { "势力声望", "Reputation" },
            { "设置选中势力", "Set selected" },
            { "声望范围：-200 ～ 200", "Reputation range: -200 to 200" },
            { "全部势力设为输入值", "Set all factions" },
            { "直接显示鉴定物真实价值", "Reveal appraisal value" },
            { "重新悬停物品，查看游戏内 TRUE VALUE", "Hover again to see TRUE VALUE in game" },
            { "修改前先暂停游戏。日期调整不会补跑剧情或结算；声望奖励由游戏后续刷新。", "Pause before editing. Day changes skip event processing; reputation rewards refresh in game." },
            { "刷新当前数值", "Refresh values" },
            { "撤销上一次修改", "Undo last edit" },
            { "备份存档…", "Back up saves…" },
            { "窗口置顶", "Always on top" },
            { "就绪。点击“选择进程…”连接游戏，无需设置盘符。请在玩游戏的 Windows 内运行。", "Ready. Select the game process to connect. Run this trainer in the Windows system running the game." },
            { "已断开 · 请重新连接", "Disconnected · Please reconnect" },
            { "价值显示恢复提示", "Appraisal display restoration" },
            { "真实价值显示已开启。重新悬停鉴定物，提示中将出现 TRUE VALUE。", "Appraisal value enabled. Hover over an appraisal item again to see TRUE VALUE." },
            { "真实价值显示已关闭。重新悬停物品刷新提示。", "Appraisal value disabled. Hover over the item again to refresh its tooltip." },
            { "请先连接游戏。", "Connect to the game first." },
            { "操作未完成", "Action not completed" },
            { "正在校验版本…", "Checking game version…" },
            { "已连接 · ", "Connected · " },
            { "游戏目录（来自所选进程）：", "Game directory (from selected process): " },
            { "版本与对象校验通过。可以修改当前存档的数值。", "Version and object checks passed. You can now edit the loaded save's values." },
            { "连接失败 · 请检查游戏状态", "Connection failed · Check game state" },
            { "当前：", "Current: " },
            { " 天", " days" },
            { " 天 / 租金 ", " days / rent " },
            { "当前声望：", "Current reputation: " },
            { "：", ": " },
            { "数值已修改，但操作日志保存失败：", "Values changed, but the change log could not be saved: " },
            { "选择包含 save_0.es3 等文件的 Probably Stolen 存档文件夹", "Select the Probably Stolen save folder containing save_0.es3 and other saves" },
            { "这个文件夹中没有 save_*.es3 存档。", "This folder contains no save_*.es3 files." },
            { "已备份 ", "Backed up " },
            { " 个存档文件到 ", " save files to " },
            { "备份已保存：\n", "Backup saved:\n" },
            { "备份完成", "Backup complete" },
            { "界面预览 · 示例数据，未连接游戏", "UI preview · Sample data, no game connected" },
            { "当前：12,450", "Current: 12,450" },
            { "当前：18 天", "Current: 18 days" },
            { "当前：3 天 / 租金 210", "Current: 3 days / rent 210" },
            { "示例势力", "Sample faction" },
            { "界面测试使用示例值，未读取或修改真实游戏。", "Preview uses sample data. No real game was read or modified." },
            { "（路径不可读）", "(path unavailable)" },
            { "选择游戏进程", "Select game process" },
            { "搜索名称 / PID / 路径", "Name / PID / path" },
            { "刷新列表", "Refresh list" },
            { "进程名", "Process name" },
            { "程序路径", "Executable path" },
            { "正在读取进程…", "Reading processes…" },
            { "连接选中进程", "Connect selected" },
            { "取消", "Cancel" },
            { "请选择游戏进程。双击一行也可以连接。", "Select the game process. You can also double-click a row to connect." },
            { "读取进程列表失败：", "Could not read process list: " },
            { "未找到匹配进程。请启动游戏后刷新，或清空搜索框。\n请在玩游戏的 Windows 系统内运行修改器。", "No matching process. Start the game and refresh, or clear the search.\nRun this trainer in the Windows system running the game." },
            { "显示 ", "Showing " },
            { " 个进程；清空搜索框可查看全部。\n连接后自动识别目录，不需要设置盘符。", " processes. Clear the search to show all.\nThe game directory is detected automatically when you connect." },
            { "无法读取这个进程的启动信息。请确认选中了游戏，且游戏和修改器使用相同的用户与权限。", "Cannot read this process's start time. Select the game and run both programs with the same user and permissions." },
            { "无法选择", "Cannot select process" },
            { "选择游戏进程（界面预览 · 示例数据）", "Select game process (preview · Sample data)" },
            { "价值提示代码已变化，请关闭开关或重启游戏。", "The appraisal tooltip code changed. Disable the toggle or restart the game." },
            { "游戏已退出。", "The game has exited." },
            { "价值提示代码不匹配，已拒绝修改。若修改器曾被强制结束，请先重启游戏。", "Appraisal tooltip code does not match. No change was made. Restart the game if the trainer was forcibly closed." },
            { "价值提示写后校验失败。", "Appraisal tooltip verification failed after writing." },
            { "切换失败且未能恢复，请重启游戏。", "Toggle failed and could not be restored. Restart the game. " },
            { "切换失败，已恢复原状态。", "Toggle failed; the original state was restored. " },
            { "提示代码被其他修改覆盖，无法安全恢复。", "Other changes have overwritten the tooltip code. It cannot be safely restored." },
            { "恢复价值提示时校验失败。", "Verification failed while restoring the appraisal tooltip." },
            { "无法读取游戏进程。请让游戏和修改器使用相同的权限运行。", "Cannot read the game process. Run the game and trainer with the same permissions." },
            { "内存地址或长度无效。", "Invalid memory address or length." },
            { "读取失败。游戏可能正在切换场景；请读入存档后重新连接。", "Read failed. The game may be changing scenes; load a save and reconnect." },
            { "拒绝无效写入。", "Invalid write rejected." },
            { "没有游戏进程写入权限。", "Write access to the game process was denied." },
            { "写入失败；未完成本次操作。", "Write failed; the action was not completed." },
            { "游戏已退出，无法切换价值显示。", "The game has exited. Appraisal display cannot be toggled." },
            { "无法切换价值显示，请检查游戏进程权限。", "Cannot toggle appraisal display. Check game process permissions." },
            { "无法访问价值提示代码。", "Cannot access appraisal tooltip code." },
            { "价值显示开关写入失败。", "Appraisal toggle write failed." },
            { "无法刷新游戏指令缓存。", "Cannot flush the game's instruction cache." },
            { "无法恢复游戏内存页属性。", "Cannot restore the game's memory page protection." },
            { "游戏版本不匹配。本修改器仅支持 Demo 构建 25382790，已停止连接。", "Game version mismatch. Only Demo build 25382790 is supported. Connection stopped." },
            { "所选进程已退出、重新启动或无法验证。请刷新进程列表后重新选择。", "The selected process exited, restarted, or could not be verified. Refresh the process list and select it again." },
            { "所选进程未加载 GameAssembly.dll。请选择已读入存档的 Probably Stolen 游戏进程。", "The selected process has not loaded GameAssembly.dll. Select Probably Stolen after loading a save." },
            { "游戏对象尚未就绪，请先读入存档。", "Game objects are not ready. Load a save first." },
            { "类型名称无效。", "Invalid type name." },
            { "字符串长度无效。", "Invalid string length." },
            { "游戏类型校验失败：", "Game type validation failed: " },
            { "游戏对象校验失败：", "Game object validation failed: " },
            { "已读取当前游戏：金钱 {0:N0}，第 {1} 天，租金剩余 {2} 天。", "Loaded game values: money {0:N0}, day {1}, rent due in {2} days." },
            { "日期字段校验失败。", "Date field validation failed." },
            { "声望列表尚未就绪或结构不匹配。", "The reputation list is not ready or its structure does not match." },
            { "声望对象类型不匹配。", "Reputation object type mismatch." },
            { "声望字段校验失败。", "Reputation field validation failed." },
            { "场景正在切换，请稍后重新连接。", "The scene is changing. Reconnect when it has finished." },
            { "存档或场景已切换，请重新连接；旧操作已清除。", "The save or scene changed. Reconnect; previous edits have been cleared." },
            { "该势力已不存在，请刷新。", "This faction no longer exists. Refresh the values." },
            { "声望 ", "Reputation " },
            { "未知字段。", "Unknown field." },
            { "输入超出范围或不是整数。", "The input is out of range or must be an integer." },
            { "存档已变化。", "The loaded save has changed." },
            { "数值或对象已发生变化，请暂停游戏并重试。", "A value or object changed. Pause the game and try again." },
            { "写后校验失败。", "Verification failed after writing." },
            { " 已写入的数值已回退。", " Written values were rolled back." },
            { " 部分修改无法安全回退，请检查游戏数值。", " Some changes could not be safely rolled back. Check the game values." },
            { "没有可撤销的操作。", "There is no edit to undo." },
            { "撤销 ", "Undo " },
            { "当前连接不支持价值显示开关。", "This connection does not support the appraisal display toggle." },
            { "价值显示未能恢复。请重启游戏恢复原始提示。", "Appraisal display could not be restored. Restart the game to restore the original tooltip. " },
            { "适配 Demo · Steam 构建 25382790  |  v1.0.1  |  选择进程，自动识别路径", "Demo · Steam build 25382790  |  v1.0.1  |  Automatic path detection" },
            { "语言已切换为简体中文。", "Language changed to English." },
            { "无法保存语言偏好，下次启动可能恢复默认语言：", "Could not save the language preference; the next launch may use the default: " },
            { "当前：{0:N0}", "Current: {0:N0}" },
            { "当前：{0} 天", "Current: {0} days" },
            { "当前：{0} 天 / 租金 {1}", "Current: {0}d / rent {1}" },
            { "当前声望：{0:0.##}", "Current reputation: {0:0.##}" },
        };
        public static void SetLanguage(string value)
        {
            if (value != "en" && value != "zh-CN") throw new ArgumentException("Unsupported language", "value");
            language = value;
        }
        public static string T(string key)
        {
            string result;
            return language == "en" && english.TryGetValue(key, out result) ? result : key;
        }
        public static string F(string key, params object[] args) { return String.Format(CultureInfo.CurrentCulture, T(key), args); }
        public static string EditLabel(string key)
        {
            if (key.StartsWith("撤销 ", StringComparison.Ordinal)) return T("撤销 ") + EditLabel(key.Substring(3));
            if (key.StartsWith("声望 ", StringComparison.Ordinal)) return T("声望 ") + key.Substring(3);
            return T(key);
        }
        public static void Load(string path)
        {
            language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh-CN" : "en";
            try
            {
                string saved = File.ReadAllText(path, Encoding.UTF8).Trim();
                if (saved == "en" || saved == "zh-CN") language = saved;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        public static void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, language, Encoding.UTF8);
        }
        // Static UI text uses the same key in either direction. Never translate user input or game data.
        public static void Apply(Control control)
        {
            if (control is Form || control is Label || control is Button || control is CheckBox)
            {
                string key = control.Tag as string;
                if (key == null)
                {
                    if (english.ContainsKey(control.Text)) key = control.Text;
                    else foreach (var pair in english) if (pair.Value == control.Text) { key = pair.Key; break; }
                    if (key != null) control.Tag = key;
                }
                if (key != null) control.Text = T(key);
            }
            foreach (Control child in control.Controls) Apply(child);
        }
    }
}
