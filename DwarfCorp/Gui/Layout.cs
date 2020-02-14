using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Gui
{
    public partial class Widget
    {
        /// <summary>
        /// Run the layout engine on this widget and all children.
        /// Warning: This operation invalidates the entire tree and potentially moves all children widgets.
        /// </summary>
        public virtual void Layout()
        {
            if (Root == null)
                return;

            Root.SafeCall(this.OnLayout, this);
            var inside = GetDrawableInterior().Interior(InteriorMargin);
            foreach (var child in Children)
                inside = LayoutChild(inside, Padding, child);
            Invalidate();
        }

        private static int Clamp(int What, int Min, int Max)
        {
            if (What < Min) return Min;
            if (What > Max) return Max;
            return What;
        }

        private static Point GetClampedChildSize(Widget Child, Point Proposed)
        {
            return new Point(
                Clamp(Proposed.X, Child.MinimumSize.X, Child.MaximumSize.X),
                Clamp(Proposed.Y, Child.MinimumSize.Y, Child.MaximumSize.Y));
        }

        // Todo: 'Flow' options. Pack widgets up in rows until full, flow...
        // Must track rects in each corner used for flow.
        private static Rectangle LayoutChild(Rectangle Inside, Margin Padding, Widget Child)
        {
            Rectangle newPos;

            var size = Child.GetBestSize();
            
            switch (Child.AutoLayout)
            {
                case AutoLayout.None:
                    newPos = Child.Rect;
                    break;
                case AutoLayout.DockTop:
                    size = GetClampedChildSize(Child, new Point(Inside.Width - Padding.Horizontal, size.Y));
                    newPos = new Rectangle(Inside.X + Padding.Left, Inside.Y + Padding.Top, size.X, size.Y);
                    Inside = Inside.Interior(0, size.Y + Padding.Top, 0, 0);
                    break;
                case AutoLayout.DockTopCentered:
                    // Same as dock top, unless the widget is too small.
                    size = GetClampedChildSize(Child, new Point(Inside.Width - Padding.Horizontal, size.Y));
                    newPos = new Rectangle(Inside.X + Padding.Left + (Inside.Width - Padding.Horizontal - size.X) / 2, Inside.Y + Padding.Top, size.X, size.Y);
                    Inside = Inside.Interior(0, size.Y + Padding.Top, 0, 0);
                    break;
                case AutoLayout.DockRight:
                    size = GetClampedChildSize(Child, new Point(size.X, Inside.Height - Padding.Vertical));
                    newPos = new Rectangle(Inside.Right - size.X - Padding.Right, 
                        Inside.Y + Padding.Top, size.X, size.Y);
                    Inside = Inside.Interior(0, 0, size.X + Padding.Right, 0);
                    break;
                case AutoLayout.DockRightCentered:
                    // Same as dock right, except is the widget is too small, center it.
                    size = GetClampedChildSize(Child, new Point(size.X, Inside.Height - Padding.Vertical));
                    newPos = new Rectangle(Inside.Right - size.X - Padding.Right,
                        Inside.Y + Padding.Top + (Inside.Height - Padding.Vertical - size.Y) / 2,
                        size.X, size.Y);
                    Inside = Inside.Interior(0, 0, size.X + Padding.Right, 0);
                    break;
                case AutoLayout.DockBottom:
                    size = GetClampedChildSize(Child, new Point(Inside.Width - Padding.Horizontal, size.Y));
                    newPos = new Rectangle(Inside.X + Padding.Left, 
                        Inside.Bottom - size.Y - Padding.Bottom, size.X, size.Y);
                    Inside = Inside.Interior(0, 0, 0, size.Y + Padding.Bottom);
                    break;
                case AutoLayout.DockLeft:
                    size = GetClampedChildSize(Child, new Point(size.X, Inside.Height - Padding.Vertical));
                    newPos = new Rectangle(Inside.X + Padding.Left, Inside.Y + Padding.Top, size.X, size.Y);
                    Inside = Inside.Interior(size.X + Padding.Left, 0, 0, 0);
                    break;
                case AutoLayout.DockLeftCentered:
                    size = GetClampedChildSize(Child, new Point(size.X, Inside.Height - Padding.Vertical));
                    newPos = new Rectangle(Inside.X + Padding.Left, 
                        Inside.Y + Padding.Top + (Inside.Height - Padding.Vertical - size.Y) / 2,
                        size.X, size.Y);
                    Inside = Inside.Interior(size.X + Padding.Left, 0, 0, 0);
                    break;
                case AutoLayout.DockFill:
                    size = GetClampedChildSize(Child, new Point(
                        Inside.Width - Padding.Horizontal, Inside.Height - Padding.Vertical));
                    newPos = new Rectangle(     // Actually just centers widget in space
                        Inside.X + (Inside.Width / 2) - (size.X / 2),
                        Inside.Y + (Inside.Height / 2) - (size.Y / 2),
                        size.X, size.Y);
                    Inside = new Rectangle(0, 0, 0, 0);
                    break;
                case AutoLayout.FloatCenter:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(
                        Inside.X + (Inside.Width / 2) - (size.X / 2),
                        Inside.Y + (Inside.Height / 2) - (size.Y / 2),
                        size.X, size.Y);
                    break;
                case AutoLayout.FloatTop:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(Inside.X + (Inside.Width / 2) - (size.X / 2),
                        Inside.Y + Padding.Top, size.X, size.Y);
                    break;
                case AutoLayout.FloatBottom:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(Inside.X + (Inside.Width / 2) - (size.X / 2),
                        Inside.Bottom - size.Y - Padding.Bottom, size.X, size.Y);
                    break;
                case AutoLayout.FloatLeft:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(Inside.X + Padding.Left, Inside.Y + (Inside.Height / 2) - (size.Y / 2),
                        size.X, size.Y);
                    break;
                case AutoLayout.FloatRight:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(Inside.Right - size.X - Padding.Right,
                        Inside.Y + (Inside.Height / 2) - (size.Y / 2),
                        size.X, size.Y);
                    break;
                case AutoLayout.FloatTopRight:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(Inside.X + Inside.Width - size.X - Padding.Right, 
                        Inside.Y + Padding.Top, size.X, size.Y);
                    break;
                case AutoLayout.FloatTopLeft:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(Inside.X + Padding.Left, Inside.Y + Padding.Top, size.X, size.Y);
                    break;
                case AutoLayout.FloatBottomRight:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(
                        Inside.X + Inside.Width - size.X - Padding.Right,
                        Inside.Y + Inside.Height - size.Y - Padding.Bottom, 
                        size.X, size.Y);
                    break;
                case AutoLayout.FloatBottomLeft:
                    size = GetClampedChildSize(Child, size);
                    newPos = new Rectangle(Inside.X + Padding.Left, 
                        Inside.Y + Inside.Height - size.Y - Padding.Bottom, size.X, size.Y);
                    break;
                default:
                    newPos = new Rectangle(0, 0, 0, 0);
                    break;
            }

            Child.Rect = newPos;
            Child.Layout();

            return Inside;
        }
    }
}
