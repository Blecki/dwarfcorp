using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ComboBox : Widget
    {
        public List<String> Items = new List<String>();
        public int ItemsVisibleInPopup = 6;

        private int _selectedIndex = 1;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;

                if (_selectedIndex < 0 || _selectedIndex >= Items.Count)
                    Text = "";
                else
                    Text = Items[_selectedIndex];

                if (Root != null)
                    Root.SafeCall(OnSelectedIndexChanged, this);

                Invalidate();
            }
        }

        public Action<Widget> OnSelectedIndexChanged = null;

        public String SelectedItem
        {
            get
            {
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                    return Items[SelectedIndex];
                else return null;
            }
        }

        private Widget SelectorPopup = null;

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Graphics)) Graphics = "combo-down";

            OnClick += (sender, args) =>
                {
                    //var interior = GetDrawableInterior();
                    //var clickX = args.X - interior.X - interior.Width;
                    //if (clickX >= 0) // Clicked the button.
                    //{
                        if (SelectorPopup != null)
                            Root.DestroyWidget(SelectorPopup);
                        else
                        {
                            var childRect = new Rectangle(Rect.X, Rect.Y + Rect.Height, Rect.Width, Rect.Width);
                            var listView = Root.ConstructWidget(new ListView
                                {
                                    Rect = childRect,
                                    TextSize = TextSize,
                                    Border = "border-thin"
                                }) as ListView;

                            listView.Items.AddRange(Items);
                            listView.SelectedIndex = SelectedIndex;

                            // Find out how much 'border space' the listview is using.
                            var bufferHeight = childRect.Height - listView.GetDrawableInterior().Height;
                            listView.Rect.Height = bufferHeight + listView.ItemHeight * ItemsVisibleInPopup;
                            listView.Layout();
                            listView.OnUpdate = (widget, time) =>
                            {
                                if (IsAnyParentHidden() || IsAnyParentTransparent())
                                {
                                    widget.Close();
                                }
                            };
                        Root.RegisterForUpdate(listView);
                            listView.OnSelectedIndexChanged += (_sender) =>
                                {
                                    if (SelectorPopup != null)
                                    {
                                        var newSelection = listView.SelectedIndex;
                                        Root.DestroyWidget(SelectorPopup);
                                        SelectedIndex = newSelection;
                                        SelectorPopup = null;
                                    }
                                };

                        listView.OnClose = (s) => SelectorPopup = null;
                        
                            SelectorPopup = listView;
                            SelectorPopup.PopupDestructionType = PopupDestructionType.DestroyOnOffClick;
                            Root.ShowMinorPopup(SelectorPopup);
                        OnClose += (s) => { if (SelectorPopup != null) { SelectorPopup.Close(); } };
                        }
                    //}
                };

            var color = TextColor;
            OnMouseEnter += (widget, action) =>
            {
                widget.TextColor = new Vector4(0.5f, 0, 0, 1.0f);
                widget.Invalidate();
            };

            OnMouseLeave += (widget, action) =>
            {
                widget.TextColor = color;
                widget.Invalidate();
            };

            Border = "border-thin";

            OnClose += (sender) =>
            {
                if (SelectorPopup != null) SelectorPopup.Close();
            };
        }

        public override Point GetBestSize()
        {
            var baseSize = new Point(0, Root.GetTileSheet(Font).TileHeight * TextSize);
            
            var gfx = Root.GetTileSheet(Graphics);
            if (baseSize.X < gfx.TileWidth) baseSize.X = gfx.TileWidth;
            if (baseSize.Y < gfx.TileHeight) baseSize.Y = gfx.TileHeight;
            
            if (!String.IsNullOrEmpty(Border))
            {
                var border = Root.GetTileSheet(Border);
                baseSize.X += border.TileWidth * 2;
                baseSize.Y += border.TileHeight * 2;
            }           
            
            return baseSize;
        }

        public override Rectangle GetDrawableInterior()
        {
            // Ensure text doesn't draw over down arrow.
            var gfx = Root.GetTileSheet(Graphics);
            return base.GetDrawableInterior().Interior(0, 0, gfx.TileWidth, 0);
        }

        protected override Mesh Redraw()
        {
            var gfx = Root.GetTileSheet(Graphics);
            var interior = GetDrawableInterior();
            var downArrow = Mesh.Quad()
               .TileScaleAndTexture(gfx, 0)
               .Translate(interior.X + interior.Width, interior.Y);
            return Mesh.Merge(base.Redraw(), downArrow);
        }
    }
}
