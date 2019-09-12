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
    public class BetterResourceIcon : Widget
    {
        private String _ResourceType = "";
        public String ResourceType
        {
            set
            {
                _ResourceType = value;
                Invalidate();
            }
        }

        public override void Construct()
        {
            base.Construct();
        }

        protected override Mesh Redraw()
        {
            if (String.IsNullOrEmpty(_ResourceType))
                return base.Redraw();

            if (Library.GetResourceType(_ResourceType).HasValue(out var res))
            {
                Tooltip = res.Name;

                var r = new List<Mesh>();
                foreach (var layer in res.GuiLayers)
                    r.Add(Mesh.Quad()
                                .Scale(Rect.Width, Rect.Height)
                                .Translate(Rect.X, Rect.Y)
                                .Colorize(BackgroundColor)
                                .Texture(Root.GetTileSheet(layer.Sheet).TileMatrix(layer.Tile)));
                r.Add(base.Redraw());
                return Mesh.Merge(r.ToArray());
            }
            else
                return base.Redraw();
        }
    }

    public class EquipmentPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        private BetterResourceIcon ToolIcon;

        public override void Construct()
        {
            Font = "font8";

            ToolIcon = AddChild(new BetterResourceIcon {
                
            }) as BetterResourceIcon;

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

                ToolIcon.ResourceType = null;

                if (Employee.Stats.Equipment == null)
                    Text = "This employee cannot use equipment.";
                else
                {
                    if (Employee.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool))
                        ToolIcon.ResourceType = tool.Resource;
                   
                }

            }
            else
                Hidden = true;

            return base.Redraw();
        }
    }
}
