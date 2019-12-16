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

        private Dictionary<String, EquippedResourceIcon> ResourceIcons = new Dictionary<string, EquippedResourceIcon>();
        private StockpileContentsPanel ContentsPanel = null;

        public override void Construct()
        {
            Font = "font8";

            var background = AddChild(new Widget
            {
                Background = new TileReference("equipment", 0),
                MinimumSize = new Point(128, 128),
                AutoLayout = AutoLayout.FloatLeft
            });

            var bgTile = Root.GetTileSheet("equipment");
            var scale = 128 / bgTile.TileWidth;

            foreach (var slot in Library.EnumerateEquipmentSlotTypes())
            {
                var slotIcon = AddChild(new EquippedResourceIcon
                {
                    OnLayout = (_) => _.Rect = new Rectangle(background.Rect.X + slot.GuiOffset.X * scale, background.Rect.Y + slot.GuiOffset.Y * scale, 16 * scale, 16 * scale)
                }) as EquippedResourceIcon;

                ResourceIcons.Add(slot.Name, slotIcon);
            }

            ContentsPanel = AddChild(new StockpileContentsPanel
            {
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(256, 0)
            }) as StockpileContentsPanel;

            base.Construct();
        }

        protected override Gui.Mesh Redraw()
        {
            // Todo: Generic placement of equipment icons
            if (Employee != null && !Employee.IsDead)
            {
                Hidden = false;
                Text = "";

                foreach (var icon in ResourceIcons)
                    icon.Value.Resource = null;
                   
                if (Employee.Creature.Equipment.HasValue(out var equipment))
                {
                    foreach (var slot in ResourceIcons)
                        if (equipment.GetItemInSlot(slot.Key).HasValue(out var tool))
                            slot.Value.Resource = tool;

                    if (Employee.GetRoot().GetComponent<Inventory>().HasValue(out var inventory))
                    {
                        ContentsPanel.Hidden = false;
                        ContentsPanel.Resources = inventory.ContentsAsResourceSet();
                        ContentsPanel.Invalidate();
                    }
                    else
                    {
                        ContentsPanel.Hidden = true;
                        ContentsPanel.Resources = null;
                    }
                }
                else
                    Text = "This employee cannot use equipment.";
            }
            else
                Hidden = true;

            return base.Redraw();
        }
    }
}
