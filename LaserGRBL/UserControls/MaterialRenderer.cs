using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace LaserGRBL.UserControls
{
    public class MaterialMenuRenderer : ToolStripProfessionalRenderer
    {
        public MaterialMenuRenderer() : base(new MaterialColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(ColorScheme.FormBackColor))
            {
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Modern flat menus have no separated gutter bar
            using (SolidBrush b = new SolidBrush(ColorScheme.FormBackColor))
            {
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);

            if (e.Item.Selected && e.Item.Enabled)
            {
                Color highlight = ColorScheme.MenuHighlightColor;
                using (GraphicsPath path = Tools.Graph.RoundedRect(rect, 4))
                using (SolidBrush brush = new SolidBrush(highlight))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            else if (e.Item.Pressed)
            {
                Color pressed = ColorScheme.PressedButtons;
                using (GraphicsPath path = Tools.Graph.RoundedRect(rect, 4))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(80, pressed)))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(ColorScheme.FormBackColor))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Color c;
            if (!e.Item.Enabled)
                c = ColorScheme.DisabledButtons;
            else if (e.Item.Selected)
                c = ColorScheme.DarkScheme ? Color.White : Color.FromArgb(15, 23, 42);
            else
                c = ColorScheme.FormForeColor;

            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, c, e.TextFormat);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(e.ImageRectangle.X - 1, e.ImageRectangle.Y - 1, e.ImageRectangle.Width + 2, e.ImageRectangle.Height + 2);

            // Draw modern rounded checkmark pill
            using (GraphicsPath path = Tools.Graph.RoundedRect(rect, 3))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(50, ColorScheme.PressedButtons)))
            using (Pen p = new Pen(ColorScheme.PressedButtons, 1.5f))
            {
                e.Graphics.FillPath(b, path);
                e.Graphics.DrawPath(p, path);

                // Draw checkmark symbol
                float x = rect.X + rect.Width * 0.25f;
                float y = rect.Y + rect.Height * 0.5f;
                PointF p1 = new PointF(x, y);
                PointF p2 = new PointF(rect.X + rect.Width * 0.45f, rect.Y + rect.Height * 0.75f);
                PointF p3 = new PointF(rect.X + rect.Width * 0.8f, rect.Y + rect.Height * 0.25f);

                using (Pen checkPen = new Pen(ColorScheme.PressedButtons, 2f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    e.Graphics.DrawLines(checkPen, new PointF[] { p1, p2, p3 });
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen p = new Pen(ColorScheme.MenuSeparatorColor, 1f))
            {
                e.Graphics.DrawLine(p, 8, y, e.Item.Width - 8, y);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                Rectangle rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                using (Pen p = new Pen(ColorScheme.ControlsBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, rect);
                }
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item.Enabled ? ColorScheme.FormForeColor : ColorScheme.DisabledButtons;
            base.OnRenderArrow(e);
        }
    }

    public class MaterialColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => ColorScheme.ControlsBorder;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => ColorScheme.MenuHighlightColor;
        public override Color MenuStripGradientBegin => ColorScheme.FormBackColor;
        public override Color MenuStripGradientEnd => ColorScheme.FormBackColor;
        public override Color ToolStripDropDownBackground => ColorScheme.FormBackColor;
        public override Color ImageMarginGradientBegin => ColorScheme.FormBackColor;
        public override Color ImageMarginGradientMiddle => ColorScheme.FormBackColor;
        public override Color ImageMarginGradientEnd => ColorScheme.FormBackColor;
        public override Color SeparatorDark => ColorScheme.MenuSeparatorColor;
        public override Color SeparatorLight => ColorScheme.MenuSeparatorColor;
        public override Color StatusStripGradientBegin => ColorScheme.FormBackColor;
        public override Color StatusStripGradientEnd => ColorScheme.FormBackColor;
        public override Color ButtonSelectedHighlight => ColorScheme.MenuHighlightColor;
        public override Color ButtonPressedHighlight => ColorScheme.PressedButtons;
    }
}
