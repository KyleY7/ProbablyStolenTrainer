using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProbablyStolenTrainer
{
    public sealed class ProcessCandidate
    {
        public int Id;
        public string Name, ExecutablePath;
        public long StartedUtcTicks;
    }

    public static class ProcessCatalog
    {
        public static List<ProcessCandidate> Capture()
        {
            var result = new List<ProcessCandidate>();
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        var entry = new ProcessCandidate { Id = process.Id, Name = process.ProcessName, ExecutablePath = L.T("（路径不可读）") };
                        try { entry.StartedUtcTicks = process.StartTime.ToUniversalTime().Ticks; } catch (Exception) { }
                        try { if (process.MainModule != null) entry.ExecutablePath = process.MainModule.FileName; } catch (Exception) { }
                        result.Add(entry);
                    }
                    catch (Exception) { /* A process may exit while the list is being read. */ }
                }
            }
            return result;
        }
        public static List<ProcessCandidate> Filter(IEnumerable<ProcessCandidate> source, string query)
        {
            query = (query ?? "").Trim();
            return source.Where(p => query.Length == 0 || p.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || p.Id.ToString() == query || p.ExecutablePath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(p => p.Name.IndexOf("Probably", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Id).ToList();
        }
    }

    public sealed class ProcessPicker : Form
    {
        readonly TextBox search, details;
        readonly ListView list;
        readonly Label count;
        readonly Button connect;
        List<ProcessCandidate> processes = new List<ProcessCandidate>();
        public ProcessCandidate SelectedProcess { get; private set; }

        public ProcessPicker(bool preview = false)
        {
            Text = L.T("选择游戏进程"); Font = new Font("Microsoft YaHei UI", 10F);
            ClientSize = new Size(920, 552); FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; MinimizeBox = false; MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(17, 25, 38); ForeColor = Color.FromArgb(225, 232, 244);
            Controls.Add(new Label { Text = L.T("搜索名称 / PID / 路径"), Left = 18, Top = 20, Width = 190, Height = 28 });
            search = new TextBox { Left = 209, Top = 17, Width = 519, Text = "Probably" };
            Controls.Add(search);
            var refresh = MakeButton(L.T("刷新列表"), 744, 14, 158); refresh.Click += delegate { Reload(); };
            list = new ListView { Left = 18, Top = 66, Width = 884, Height = 324, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, BackColor = Color.FromArgb(29, 40, 56), ForeColor = ForeColor };
            list.Columns.Add(L.T("进程名"), 222); list.Columns.Add("PID", 88); list.Columns.Add(L.T("程序路径"), 545);
            Controls.Add(list);
            details = new TextBox { Left = 18, Top = 407, Width = 884, Height = 52, Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(12, 19, 29), ForeColor = ForeColor, BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(details);
            count = new Label { Left = 18, Top = 472, Width = 593, Height = 64, Text = L.T("正在读取进程…") }; Controls.Add(count);
            connect = MakeButton(L.T("连接选中进程"), 621, 490, 170); connect.Enabled = false;
            connect.Click += delegate { Choose(); };
            var cancel = MakeButton(L.T("取消"), 802, 490, 100); cancel.DialogResult = DialogResult.Cancel;
            CancelButton = cancel; AcceptButton = connect;
            search.TextChanged += delegate { Populate(); };
            list.SelectedIndexChanged += delegate
            {
                var selected = Current(); connect.Enabled = selected != null;
                details.Text = selected == null ? L.T("请选择游戏进程。双击一行也可以连接。") : selected.Name + ".exe  ·  PID " + selected.Id + Environment.NewLine + selected.ExecutablePath;
            };
            list.DoubleClick += delegate { Choose(); };
            if (preview)
            {
                processes.Add(new ProcessCandidate { Id = 1234, Name = "Probably Stolen", StartedUtcTicks = 1, ExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen Demo\Probably Stolen.exe" });
                Populate();
            }
            else Shown += delegate { Reload(); };
        }
        Button MakeButton(string text, int x, int y, int width)
        {
            var button = new Button { Text = text, Left = x, Top = y, Width = width, Height = 38, BackColor = Color.FromArgb(41, 65, 79), ForeColor = ForeColor, FlatStyle = FlatStyle.Flat };
            button.FlatAppearance.BorderColor = Color.FromArgb(62, 103, 111); Controls.Add(button); return button;
        }
        ProcessCandidate Current() { return list.SelectedItems.Count == 0 ? null : list.SelectedItems[0].Tag as ProcessCandidate; }
        void Reload()
        {
            UseWaitCursor = true;
            try { processes = ProcessCatalog.Capture(); Populate(); }
            catch (Exception ex) { count.Text = L.T("读取进程列表失败：") + ex.Message; }
            finally { UseWaitCursor = false; }
        }
        void Populate()
        {
            var previous = Current(); var visible = ProcessCatalog.Filter(processes, search.Text);
            list.BeginUpdate(); list.Items.Clear();
            foreach (var process in visible)
            {
                var row = new ListViewItem(new string[] { process.Name + ".exe", process.Id.ToString(), process.ExecutablePath }) { Tag = process };
                list.Items.Add(row);
                if (previous != null && process.Id == previous.Id && process.StartedUtcTicks == previous.StartedUtcTicks) row.Selected = true;
            }
            list.EndUpdate();
            if (visible.Count == 1 && list.SelectedItems.Count == 0) list.Items[0].Selected = true;
            count.Text = visible.Count == 0 ? L.T("未找到匹配进程。请启动游戏后刷新，或清空搜索框。\n请在玩游戏的 Windows 系统内运行修改器。") : L.T("显示 ") + visible.Count + L.T(" 个进程；清空搜索框可查看全部。\n连接后自动识别目录，不需要设置盘符。");
            connect.Enabled = Current() != null;
            if (Current() == null) details.Text = L.T("请选择游戏进程。双击一行也可以连接。");
        }
        void Choose()
        {
            var selected = Current(); if (selected == null) return;
            if (selected.StartedUtcTicks == 0)
            {
                MessageBox.Show(this, L.T("无法读取这个进程的启动信息。请确认选中了游戏，且游戏和修改器使用相同的用户与权限。"), L.T("无法选择"));
                return;
            }
            SelectedProcess = selected; DialogResult = DialogResult.OK; Close();
        }
        public void RenderPreview(string path)
        {
            Text = L.T("选择游戏进程（界面预览 · 示例数据）");
            ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Location = new Point(-20000, -20000);
            Show(); Application.DoEvents();
            if (list.Items.Count > 0) { list.Items[0].Selected = true; list.Select(); }
            PerformLayout();
            using (var bitmap = new Bitmap(Width, Height)) { DrawToBitmap(bitmap, new Rectangle(0, 0, Width, Height)); bitmap.Save(path); }
            Hide();
        }
    }
}
