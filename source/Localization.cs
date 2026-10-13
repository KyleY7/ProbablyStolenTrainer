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
            { "无法读取对象：{0}。", "Cannot read object: {0}." },
            { "对象为空：{0}。请查看连接诊断日志。", "Object is null: {0}. See the connection diagnostic log." },
            { "对象指针无效：{0}。请查看连接诊断日志。", "Invalid object pointer: {0}. See the connection diagnostic log." },
            { "连接诊断已保存：", "Connection diagnostics saved: " },
            { "诊断日志保存失败：", "Could not save diagnostics: " },
            { "无法保存连接诊断。", "Could not save connection diagnostics." },
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
            { "适配 Demo · Steam 构建 25382790  |  v1.1.1  |  选择进程，自动识别路径", "Demo · Steam build 25382790  |  v1.1.1  |  Automatic path detection" },
            { "语言已切换为简体中文。", "Language changed to English." },
            { "无法保存语言偏好，下次启动可能恢复默认语言：", "Could not save the language preference; the next launch may use the default: " },
            { "当前：{0:N0}", "Current: {0:N0}" },
            { "当前：{0} 天", "Current: {0} days" },
            { "当前：{0} 天 / 租金 {1}", "Current: {0}d / rent {1}" },
            { "当前声望：{0:0.##}", "Current reputation: {0:0.##}" },
            { "物品菜单代码不匹配，已停止操作。请重启游戏并确认版本。", "Item-menu code does not match. Restart the game and check its version." },
            { "物品菜单写后校验失败。", "Item-menu verification failed after writing." },
            { "物品菜单切换失败，已恢复原状态。", "Item-menu toggle failed; the original state was restored. " },
            { "物品菜单切换失败且未能完全恢复，请重启游戏。", "Item-menu toggle failed and could not be fully restored. Restart the game. " },
            { "无法安全切换物品菜单，请检查游戏进程权限。", "Cannot safely toggle the item menu. Check game process permissions." },
            { "游戏线程未能恢复运行，请重启游戏。", "Game threads could not be resumed. Restart the game." },
            { "无法检查游戏线程状态，请稍后重试。", "Cannot check the game thread state. Try again shortly." },
            { "物品菜单正在处理操作，请松开按键并稍后重试。", "The item menu is busy. Release the keys and try again shortly." },
            { "物品菜单写入失败。", "Item-menu write failed." },
            { "物品菜单代码已被其他修改改变，请重启游戏。", "Another change has modified the item-menu code. Restart the game." },
            { "物品菜单快捷键无法识别，请检查游戏按键设置。", "Cannot identify the item-menu shortcut. Check the game's key settings." },
            { "后仓物品栏尚未就绪，请进入店铺后重试。", "The back-room inventory is not ready. Enter the shop and try again." },
            { "游戏物品菜单尚未初始化，请进入店铺后重试。", "The game's item menu is not initialized. Enter the shop and try again." },
            { "当前连接不支持物品添加菜单。", "This connection does not support the item menu." },
            { "物品菜单未能恢复，请重启游戏。", "The item menu could not be restored. Restart the game. " },
            { "开启物品添加菜单", "Enable item library" },
            { "添加到后仓：使用说明", "Add to back room: help" },
            { "游戏内按键：—", "In-game key: —" },
            { "游戏内按键：{0}", "In-game key: {0}" },
            { "游戏功能恢复提示", "Game feature restoration" },
            { "物品菜单已开启。返回游戏按 {0}，从物品库拖到后仓空位；完成后先按同一键关闭菜单，再取消开关。", "Item library enabled. Press {0} in game and drag items into free back-room slots. Close the menu with the same key before disabling the toggle." },
            { "物品添加快捷键已恢复。已加入后仓的物品保留，由游戏正常保存。", "Item-menu shortcut restored. Items already added remain in the game and follow normal saving." },
            { "添加物品到后仓", "Add items to the back room" },
            { "先备份存档并进入店铺。\n1. 连接游戏，勾选“开启物品添加菜单”。\n2. 返回游戏，按界面显示的快捷键打开物品库；首次打开可能需要稍等。\n3. 从游戏物品库把需要的物品拖到店铺后仓空位。物品库初始每种 10 个，实际取出数量与堆叠由游戏处理，可重复拖取。\n4. 先按同一快捷键关闭物品库，再取消开关。\n\n这不是输入物品 ID 后自动入库；物品要在游戏内选择和摆放。无法放下时先整理后仓。不要取用测试物、任务道具或不认识的内部物品。\n新增物品由游戏正常存档保存，不属于“撤销上一次修改”的范围。", "Back up your save and enter the shop.\n1. Connect and enable the item library.\n2. Press the displayed key in game to open the library. The first load may take a moment.\n3. Drag the desired items into free slots in the back room. Library entries start with 10 units; the game handles removal quantities and stacking. You can take items repeatedly.\n4. Close the library with the same key before disabling the toggle.\n\nChoose and place items inside the game; this does not insert an item ID automatically. Make space if placement fails. Avoid test objects, quest items and unfamiliar internal objects.\nAdded items follow normal game saving and are outside Undo last edit." },
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
