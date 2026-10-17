using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SimpleHMS
{
    public class MacOSButton : Button
    {
        public enum ButtonType
        {
            Close,
            Minimize,
            Maximize
        }

        private ButtonType _buttonType;
        private bool _isHovered = false;
        private Timer _hoverTimer;
        private float _iconOpacity = 0f;

        public MacOSButton(ButtonType type)
        {
            _buttonType = type;
            this.Size = new Size(12, 12);
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.FlatAppearance.MouseOverBackColor = Color.Transparent;
            this.FlatAppearance.MouseDownBackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;

            _hoverTimer = new Timer();
            _hoverTimer.Interval = 20;
            _hoverTimer.Tick += (sender, e) => HoverTimer_Tick(sender!, e);

            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            if (_isHovered && _iconOpacity < 1f)
            {
                _iconOpacity += 0.15f;
                if (_iconOpacity >= 1f)
                {
                    _iconOpacity = 1f;
                    _hoverTimer.Stop();
                }
                this.Invalidate();
            }
            else if (!_isHovered && _iconOpacity > 0f)
            {
                _iconOpacity -= 0.15f;
                if (_iconOpacity <= 0f)
                {
                    _iconOpacity = 0f;
                    _hoverTimer.Stop();
                }
                this.Invalidate();
            }
        }

        private Color GetButtonColor()
        {
            switch (_buttonType)
            {
                case ButtonType.Close:
                    return Color.FromArgb(255, 95, 86);
                case ButtonType.Minimize:
                    return Color.FromArgb(255, 189, 46);
                case ButtonType.Maximize:
                    return Color.FromArgb(40, 201, 64);
                default:
                    return Color.Gray;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(1, 1, this.Width - 2, this.Height - 2);

                using (PathGradientBrush shadowBrush = new PathGradientBrush(path))
                {
                    shadowBrush.CenterColor = GetButtonColor();
                    shadowBrush.SurroundColors = new Color[] { Color.FromArgb(30, 0, 0, 0) };
                    shadowBrush.CenterPoint = new PointF(this.Width / 2f - 0.5f, this.Height / 2f - 0.5f);

                    e.Graphics.FillEllipse(shadowBrush, 0, 0, this.Width - 1, this.Height - 1);
                }

                using (SolidBrush brush = new SolidBrush(GetButtonColor()))
                {
                    e.Graphics.FillEllipse(brush, 1, 1, this.Width - 3, this.Height - 3);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(40, 0, 0, 0), 0.5f))
                {
                    e.Graphics.DrawEllipse(borderPen, 1, 1, this.Width - 3, this.Height - 3);
                }
            }

            if (_iconOpacity > 0)
            {
                int alpha = (int)(255 * _iconOpacity);
                using (Pen pen = new Pen(Color.FromArgb(alpha, 0, 0, 0), 1.2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    float centerX = this.Width / 2f;
                    float centerY = this.Height / 2f;

                    switch (_buttonType)
                    {
                        case ButtonType.Close:
                            e.Graphics.DrawLine(pen, centerX - 2.5f, centerY - 2.5f, centerX + 2.5f, centerY + 2.5f);
                            e.Graphics.DrawLine(pen, centerX + 2.5f, centerY - 2.5f, centerX - 2.5f, centerY + 2.5f);
                            break;
                        case ButtonType.Minimize:
                            e.Graphics.DrawLine(pen, centerX - 3f, centerY, centerX + 3f, centerY);
                            break;
                        case ButtonType.Maximize:
                            e.Graphics.DrawLine(pen, centerX - 2.5f, centerY - 1.5f, centerX - 2.5f, centerY - 2.5f);
                            e.Graphics.DrawLine(pen, centerX - 2.5f, centerY - 2.5f, centerX - 1.5f, centerY - 2.5f);
                            e.Graphics.DrawLine(pen, centerX + 2.5f, centerY + 1.5f, centerX + 2.5f, centerY + 2.5f);
                            e.Graphics.DrawLine(pen, centerX + 2.5f, centerY + 2.5f, centerX + 1.5f, centerY + 2.5f);
                            e.Graphics.DrawLine(pen, centerX - 1f, centerY - 1f, centerX - 2f, centerY - 2f);
                            e.Graphics.DrawLine(pen, centerX + 1f, centerY + 1f, centerX + 2f, centerY + 2f);
                            break;
                    }
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            _hoverTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _hoverTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hoverTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}