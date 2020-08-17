using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class CheckBox : Widget
    {
        public bool ToggleOnTextClick = true;

        private bool _enabled = true;
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value;  Invalidate(); }
        }

        private bool _checkState = false;
        public bool CheckState 
        {
            get { return _checkState; }
            set {
                _checkState = value;

                if (Root != null) // If root is null, this must be being set as part of construction and events should not fire.
                {
                    Root.SafeCall(OnCheckStateChange, this);
                    Invalidate();
                }
            }
        }

        public void SilentSetCheckState(bool NewState)
        {
            _checkState = NewState;
            this.Invalidate();
        }

        public Action<Widget> OnCheckStateChange = null;

        public override void Construct()
        {
            OnClick += (sender, args) => { if (Enabled)
                {
                    if (ToggleOnTextClick)
                        CheckState = !CheckState;
                    else
                    {
                        var drawArea = GetDrawableInterior();
                        if (args.X < drawArea.Left + drawArea.Height)
                            CheckState = !CheckState;
                    }
                }
            };

            TextVerticalAlign = VerticalAlign.Center;
            ChangeColorOnHover = true;
            HoverTextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();

            if (String.IsNullOrEmpty(Graphics))
                Graphics = "checkbox";

            base.Construct();
        }

        public override Rectangle GetDrawableInterior()
        {
            var baseDrawArea = base.GetDrawableInterior();
            return baseDrawArea.Interior(baseDrawArea.Height + 2, 0, 0, 0);
        }

        protected override Mesh Redraw()
        {
            var baseMesh = base.Redraw();
            var baseDrawArea = base.GetDrawableInterior();

            var checkMesh = baseMesh.QuadPart()
                .Scale(16, 16)
                .Translate(baseDrawArea.X + InteriorMargin.Left, baseDrawArea.Y + InteriorMargin.Top + (baseDrawArea.Height / 2 - 8))
                .Texture(Root.GetTileSheet(Graphics).TileMatrix(CheckState ? 1 : 0));

            if (!Enabled)
                checkMesh.MorphEx(v =>
                {
                    v.Color = new Vector4(TextColor.X, TextColor.Y, TextColor.Z, 0.3f);
                    return v;
                });

            return baseMesh;
        }

        public override Point GetBestSize()
        {
            var size = new Point(0, 0);
            if (!String.IsNullOrEmpty(Text))
            {
                var font = Root.GetTileSheet(Font);
                size = font.MeasureString(Text).Scale(TextSize);
            }

            var gfx = Root.GetTileSheet(Graphics);
            return new Point(gfx.TileWidth + size.X + 2, Math.Max(gfx.TileHeight, size.Y));
        }
    }
}
