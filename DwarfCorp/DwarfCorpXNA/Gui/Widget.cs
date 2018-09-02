using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Gui
{
    /// <summary>
    /// An individual element in the GUI.
    /// </summary>
    public partial class Widget
    {
        #region Position and Layout

        public Rectangle Rect = new Rectangle(0, 0, 0, 0);

        public Point MinimumSize = new Point(0, 0);
        public Point MaximumSize = new Point(int.MaxValue, int.MaxValue);
        public AutoLayout AutoLayout = AutoLayout.None;

        /// <summary>
        /// Space to leave between children when laying them out on the widget.
        /// </summary>
        public Margin Padding = Margin.Zero;

        /// <summary>
        /// Extra space to leave on the edges of the widget when arranging children.
        /// </summary>
        public Margin InteriorMargin = Margin.Zero;
        // Todo: Exterior margin

        // Todo: Hover styling.

        #endregion

        #region Appearance
        /// <summary>
        /// If transparent, this widget is not drawn. Children may still be drawn.
        /// </summary>
        public bool Transparent = false;

        // If Hidden, widget is not drawn and does not interact.
        public bool Hidden = false;

        public Vector4 BackgroundColor = Vector4.One;

        /// <summary>
        /// Tile to use as a background for this widget. If null, not background is drawn.
        /// </summary>
        public TileReference Background = null;
        public String Graphics = null;

        /// <summary>
        /// Tilesheet to use as a border for this widget. If null, no border is drawn.
        /// </summary>
        public String Border = null;

        public String _text = null;

        /// <summary>
        /// Text to draw on this widget.
        /// </summary>
        public String Text
        {
            get { return _text; }
            set { _text = value; Invalidate(); }
        }

        public HorizontalAlign TextHorizontalAlign = HorizontalAlign.Left;
        public VerticalAlign TextVerticalAlign = VerticalAlign.Top;

        private String _Font = null;
        public String Font
        {
            get
            {
                if (!String.IsNullOrEmpty(_Font)) return _Font;
                else if (Parent != null) return Parent.Font;
                else return "font8";
            }
            set { _Font = value; }
        }

        private Vector4? _textColor = null;
        public Vector4 TextColor
        {
            get
            {
                if (_textColor.HasValue) return _textColor.Value;
                else if (Parent != null) return Parent.TextColor;
                else return new Vector4(0, 0, 0, 1);
            }
            set { _textColor = value; }
        }

        public int TextSize = 1; 

        public String _tooltip = null;
        public String Tooltip
        {
            get
            {
                if (_tooltip != null) return _tooltip;
                else if (Parent != null) return Parent.Tooltip;
                else return null;
            }
            set
            {
                _tooltip = value;
            }
        }

        public Vector4 HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
        public bool ChangeColorOnHover = false;
        public bool WrapText = true;
        public bool AutoResizeToTextHeight = false;

        private class HoverClickHelper
        {
            public InputEventArgs EventArgs = null;
            public Widget Widget = null;
            private double CurrentTime = 0.0;
            private double TimeOnClick = 0.0;
            private double MaxTickFrequency = 0.1f;
            private double MinTickFrequency = 1.0f;
            private double TickFrequency = 1.0f;

            public void OnMouseDown(Widget widget, InputEventArgs args)
            {
                TickFrequency = MinTickFrequency;
                TimeOnClick = CurrentTime;
                EventArgs = args;
                Widget = widget;
            }

            public void OnMouseUp(Widget widget, InputEventArgs args)
            {
                TickFrequency = MinTickFrequency;
                TimeOnClick = 0.0;
                Widget = null;
                EventArgs = null;
            }

            public void OnMouseLeave(Widget widget, InputEventArgs args)
            {
                TickFrequency = MinTickFrequency;
                TimeOnClick = 0.0;
                Widget = null;
                EventArgs = null;
            }

            public void Update(Widget widget, GameTime time)
            {
                CurrentTime = time.TotalGameTime.TotalSeconds;

                if (Widget != null && (CurrentTime - TimeOnClick) > TickFrequency)
                {
                    if (Widget.OnClick != null)
                    {
                        Widget.OnClick.Invoke(Widget, EventArgs);
                        TickFrequency = Math.Max(TickFrequency * 0.5f, MaxTickFrequency);
                        TimeOnClick = CurrentTime;
                    }
                }
            }
        }

        #endregion

        #region Events

        public Action<Widget, InputEventArgs> OnMouseEnter = null;
        public Action<Widget, InputEventArgs> OnMouseLeave = null;
        public Action<Widget, InputEventArgs> OnMouseMove = null;
        public Action<Widget> OnHover = null;
        public Action<Widget, InputEventArgs> OnClick = null;
        public Action<Widget, InputEventArgs> OnMouseDown = null;
        public Action<Widget, InputEventArgs> OnMouseUp = null;
        public Action<Widget, InputEventArgs> OnScroll = null;
        public Action<Widget> OnGainFocus = null;
        public Action<Widget> OnLoseFocus = null;
        public Action<Widget, InputEventArgs> OnKeyPress = null;
        public Action<Widget, InputEventArgs> OnKeyDown = null;
        public Action<Widget, InputEventArgs> OnKeyUp = null;
        public Action<Widget> OnUpdateWhileFocus = null;
        public Action<Widget, GameTime> OnUpdate = null;
        public Action<Widget> OnConstruct = null;
        public Action<Widget> OnLayout = null;
        public Action<Widget> OnClose = null;
        public Action<Widget> OnShown = null;
        public bool TriggerOnChildClick = false;
        private HoverClickHelper HoverClick = null;

        public void EnableHoverClick()
        {
            HoverClick = new HoverClickHelper();
            OnMouseDown = HoverClick.OnMouseDown;
            OnMouseUp = HoverClick.OnMouseUp;
            OnMouseLeave = HoverClick.OnMouseLeave;
            OnUpdate = HoverClick.Update;
            Root.RegisterForUpdate(this);
        }
        #endregion

        public Object Tag = null;
        public PopupDestructionType PopupDestructionType = PopupDestructionType.Keep;

        private Mesh CachedRenderMesh = null;
        internal List<Widget> Children = new List<Widget>();
        public Widget Parent { get; private set; }
        public Root Root { get; internal set; }

        public bool IsFloater = false;

        internal bool Constructed = false;

        public Widget()
        {

        }

        internal void _Construct(Root Root)
        {
            this.Root = Root;
            if (!Constructed)
            {
                if (Text != null)
                    Text = StringLibrary.TransformDataString(Text, Text);

                if (Tooltip != null)
                    Tooltip = StringLibrary.TransformDataString(Tooltip, Tooltip);
                
                Constructed = true;
                this.Construct();
                Root.SafeCall(OnConstruct, this);
            }
        }

        public virtual void Construct()
        {
            if (ChangeColorOnHover)
            {
                OnMouseEnter += (sender, args) =>
                {
                    var t = TextColor;
                    TextColor = HoverTextColor;
                    HoverTextColor = t;
                    Invalidate();
                };

                OnMouseLeave += (sender, args) =>
                {
                    var t = TextColor;
                    TextColor = HoverTextColor;
                    HoverTextColor = t;
                    Invalidate();
                };
            }
        }

        public Widget FindWidgetAt(int x, int y)
        {
            foreach (var child in Children.Reverse<Widget>().Where(c => !c.Hidden && !c.IsFloater))
            {
                var item = child.FindWidgetAt(x, y);
                if (item != null) return item;
            }

            if (!Transparent && Rect.Contains(x, y)) return this;
            return null;
        }

        public void Invalidate()
        {
            CachedRenderMesh = null;
            if (Parent != null) Parent.Invalidate();
        }

        public Widget AddChild(Widget child)
        {
            if (!Constructed)
                throw new InvalidOperationException("Widget must be constructed before children can be added.");

            if (!child.Constructed)
                child._Construct(Root);

            if (!Object.ReferenceEquals(child.Root, Root))
                throw new InvalidOperationException("Can't add UIItem to different heirarchy");

            Children.Add(child);
            child.Parent = this;
            Invalidate();

            return child;
        }

        public void SendToBack(Widget Child)
        {
            if (!Object.ReferenceEquals(Child.Parent, this)) throw new InvalidOperationException();
            Children.Remove(Child);
            Children.Insert(0, Child);
            Invalidate();
        }

        public void BringToFront()
        {
            if (Parent != null)
            {
                Parent.Children.Remove(this);
                Parent.Children.Add(this);
                Parent.BringToFront();
            }
        }

        public void RemoveChild(Widget child)
        {
            Children.Remove(child);
            child.Parent = null;
            Invalidate();
        }

        public Widget GetChild(int ID)
        {
            return Children[ID];
        }

        public IEnumerable<Widget> EnumerateChildren()
        {
            foreach (var child in Children) yield return child;
        }

        public IEnumerable<Widget> EnumerateTree()
        {
            yield return this;
            foreach (var child in Children)
                foreach (var grandchild in child.EnumerateTree())
                    yield return grandchild;
        }

        public void Clear()
        {
            foreach (var child in Children)
            {
                Root.CleanupWidget(child);
                child.Parent = null;
            }

            Children.Clear();
            Invalidate();
        }

        /// <summary>
        /// Check to see if the widget is a child, grandchild, etc of another widget.
        /// </summary>
        /// <param name="Ancestor"></param>
        /// <returns>True if ancestor appears in the parent chain of this widget</returns>
        public bool IsChildOf(Widget Ancestor)
        {
            if (Object.ReferenceEquals(Ancestor, Parent)) return true;
            if (Parent != null) return Parent.IsChildOf(Ancestor);
            return false;
        }

        public void Close()
        {
            if (Root != null)
                Root.DestroyWidget(this);
        }

        /// <summary>
        /// Returns the space inside the widget where it is safe to place child widgets, draw text, etc.
        /// </summary>
        /// <returns></returns>
        public virtual Rectangle GetDrawableInterior()
        {
            if (!String.IsNullOrEmpty(Border))
            {
                var tileSheet = Root.GetTileSheet(Border);
                return Rect.Interior(tileSheet.TileWidth, tileSheet.TileHeight,
                    tileSheet.TileWidth, tileSheet.TileHeight);
            }
            else
                return Rect;
        }
        
        /// <summary>
        /// Get the best size for this widget. Does not respect minimum or maximum sizes, those values are
        /// enforced by the layout engine.
        /// </summary>
        /// <returns></returns>
        public virtual Point GetBestSize()
        {
            var size = new Point(0, 0);

            if (!String.IsNullOrEmpty(Text))
            {
                var font = Root.GetTileSheet(Font);
                size = font.MeasureString(Text).Scale(TextSize);
            }

            if (!String.IsNullOrEmpty(Border))
            {
                var border = Root.GetTileSheet(Border);
                size = new Point(size.X + border.TileWidth + border.TileWidth,
                    size.Y + border.TileHeight + border.TileHeight);
            }

            return size;
        }

        /// <summary>
        /// Create the rendering mesh for this widget.
        /// </summary>
        /// <returns></returns>
        protected virtual Mesh Redraw()
        {
            if (Hidden) throw new InvalidOperationException();

            if (Transparent || Root== null)
                return Mesh.EmptyMesh();

            var result = new List<Mesh>();

            if (Background != null)
                result.Add(Mesh.Quad()
                    .Scale(Rect.Width, Rect.Height)
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(BackgroundColor)
                    .Texture(Root.GetTileSheet(Background.Sheet).TileMatrix(Background.Tile)));

            if (!String.IsNullOrEmpty(Border))
            {
                //Create a 'scale 9' background 
                result.Add(
                    Mesh.CreateScale9Background(Rect, Root.GetTileSheet(Border))
                    .Colorize(BackgroundColor));
            }

            // Add text label
            if (!String.IsNullOrEmpty(Text))
            {
                GetTextMesh(result);
            }

            return Mesh.Merge(result.ToArray());
        }

        public virtual void PostDraw(GraphicsDevice device)
        {

        }

        public bool IsAnyParentHidden()
        {
            if (Hidden)
            {
                return true;
            }

            if (Parent == null)
            {
                return false;
            }

            if (Parent == Root.RootItem && !Hidden)
            {
                return false;
            }
            return Parent.IsAnyParentHidden();
        }

        public bool IsAnyParentTransparent()
        {
            if (Transparent)
            {
                return true;
            }

            if (Parent == null)
            {
                return false;
            }

            if (Parent == Root.RootItem && !Transparent)
            {
                return false;
            }
            return Parent.IsAnyParentTransparent();
        }

        public void GetTextMesh(List<Mesh> result, String Text, Vector4 TextColor)
        {
            var drawableArea = GetDrawableInterior();
            var stringMeshSize = new Rectangle();
            var font = Root.GetTileSheet(Font);
            var text = (WrapText)
                ? font.WordWrapString(Text, TextSize, drawableArea.Width)
                : Text;
            var stringMesh = Mesh.CreateStringMesh(
                text,
                font,
                new Vector2(TextSize, TextSize),
                out stringMeshSize)
                .Colorize(TextColor);

            if (AutoResizeToTextHeight && stringMeshSize.Height < Rect.Height)
            {
                if (!String.IsNullOrEmpty(Border))
                {
                    var tileSheet = Root.GetTileSheet(Border);
                    Rect = new Rectangle(Rect.X, Rect.Y, Rect.Width, stringMeshSize.Height + tileSheet.TileHeight * 2);
                }
                else
                {
                    Rect = new Rectangle(Rect.X, Rect.Y, Rect.Width, stringMeshSize.Height);
                }
                MinimumSize.Y = stringMeshSize.Height;
                Parent.Layout();
            }

            var textDrawPos = Vector2.Zero;

            switch (TextHorizontalAlign)
            {
                case HorizontalAlign.Left:
                    textDrawPos.X = drawableArea.X;
                    break;
                case HorizontalAlign.Right:
                    textDrawPos.X = drawableArea.X + drawableArea.Width - stringMeshSize.Width;
                    break;
                case HorizontalAlign.Center:
                    textDrawPos.X = drawableArea.X + ((drawableArea.Width - stringMeshSize.Width) / 2);
                    break;
            }

            switch (TextVerticalAlign)
            {
                case VerticalAlign.Top:
                    textDrawPos.Y = drawableArea.Y;
                    break;
                case VerticalAlign.Bottom:
                    textDrawPos.Y = drawableArea.Y + drawableArea.Height - stringMeshSize.Height;
                    break;
                case VerticalAlign.Below:
                    textDrawPos.Y = drawableArea.Y + drawableArea.Height;
                    break;
                case VerticalAlign.Center:
                    textDrawPos.Y = drawableArea.Y + ((drawableArea.Height - stringMeshSize.Height) / 2);
                    break;
            }

            stringMesh.Translate(textDrawPos.X, textDrawPos.Y);
            result.Add(stringMesh);
        }

        public void GetTextMesh(List<Mesh> result)
        {
            GetTextMesh(result, Text, TextColor);
        }

        /// <summary>
        /// Get the render mesh to draw this widget.
        /// </summary>
        /// <returns></returns>
        public Mesh GetRenderMesh()
        {
            if (Hidden) return Mesh.EmptyMesh();

            if (CachedRenderMesh == null)
            {
                var r = new Mesh[1 + Children.Count];
                r[0] = Redraw();
                for (var i = 0; i < Children.Count; ++i)
                    r[i + 1] = Children[i].GetRenderMesh();
                CachedRenderMesh = Mesh.Merge(r);
            }

            return CachedRenderMesh;
        }

        public Rectangle ComputeBoundingChildRect()
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var child in EnumerateTree().Where(child => !child.Hidden))
            {
                minX = Math.Min(minX, child.Rect.Left);
                maxX = Math.Max(maxX, child.Rect.Right);
                minY = Math.Min(minY, child.Rect.Top);
                maxY = Math.Max(maxY, child.Rect.Bottom);
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
