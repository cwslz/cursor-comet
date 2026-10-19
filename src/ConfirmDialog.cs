using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MouseTrail
{
    /// <summary>恢复默认设置前的警告确认框：淡红色、无标题栏、无右上角关闭按钮。</summary>
    internal sealed class ConfirmDialog : Form
    {
        private readonly float _scale;
        private bool _allowClose;
        private Button _cancel;

        public ConfirmDialog(string title, string message, string confirmText, string cancelText, float scale)
        {
            _scale = Math.Max(1f, scale);
            Font = FluentFonts.Create(10f, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = title;
            BackColor = FluentColors.ErrorContainer;
            Opacity = 0.96;
            DoubleBuffered = true;
            ClientSize = new Size((int)(440 * _scale), (int)(200 * _scale));

            int pad = (int)(24 * _scale);
            int iconSize = (int)(40 * _scale);
            Padding = new Padding(pad, pad, pad, pad);

            InfoDot icon = new InfoDot();
            icon.UiScale = _scale * 1.7f;
            icon.Width = iconSize;
            icon.Height = iconSize;
            icon.BackColor = FluentColors.ErrorContainer;
            icon.Cursor = Cursors.Default;
            icon.TabStop = false;
            icon.Location = new Point(pad, pad);
            Controls.Add(icon);

            int textLeft = pad + iconSize + (int)(16 * _scale);

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.AutoSize = true;
            titleLabel.Font = FluentFonts.Create(16f, FontStyle.Bold);
            titleLabel.ForeColor = FluentColors.Error;
            titleLabel.BackColor = FluentColors.ErrorContainer;
            titleLabel.Location = new Point(textLeft, pad - (int)(2 * _scale));
            Controls.Add(titleLabel);

            Label messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.AutoSize = true;
            messageLabel.MaximumSize = new Size(ClientSize.Width - textLeft - pad, 0);
            messageLabel.Font = FluentFonts.Create(10f, FontStyle.Regular);
            messageLabel.ForeColor = FluentColors.OnSurface;
            messageLabel.BackColor = FluentColors.ErrorContainer;
            messageLabel.Location = new Point(textLeft, pad + (int)(32 * _scale));
            Controls.Add(messageLabel);

            FluentButton confirm = new FluentButton();
            confirm.Text = confirmText;
            confirm.FillColor = FluentColors.Error;
            confirm.TextColor = Color.White;
            confirm.BorderColor = FluentColors.Error;
            confirm.Height = (int)(34 * _scale);
            confirm.Font = FluentFonts.Create(10f, FontStyle.Bold);
            confirm.Margin = new Padding((int)(10 * _scale), 0, 0, 0);
            confirm.Click += delegate
            {
                _allowClose = true;
                DialogResult = DialogResult.OK;
                Close();
            };

            Button cancel = new Button();
            cancel.Text = cancelText;
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderSize = 1;
            cancel.FlatAppearance.BorderColor = FluentColors.Stroke;
            cancel.FlatAppearance.MouseOverBackColor = FluentColors.SubtleHover;
            cancel.FlatAppearance.MouseDownBackColor = FluentColors.SubtlePressed;
            cancel.BackColor = Color.White;
            cancel.UseVisualStyleBackColor = false;
            cancel.ForeColor = FluentColors.OnSurface;
            cancel.Height = (int)(34 * _scale);
            cancel.AutoSize = true;
            cancel.MinimumSize = new Size((int)(84 * _scale), (int)(30 * _scale));
            cancel.Padding = new Padding((int)(12 * _scale), 4, (int)(12 * _scale), 4);
            cancel.Font = FluentFonts.Create(10f, FontStyle.Regular);
            cancel.Cursor = Cursors.Hand;
            cancel.Margin = new Padding(0);
            _cancel = cancel;
            cancel.Resize += delegate
            {
                using (GraphicsPath cancelPath = FluentDraw.RoundRect(
                    new Rectangle(0, 0, cancel.Width, cancel.Height), (int)(4 * _scale)))
                {
                    cancel.Region = new Region(cancelPath);
                }
            };
            cancel.Click += delegate
            {
                _allowClose = true;
                DialogResult = DialogResult.Cancel;
                Close();
            };

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = (int)(42 * _scale);
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;
            actions.BackColor = FluentColors.ErrorContainer;
            actions.Padding = new Padding(0);
            actions.Controls.Add(confirm);
            actions.Controls.Add(cancel);
            Controls.Add(actions);

            AcceptButton = confirm;
            CancelButton = cancel;
            ActiveControl = confirm;

            using (GraphicsPath path = FluentDraw.RoundRect(
                new Rectangle(0, 0, Width, Height), (int)(8 * _scale)))
            {
                Region = new Region(path);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_cancel == null)
            {
                return;
            }
            using (GraphicsPath path = FluentDraw.RoundRect(
                new Rectangle(0, 0, _cancel.Width, _cancel.Height), (int)(4 * _scale)))
            {
                _cancel.Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = FluentDraw.RoundRect(
                new Rectangle(0, 0, Width - 1, Height - 1), (int)(8 * _scale)))
            using (Pen pen = new Pen(FluentColors.Error, Math.Max(1.5f, 1.6f * _scale)))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (GraphicsPath path = FluentDraw.RoundRect(
                new Rectangle(0, 0, Width, Height), (int)(8 * _scale)))
            {
                Region = new Region(path);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 没有标题栏关闭按钮，也不允许 Alt+F4 绕过确认；只能点确认或取消。
            if (!_allowClose)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
