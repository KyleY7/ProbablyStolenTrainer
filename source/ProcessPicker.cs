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
        bool renderingPreview;
        protected override bool ShowWithoutActivation { get { return renderingPreview; } }
        public ProcessCandidate SelectedProcess { get; private set; }

        public ProcessPicker(bool preview = false)
        {
            Text = L.T("选择游戏进程"); Font = UITheme.Body(10F);
            ClientSize = new Size(920, 558); FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(680, 440); AutoScroll = true; AutoScrollMinSize = new Size(920, 558);
            StartPosition = FormStartPosition.CenterParent; MinimizeBox = false; MaximizeBox = false;
            AutoScaleDimensions = new SizeF(96F, 96F); AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = UITheme.Paper; ForeColor = UITheme.Ink;
            Shown += delegate { if (!renderingPreview) UITheme.FitToWorkingArea(this); };
            Controls.Add(new Label { Text = "> " + L.T("选择游戏进程"), Left = 24, Top = 24, Width = 850, Height = 35, Font = UITheme.Mono(18F, FontStyle.Bold), ForeColor = UITheme.Ink });
            Controls.Add(new Label { Text = L.T("请选择游戏进程。双击一行也可以连接。"), Left = 27, Top = 65, Width = 847, Height = 25, Font = UITheme.Body(9.5F), ForeColor = UITheme.Muted });
            Controls.Add(new Label { Text = L.T("搜索名称 / PID / 路径"), Left = 25, Top = 103, Width = 195, Height = 26, ForeColor = UITheme.Muted });
            var searchFrame = new OutlinePanel { Left = 229, Top = 97, Width = 475, Height = 38, BackColor = UITheme.Surface };
            search = new TextBox { Left = 12, Top = 7, Width = 451, Text = "Probably", BorderStyle = BorderStyle.None, BackColor = UITheme.Surface, ForeColor = UITheme.Ink, Font = UITheme.Mono(11F) };
            searchFrame.Controls.Add(search); Controls.Add(searchFrame);
            var refresh = MakeButton(L.T("刷新列表"), 720, 96, 176); refresh.Click += delegate { Reload(); };
            var listFrame = new OutlinePanel { Left = 24, Top = 151, Width = 872, Height = 245, Padding = new Padding(1), BackColor = UITheme.Surface };
            list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, BackColor = UITheme.Surface, ForeColor = UITheme.Ink, BorderStyle = BorderStyle.None, OwnerDraw = true, Font = UITheme.Body(10F) };
            list.Columns.Add(L.T("进程名"), 216); list.Columns.Add("PID", 85); list.Columns.Add(L.T("程序路径"), 542);
            var rowSpacing = new ImageList { ImageSize = new Size(1, 33) };
            list.SmallImageList = rowSpacing;
            FormClosed += delegate { rowSpacing.Dispose(); };
            list.DrawColumnHeader += delegate(object sender, DrawListViewColumnHeaderEventArgs e)
            {
                using (var brush = new SolidBrush(UITheme.Soft)) e.Graphics.FillRectangle(brush, e.Bounds);
                using (var pen = new Pen(UITheme.Border)) e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                var bounds = new Rectangle(e.Bounds.Left + 12, e.Bounds.Top, e.Bounds.Width - 18, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, list.Font, bounds, UITheme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            };
            list.DrawSubItem += delegate(object sender, DrawListViewSubItemEventArgs e)
            {
                var background = e.Item.Selected ? UITheme.Soft : UITheme.Surface;
                using (var brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
                if (e.Item.Selected && e.ColumnIndex == 0)
                    using (var brush = new SolidBrush(UITheme.Accent)) e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Left, e.Bounds.Top, 3, e.Bounds.Height));
                using (var pen = new Pen(UITheme.Border)) e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                var bounds = new Rectangle(e.Bounds.Left + 12, e.Bounds.Top, e.Bounds.Width - 18, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, list.Font, bounds, UITheme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            };
            listFrame.Controls.Add(list); Controls.Add(listFrame);
            var detailsFrame = new OutlinePanel { Left = 24, Top = 412, Width = 872, Height = 60, BackColor = UITheme.Soft };
            details = new TextBox { Left = 12, Top = 9, Width = 848, Height = 43, Multiline = true, ReadOnly = true, BackColor = UITheme.Soft, ForeColor = UITheme.Muted, BorderStyle = BorderStyle.None, Font = UITheme.Mono(9.5F) };
            detailsFrame.Controls.Add(details); Controls.Add(detailsFrame);
            count = new Label { Left = 24, Top = 492, Width = 574, Height = 53, Text = L.T("正在读取进程…"), ForeColor = UITheme.Muted, Font = UITheme.Body(9F) }; Controls.Add(count);
            connect = MakeButton(L.T("连接选中进程"), 610, 494, 180); ((TerminalButton)connect).Primary = true; connect.Enabled = false;
            connect.Click += delegate { Choose(); };
            var cancel = MakeButton(L.T("取消"), 802, 494, 94); cancel.DialogResult = DialogResult.Cancel;
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
            var button = new TerminalButton { Text = text, Left = x, Top = y, Width = width, Height = 40, Font = UITheme.Body(10F) };
            Controls.Add(button); return button;
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
            renderingPreview = true;
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
