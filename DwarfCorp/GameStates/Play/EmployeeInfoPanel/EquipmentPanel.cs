using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Play.EmployeeInfo
{
    public class EquippedResourceIcon : Widget
    {
        private Resource _Resource = null;
        public Resource Resource
        {
            set
            {
                _Resource = value;
                Invalidate();
            }
        }

        public override void Construct()
        {
            base.Construct();
        }

        protected override Mesh Redraw()
        {
            if (_Resource == null)
                return base.Redraw();

            Tooltip = String.Format("{0}\nWear: {1:##.##}%", _Resource.DisplayName, (_Resource.Tool_Wear / _Resource.Tool_Durability) * 100.0f);
            var layers = _Resource.GuiLayers;

            var r = new List<Mesh>();
            foreach (var layer in layers)
                r.Add(Mesh.Quad()
                            .Scale(Rect.Width, Rect.Height)
                            .Translate(Rect.X, Rect.Y)
                            .Colorize(BackgroundColor)
                            .Texture(Root.GetTileSheet(layer.Sheet).TileMatrix(layer.Tile)));
            r.Add(base.Redraw());
            return Mesh.Merge(r.ToArray());
        }
    }

    public class EquipmentPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        private EquippedResourceIcon ToolIcon;

        public override void Construct()
        {
            Font = "font8";

            ToolIcon = AddChild(new EquippedResourceIcon {
                
            }) as EquippedResourceIcon;

            base.Construct();

            OnLayout += (sender) =>
            {
                ToolIcon.Rect = new Rectangle(this.Rect.X + 64, this.Rect.Y + 64, 32, 32);
            };
        }

        protected override Gui.Mesh Redraw()
        {
            if (Employee != null && !Employee.IsDead)
            {
                Hidden = false;
                Text = "";

                ToolIcon.Resource = null;

                if (Employee.Stats.Equipment == null)
                    Text = "This employee cannot use equipment.";
                else
                {
                    if (Employee.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool))
                        ToolIcon.Resource = tool;
                }
            }
            else
                Hidden = true;

            return base.Redraw();
        }
    }
}
