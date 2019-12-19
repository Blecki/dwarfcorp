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
    public class EquipmentPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        private Dictionary<String, ResourceIcon> ResourceIcons = new Dictionary<string, ResourceIcon>();
        private ContentsPanel ContentsPanel = null;
        private EquipmentSlotType SelectedSlot = null;

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
                var slotIcon = AddChild(new ResourceIcon
                {
                    OnLayout = (_) => _.Rect = new Rectangle(background.Rect.X + slot.GuiOffset.X * scale, background.Rect.Y + slot.GuiOffset.Y * scale, 16 * scale, 16 * scale),
                    EnableDragAndDrop = false,
                    Tag = slot,
                    OnClick = (sender, args) => SelectedSlot = sender.Tag as EquipmentSlotType
                }) as ResourceIcon;

                ResourceIcons.Add(slot.Name, slotIcon);
            }

            ContentsPanel = AddChild(new ContentsPanel
            {
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(256, 0),
                EnableDragAndDrop = false,
                Resources = new ResourceSet()
                
            }) as ContentsPanel;

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

                    ContentsPanel.Resources.Clear();

                    if (Employee.GetRoot().GetComponent<Inventory>().HasValue(out var inventory))
                    {
                        ContentsPanel.Hidden = false;

                        if (SelectedSlot != null)
                        {
                            foreach (var res in Employee.World.EnumerateResourcesInStockpiles().Where(r => r.Equipment_Slot == SelectedSlot.Name))
                                ContentsPanel.Resources.Add(res);
                            foreach (var res in inventory.Resources.Where(r => r.Resource.Equipment_Slot == SelectedSlot.Name))
                                ContentsPanel.Resources.Add(res.Resource);
                        }

                        ContentsPanel.Invalidate();
                    }
                    else
                        ContentsPanel.Hidden = true;
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
