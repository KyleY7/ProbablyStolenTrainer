using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ProbablyStolenTrainer
{
    public sealed class TrainerForm : Form
    {
        Session session;
        readonly Label status, currentCash, currentDay, currentRent, currentFaction;
        readonly NumericUpDown cash, day, rent, reputation;
        readonly ComboBox factions;
        readonly ComboBox language;
        readonly Button undo;
        readonly CheckBox appraisal;
        readonly CheckBox itemLibrary;
        readonly Label itemMenuKey;
        bool changingAppraisal;
        bool changingItemLibrary;
        bool changingLanguage;
        readonly Panel editing;
        readonly TextBox log;
        readonly Timer timer;
        readonly Color ink = UITheme.Ink;
        readonly Color muted = UITheme.Muted;
        readonly Color accent = UITheme.Accent;
        readonly List<Control> editable = new List<Control>();
        Snapshot last;
        string statusKey = "未连接  ·  先启动游戏并读入存档";
        bool preview;
        bool renderingPreview;
        protected override bool ShowWithoutActivation { get { return renderingPreview; } }
        public TrainerForm()
        {
            Text = L.T("Probably Stolen · 金钱 / 时间 / 声望 / 鉴定");
            Font = UITheme.Body(10F);
            BackColor = UITheme.Paper; ForeColor = ink;
            AutoScaleDimensions = new SizeF(96F, 96F);
            ClientSize = new Size(1040, 722); FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(820, 540); StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScroll = true; AutoScrollMinSize = new Size(1040, 722);
            Label prompt = LabelAt(this, ">_", 28, 18, 52, 38);
            prompt.Font = UITheme.Mono(23F, FontStyle.Bold); prompt.ForeColor = accent;
            Label title = LabelAt(this, "probably stolen", 85, 16, 505, 42);
            title.Font = UITheme.Mono(25F, FontStyle.Bold);
            Label edition = LabelAt(this, "TRAINER / v2.0.0", 595, 22, 217, 30);
            edition.Font = UITheme.Mono(10F); edition.ForeColor = muted;
            language = new ComboBox { Left = 832, Top = 26, Width = 178, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = UITheme.Surface, ForeColor = ink, AccessibleName = "Language / 语言" };
            language.Items.AddRange(new object[] { "简体中文", "English" });
            language.SelectedIndex = L.Language == "en" ? 1 : 0;
            Controls.Add(language);
            LabelAt(this, L.T("试玩版修改器  /  金钱 · 时间 · 势力声望 · 鉴定价值"), 30, 60, 920, 24).ForeColor = muted;
            var connection = new OutlinePanel { Left = 28, Top = 96, Width = 984, Height = 66 };
            Controls.Add(connection);
            status = LabelAt(connection, L.T("未连接  ·  先启动游戏并读入存档"), 18, 8, 710, 25);
            status.ForeColor = accent;
            LabelAt(connection, L.T("进入店铺后选择进程，当前数值会自动刷新。"), 18, 35, 710, 22).ForeColor = muted;
            ((TerminalButton)ButtonAt(connection, L.T("选择进程…"), 750, 14, 214, delegate { ConnectGame(); })).Primary = true;

            editing = new OutlinePanel { Left = 28, Top = 178, Width = 616, Height = 214 };
            Controls.Add(editing);
            SectionTitle(editing, "/values", L.T("数值修改"));
            LabelAt(editing, L.T("金钱"), 18, 53, 106, 28);
            currentCash = LabelAt(editing, L.T("当前：—"), 127, 53, 202, 28);
            currentCash.ForeColor = muted;
            cash = Number(editing, 340, 51, 0, 999999999, 100000, 0);
            editable.Add(ButtonAt(editing, L.T("设置金钱"), 480, 49, 118, delegate { Apply("cash", cash.Value, null); }));

            LabelAt(editing, L.T("天数"), 18, 105, 106, 28);
            currentDay = LabelAt(editing, L.T("当前：—"), 127, 105, 202, 28);
            currentDay.ForeColor = muted;
            day = Number(editing, 340, 103, 0, 99999, 1, 0);
            editable.Add(ButtonAt(editing, L.T("设置天数"), 480, 101, 118, delegate { Apply("day", day.Value, null); }));

            LabelAt(editing, L.T("租金倒计时"), 18, 157, 106, 28);
            currentRent = LabelAt(editing, L.T("当前：—"), 127, 157, 202, 28);
            currentRent.ForeColor = muted;
            rent = Number(editing, 340, 155, 0, 9999, 30, 0);
            editable.Add(ButtonAt(editing, L.T("设置剩余天数"), 480, 153, 118, delegate { Apply("rent", rent.Value, null); }));

            var factionPanel = new OutlinePanel { Left = 28, Top = 408, Width = 616, Height = 140 };
            Controls.Add(factionPanel); SectionTitle(factionPanel, "/reputation", L.T("势力声望"));
            factions = new ComboBox { Left = 18, Top = 49, Width = 302, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = UITheme.Paper, ForeColor = ink };
            factionPanel.Controls.Add(factions); editable.Add(factions);
            factions.SelectedIndexChanged += delegate { ShowFaction(); };
            currentFaction = LabelAt(factionPanel, L.T("当前：—"), 18, 86, 315, 27);
            reputation = Number(factionPanel, 340, 49, -200, 200, 100, 2);
            editable.Add(ButtonAt(factionPanel, L.T("设置选中势力"), 480, 47, 118, delegate
            {
                var selected = factions.SelectedItem as Faction;
                if (selected != null) Apply("reputation", reputation.Value, selected.Id);
            }));
            var repRange = LabelAt(factionPanel, L.T("声望范围：-200 ～ 200"), 18, 112, 316, 20);
            repRange.ForeColor = muted; repRange.Font = UITheme.Body(9F);
            editable.Add(ButtonAt(factionPanel, L.T("全部势力设为输入值"), 340, 92, 258, delegate { Apply("reputation", reputation.Value, null); }));

            var tools = new OutlinePanel { Left = 660, Top = 178, Width = 352, Height = 214 };
            Controls.Add(tools); SectionTitle(tools, "/tools", L.T("游戏辅助"));
            appraisal = new TerminalToggle { Text = L.T("直接显示鉴定物真实价值"), Left = 18, Top = 49, Width = 316, Height = 27 };
            tools.Controls.Add(appraisal); editable.Add(appraisal);
            appraisal.CheckedChanged += delegate { ToggleAppraisal(); };
            var appraisalHint = LabelAt(tools, L.T("重新悬停物品，查看游戏内 TRUE VALUE"), 18, 80, 316, 28);
            appraisalHint.ForeColor = muted; appraisalHint.Font = UITheme.Body(9F);
            itemLibrary = new TerminalToggle { Text = L.T("开启物品添加菜单"), Left = 18, Top = 117, Width = 316, Height = 27 };
            tools.Controls.Add(itemLibrary); editable.Add(itemLibrary);
            itemLibrary.CheckedChanged += delegate { ToggleItemLibrary(); };
            ButtonAt(tools, L.T("添加到后仓：使用说明"), 18, 162, 181, ShowItemLibraryHelp);
            itemMenuKey = LabelAt(tools, L.T("游戏内按键：—"), 208, 163, 126, 32);
            itemMenuKey.ForeColor = accent; itemMenuKey.Font = UITheme.Body(9F);
            var actions = new OutlinePanel { Left = 660, Top = 408, Width = 352, Height = 140 };
            Controls.Add(actions); SectionTitle(actions, "/session", L.T("会话操作"));
            ButtonAt(actions, L.T("刷新当前数值"), 18, 48, 151, delegate { Run(delegate { RefreshValues(false); }); });
            undo = ButtonAt(actions, L.T("撤销上一次修改"), 182, 48, 152, delegate { Run(delegate { RequireSession(); Record(session.Undo()); RefreshValues(false); }); });
            ButtonAt(actions, L.T("备份存档…"), 18, 93, 178, delegate { Run(Backup); });
            var topmost = new TerminalToggle { Text = L.T("窗口置顶"), Left = 208, Top = 97, Width = 126, Height = 28, Font = UITheme.Body(9F) };
            topmost.CheckedChanged += delegate { TopMost = topmost.Checked; }; actions.Controls.Add(topmost);
            var caution = LabelAt(this, L.T("修改前先暂停游戏。日期调整不会补跑剧情或结算；声望奖励由游戏后续刷新。"), 30, 562, 982, 25);
            caution.ForeColor = muted; caution.Font = UITheme.Body(9F);
            var activity = new OutlinePanel { Left = 28, Top = 600, Width = 984, Height = 72, BackColor = UITheme.Soft };
            Controls.Add(activity);
            var activityTitle = LabelAt(activity, "/activity", 16, 12, 114, 25);
            activityTitle.Font = UITheme.Mono(10F); activityTitle.ForeColor = muted;
            log = new TextBox { Left = 140, Top = 12, Width = 826, Height = 48, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = UITheme.Soft, ForeColor = muted, BorderStyle = BorderStyle.None, Font = UITheme.Mono(9F) };
            activity.Controls.Add(log);
            var footer = LabelAt(this, L.T("适配 Demo · Steam 构建 25382790  |  v2.0.0"), 30, 687, 641, 24);
            footer.ForeColor = muted; footer.Font = UITheme.Mono(9F);
            var learning = LabelAt(this, L.T("仅供学习，欢迎大家反馈。"), 680, 687, 330, 24);
            learning.TextAlign = ContentAlignment.MiddleRight; learning.ForeColor = muted; learning.Font = UITheme.Body(9F);
            Shown += delegate
            {
                if (renderingPreview) return;
                UITheme.FitToWorkingArea(this);
            };
            SetConnected(false);
            Log(L.T("就绪。点击“选择进程…”连接游戏，无需设置盘符。请在玩游戏的 Windows 内运行。"));
            timer = new Timer { Interval = 1200 };
            timer.Tick += delegate
            {
                if (session == null) return;
                try { RefreshValues(false); }
                catch (Exception ex) { Disconnect(); SetStatus("已断开 · 请重新连接"); Log(ex.Message); }
            };
            timer.Start();
            FormClosing += delegate
            {
                timer.Stop(); string warning = Disconnect();
                if (!String.IsNullOrEmpty(warning)) MessageBox.Show(this, warning, L.T("游戏功能恢复提示"));
            };
            FormClosed += delegate { timer.Dispose(); };
            language.SelectedIndexChanged += delegate
            {
                if (!changingLanguage) ChangeLanguage(language.SelectedIndex == 1 ? "en" : "zh-CN", true);
            };
            ApplyLanguage();
        }
        internal void ChangeLanguage(string code, bool persist)
        {
            L.SetLanguage(code);
            changingLanguage = true;
            try { language.SelectedIndex = code == "en" ? 1 : 0; }
            finally { changingLanguage = false; }
            ApplyLanguage();
            Log(L.T("语言已切换为简体中文。"));
            if (persist)
                try { L.Save(L.SettingsPath); }
                catch (Exception ex) { Log(L.T("无法保存语言偏好，下次启动可能恢复默认语言：") + ex.Message); }
        }
        void ApplyLanguage()
        {
            L.Apply(this);
            SetStatus(statusKey);
            DrawValues();
            DrawItemMenuKey();
        }
        void SetStatus(string key)
        {
            statusKey = key;
            status.Text = L.T(key) + (key == "已连接 · " && session != null ? session.ProcessDescription : "");
        }
        void DrawValues()
        {
            currentCash.Text = last == null ? L.T("当前：—") : L.F("当前：{0:N0}", last.Cash);
            currentDay.Text = last == null ? L.T("当前：—") : L.F("当前：{0} 天", last.Day);
            currentRent.Text = last == null ? L.T("当前：—") : L.F("当前：{0} 天 / 租金 {1}", last.RentDays, last.RentValue);
            if (preview && factions.Items.Count > 0) ((Faction)factions.Items[0]).Display = L.T("示例势力");
            ShowFaction();
        }
        Label LabelAt(Control parent, string text, int x, int y, int w, int h)
        {
            var label = new Label { Text = text, Left = x, Top = y, Width = w, Height = h, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ink, AutoEllipsis = true };
            parent.Controls.Add(label); return label;
        }
        Button ButtonAt(Control parent, string text, int x, int y, int width, Action action)
        {
            var button = new TerminalButton { Text = text, Left = x, Top = y, Width = width, Height = 36 };
            button.Click += delegate { action(); }; parent.Controls.Add(button); return button;
        }
        void SectionTitle(Control parent, string command, string caption)
        {
            var label = LabelAt(parent, command, 18, 12, 135, 24);
            label.Font = UITheme.Mono(11F); label.ForeColor = accent;
            var hint = LabelAt(parent, caption, 164, 12, parent.Width - 182, 24);
            hint.Font = UITheme.Body(9F); hint.ForeColor = muted;
        }
        NumericUpDown Number(Control parent, int x, int y, decimal min, decimal max, decimal value, int decimals)
        {
            var number = new NumericUpDown { Left = x, Top = y, Width = 124, Height = 32, Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals, ThousandsSeparator = true, BackColor = UITheme.Paper, ForeColor = ink, Font = UITheme.Mono(10F), BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(number); editable.Add(number); return number;
        }
        void Log(string text) { log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + text + Environment.NewLine); }
        void SetConnected(bool ready) { foreach (var control in editable) control.Enabled = ready; undo.Enabled = ready && session != null && session.CanUndo; }
        string Disconnect()
        {
            string warning = null;
            if (session != null) { session.Dispose(); warning = session.CloseWarning; }
            session = null; last = null; changingAppraisal = true;
            try { appraisal.Checked = false; } finally { changingAppraisal = false; }
            changingItemLibrary = true;
            try { itemLibrary.Checked = false; } finally { changingItemLibrary = false; }
            DrawItemMenuKey();
            SetConnected(false);
            if (!String.IsNullOrEmpty(warning)) Log(warning);
            return warning;
        }
        void ToggleAppraisal()
        {
            if (changingAppraisal) return;
            Run(delegate
            {
                RequireSession(); session.SetAppraisalVisible(appraisal.Checked);
                Log(session.AppraisalVisible ? L.T("真实价值显示已开启。重新悬停鉴定物，提示中将出现 TRUE VALUE。") : L.T("真实价值显示已关闭。重新悬停物品刷新提示。"));
            });
            changingAppraisal = true;
            try { appraisal.Checked = session != null && session.AppraisalVisible; }
            finally { changingAppraisal = false; }
        }
        void ToggleItemLibrary()
        {
            if (changingItemLibrary) return;
            Run(delegate
            {
                RequireSession(); session.SetItemLibraryEnabled(itemLibrary.Checked);
                if (session.ItemLibraryEnabled)
                    Log(L.F("物品菜单已开启。返回游戏按 {0}，从物品库拖到后仓空位；完成后先按同一键关闭菜单，再取消开关。", session.ItemMenuKey));
                else Log(L.T("物品添加快捷键已恢复。已加入后仓的物品保留，由游戏正常保存。"));
            });
            changingItemLibrary = true;
            try { itemLibrary.Checked = session != null && session.ItemLibraryEnabled; }
            finally { changingItemLibrary = false; }
            DrawItemMenuKey();
        }
        void DrawItemMenuKey()
        {
            if (itemMenuKey == null) return;
            string key = preview ? "F10" : session == null || !session.ItemLibraryEnabled ? null : session.ItemMenuKey;
            itemMenuKey.Text = key == null ? L.T("游戏内按键：—") : L.F("游戏内按键：{0}", key);
        }
        void ShowItemLibraryHelp()
        {
            MessageBox.Show(this, L.T("先备份存档并进入店铺。\n1. 连接游戏，勾选“开启物品添加菜单”。\n2. 返回游戏，按界面显示的快捷键打开物品库；首次打开可能需要稍等。\n3. 从游戏物品库把需要的物品拖到店铺后仓空位。物品库初始每种 10 个，实际取出数量与堆叠由游戏处理，可重复拖取。\n4. 先按同一快捷键关闭物品库，再取消开关。\n\n这不是输入物品 ID 后自动入库；物品要在游戏内选择和摆放。无法放下时先整理后仓。不要取用测试物、任务道具或不认识的内部物品。\n新增物品由游戏正常存档保存，不属于“撤销上一次修改”的范围。"), L.T("添加物品到后仓"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        void RequireSession() { if (session == null) throw new InvalidOperationException(L.T("请先连接游戏。")); }
        void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { Log(ex.Message); MessageBox.Show(this, ex.Message, L.T("操作未完成"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        void ConnectGame()
        {
            ProcessCandidate selected;
            timer.Stop();
            try
            {
                using (var picker = new ProcessPicker())
                {
                    if (picker.ShowDialog(this) != DialogResult.OK) return;
                    selected = picker.SelectedProcess;
                }
            }
            finally { timer.Start(); }
            Run(delegate
            {
                Disconnect(); SetStatus("正在校验版本…"); status.Refresh(); UseWaitCursor = true;
                try
                {
                    session = Session.Attach(selected); RefreshValues(true); SetConnected(true);
                    SetStatus("已连接 · ");
                    Log(L.T("游戏目录（来自所选进程）：") + session.GameDirectory);
                    Log(L.T("版本与对象校验通过。可以修改当前存档的数值。"));
                    Log(L.F("已读取当前游戏：金钱 {0:N0}，第 {1} 天，租金剩余 {2} 天。", last.Cash, last.Day, last.RentDays));
                }
                catch { Disconnect(); SetStatus("连接失败 · 请检查游戏状态"); throw; }
                finally { UseWaitCursor = false; }
            });
        }
        void RefreshValues(bool first)
        {
            RequireSession(); last = session.Refresh();
            var selection = factions.SelectedItem as Faction;
            string selectedId = selection == null ? null : selection.Id;
            bool sameList = factions.Items.Count == last.Factions.Count && factions.Items.Cast<Faction>().Select(f => f.Id).SequenceEqual(last.Factions.Select(f => f.Id));
            if (sameList)
            {
                for (int i = 0; i < last.Factions.Count; i++) ((Faction)factions.Items[i]).Amount = last.Factions[i].Amount;
            }
            else
            {
                factions.BeginUpdate(); factions.Items.Clear();
                foreach (var faction in last.Factions) factions.Items.Add(faction);
                factions.SelectedIndex = Math.Max(0, last.Factions.FindIndex(f => f.Id == selectedId));
                factions.EndUpdate();
            }
            DrawValues();
            if (first)
            {
                day.Value = Math.Max(day.Minimum, Math.Min(day.Maximum, last.Day));
                rent.Value = Math.Max(rent.Minimum, Math.Min(rent.Maximum, last.RentDays));
            }
            undo.Enabled = session.CanUndo;
        }
        void ShowFaction()
        {
            if (currentFaction == null) return;
            var faction = factions.SelectedItem as Faction;
            currentFaction.Text = faction == null ? L.T("当前：—") : L.F("当前声望：{0:0.##}", faction.Amount);
        }
        void Apply(string field, decimal value, string factionId)
        {
            Run(delegate { RequireSession(); Record(session.Apply(field, value, factionId)); RefreshValues(false); });
        }
        void Record(List<Edit> edits)
        {
            var lines = new List<string>();
            foreach (var edit in edits)
            {
                string before = edit.Before.Length == 4 ? BitConverter.ToInt32(edit.Before, 0).ToString(CultureInfo.InvariantCulture) : BitConverter.ToDouble(edit.Before, 0).ToString("R", CultureInfo.InvariantCulture);
                string after = edit.After.Length == 4 ? BitConverter.ToInt32(edit.After, 0).ToString(CultureInfo.InvariantCulture) : BitConverter.ToDouble(edit.After, 0).ToString("R", CultureInfo.InvariantCulture);
                string line = L.EditLabel(edit.Label) + L.T("：") + before + " → " + after;
                Log(line); lines.Add(DateTime.Now.ToString("o") + "\t" + line);
            }
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"); Directory.CreateDirectory(path);
                File.AppendAllLines(Path.Combine(path, "changes-" + DateTime.Now.ToString("yyyyMMdd") + ".txt"), lines, Encoding.UTF8);
            }
            catch (Exception ex) { Log(L.T("数值已修改，但操作日志保存失败：") + ex.Message); }
        }
        void Backup()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = L.T("选择包含 save_0.es3 等文件的 Probably Stolen 存档文件夹");
                dialog.ShowNewFolderButton = false;
                string normal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Questing Goose Studio", "Probably Stolen");
                if (Directory.Exists(normal)) dialog.SelectedPath = normal;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string[] files = Directory.GetFiles(dialog.SelectedPath, "*.es3", SearchOption.TopDirectoryOnly);
                if (!files.Any(f => Path.GetFileName(f).StartsWith("save_", StringComparison.OrdinalIgnoreCase))) throw new IOException(L.T("这个文件夹中没有 save_*.es3 存档。"));
                string output = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups"); Directory.CreateDirectory(output);
                string archive = Path.Combine(output, "ProbablyStolen-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".zip");
                string partial = archive + ".partial";
                try
                {
                    using (var file = new FileStream(partial, FileMode.CreateNew))
                    using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                        foreach (string save in files) zip.CreateEntryFromFile(save, Path.GetFileName(save), CompressionLevel.Optimal);
                    File.Move(partial, archive);
                }
                catch { if (File.Exists(partial)) File.Delete(partial); throw; }
                Log(L.T("已备份 ") + files.Length + L.T(" 个存档文件到 ") + archive);
                MessageBox.Show(this, L.T("备份已保存：\n") + archive, L.T("备份完成"));
            }
        }
        public void RenderPreview(string path, bool connected = true)
        {
            timer.Stop();
            renderingPreview = true; preview = connected;
            if (connected)
            {
                SetStatus("界面预览 · 示例数据，未连接游戏");
                last = new Snapshot { Cash = 12450, Day = 18, RentDays = 3, RentValue = 210 };
                factions.Items.Add(new Faction { Id = "EXAMPLE", Display = L.T("示例势力"), Amount = 24.5 }); factions.SelectedIndex = 0;
                foreach (var control in editable) control.Enabled = true;
                changingAppraisal = true; appraisal.Checked = true; changingAppraisal = false;
                changingItemLibrary = true; itemLibrary.Checked = true; changingItemLibrary = false;
                log.Text = L.T("界面测试使用示例值，未读取或修改真实游戏。");
            }
            ApplyLanguage();
            ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Location = new Point(-20000, -20000);
            Show(); Application.DoEvents(); PerformLayout();
            using (var bitmap = new Bitmap(Width, Height)) { DrawToBitmap(bitmap, new Rectangle(0, 0, Width, Height)); bitmap.Save(path); }
            Hide();
        }
    }
    static class Program
    {
        [STAThread] static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--patch-helper") return Tests.RunPatchHelper(args[1]);
            if (args.Length == 2 && args[0] == "--self-test") return Tests.Run(args[1]);
            if (args.Length == 2 && args[0] == "--verify-build")
            {
                try { BuildProfile.Verify(args[1]); return 0; } catch { return 1; }
            }
            L.Load(L.SettingsPath);
            // CLI override is useful for preview rendering and does not persist settings.
            if (args.Length >= 2 && args[0] == "--language")
            {
                if (args[1] != "en" && args[1] != "zh-CN") return 2;
                L.SetLanguage(args[1]); args = args.Skip(2).ToArray();
            }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 2 && args[0] == "--render-picker")
            {
                using (var picker = new ProcessPicker(true)) picker.RenderPreview(args[1]);
                return 0;
            }
            using (var form = new TrainerForm())
            {
                if (args.Length == 2 && args[0] == "--render-preview") { form.RenderPreview(args[1]); return 0; }
                if (args.Length == 2 && args[0] == "--render-idle") { form.RenderPreview(args[1], false); return 0; }
                Application.Run(form);
            }
            return 0;
        }
    }
}
