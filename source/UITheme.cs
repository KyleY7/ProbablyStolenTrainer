using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProbablyStolenTrainer
{
    public static class UITheme
    {
        public static readonly Color Paper = ColorTranslator.FromHtml("#F7F5EF");
        public static readonly Color Surface = ColorTranslator.FromHtml("#FEFDF9");
        public static readonly Color Soft = ColorTranslator.FromHtml("#EEEAE2");
        public static readonly Color Ink = ColorTranslator.FromHtml("#302B26");
        public static readonly Color Muted = ColorTranslator.FromHtml("#766F66");
        public static readonly Color Accent = ColorTranslator.FromHtml("#B95735");
        public static readonly Color Border = ColorTranslator.FromHtml("#DDD7CC");
        public static Font Body(float size = 10F) { return new Font("Microsoft YaHei UI", size); }
        public static Font Mono(float size = 10F, FontStyle style = FontStyle.Regular) { return new Font("Consolas", size, style); }
        public static void FitToWorkingArea(Form form)
        {
            Rectangle area = Screen.FromControl(form).WorkingArea;
            int width = Math.Max(1, area.Width - 32), height = Math.Max(1, area.Height - 32);
            form.MinimumSize = new Size(Math.Min(form.MinimumSize.Width, width), Math.Min(form.MinimumSize.Height, height));
            form.Size = new Size(Math.Min(form.Width, width), Math.Min(form.Height, height));
            form.Location = new Point(area.Left + (area.Width - form.Width) / 2, area.Top + (area.Height - form.Height) / 2);
        }
    }

    public sealed class OutlinePanel : Panel
    {
        public OutlinePanel()
        {
            BackColor = UITheme.Surface;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(UITheme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }

    public sealed class TerminalButton : Button
    {
        bool primary, hovered, pressed;
        public bool Primary { get { return primary; } set { primary = value; Invalidate(); } }
        public TerminalButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = UITheme.Surface; ForeColor = UITheme.Ink;
            Font = UITheme.Body(9.5F); Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { pressed = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { hovered = pressed = false; Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { pressed = true; Invalidate(); } base.OnKeyDown(e); }
        protected override void OnKeyUp(KeyEventArgs e) { pressed = false; Invalidate(); base.OnKeyUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Color fill = !Enabled ? UITheme.Soft : primary ? (pressed ? Color.FromArgb(151, 64, 36) : hovered ? Color.FromArgb(168, 75, 42) : UITheme.Accent) : pressed || hovered ? UITheme.Soft : UITheme.Surface;
            Color text = !Enabled ? UITheme.Muted : primary ? UITheme.Surface : UITheme.Ink;
            e.Graphics.Clear(fill);
            using (var pen = new Pen(Enabled && primary ? UITheme.Accent : UITheme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(6, 2, Math.Max(1, Width - 12), Math.Max(1, Height - 4)), text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (Focused && Enabled)
                ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(4, 4, Math.Max(1, Width - 8), Math.Max(1, Height - 8)), text, fill);
        }
    }

    public sealed class TerminalToggle : CheckBox
    {
        public TerminalToggle()
        {
            FlatStyle = FlatStyle.Flat;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = UITheme.Body(9.5F); ForeColor = UITheme.Ink;
            UseVisualStyleBackColor = false; Cursor = Cursors.Hand;
        }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            Rectangle box = new Rectangle(1, (Height - 14) / 2, 14, 14);
            using (var brush = new SolidBrush(Checked ? Enabled ? UITheme.Accent : UITheme.Muted : UITheme.Surface)) e.Graphics.FillRectangle(brush, box);
            using (var pen = new Pen(Checked && Enabled ? UITheme.Accent : UITheme.Border)) e.Graphics.DrawRectangle(pen, box);
            if (Checked)
                using (var pen = new Pen(UITheme.Surface, 2F))
                    e.Graphics.DrawLines(pen, new[] { new Point(box.Left + 3, box.Top + 7), new Point(box.Left + 6, box.Top + 10), new Point(box.Left + 11, box.Top + 4) });
            Color text = Enabled ? UITheme.Ink : UITheme.Muted;
            Rectangle label = new Rectangle(23, 0, Math.Max(1, Width - 23), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, label, text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (Focused && Enabled) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(20, 3, Math.Max(1, Width - 22), Math.Max(1, Height - 6)), text, BackColor);
        }
    }
}
