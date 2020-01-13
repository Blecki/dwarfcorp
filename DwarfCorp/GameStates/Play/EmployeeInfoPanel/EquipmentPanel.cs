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
        private ResourceIcon SelectedSlotIcon = null;
        private ResourceIcon SelectedEquipIcon = null;
        private Widget RemoveButton = null;
        private Widget EquipButton = null;

        public override void Construct()
        {
            Font = "font8";

            ContentsPanel = AddChild(new ContentsPanel
            {
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(200, 0),
                Border = "border-one",
                EnableDragAndDrop = false,
                Resources = new ResourceSet(),
                OnIconClicked = (sender, args) =>
                {
                    SelectedEquipIcon.Resource = (sender as ResourceIcon).Resource;
                    EquipButton.Hidden = false;
                    EquipButton.Invalidate();
                }

            }) as ContentsPanel;

            var background = AddChild(new Widget
            {
                Background = new TileReference("equipment", 0),
                MinimumSize = new Point(128, 128),
                MaximumSize = new Point(128, 128),
                AutoLayout = AutoLayout.DockTop
            });

            var comparisonPanel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Border = "border-one"
            });

            var bottomBar = comparisonPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });

            RemoveButton = bottomBar.AddChild(new Widget
            {
                Text = "REMOVE",
                OnLayout = (_) => _.Rect = new Rectangle(bottomBar.Rect.X, bottomBar.Rect.Y, bottomBar.Rect.Width / 2, bottomBar.Rect.Height),
                TextVerticalAlign = VerticalAlign.Center,
                ChangeColorOnHover = true,
                OnClick = (_sender, args) =>
                {
                    if (SelectedSlotIcon.Resource != null)
                        Employee.AssignTask(new UnequipTask(SelectedSlotIcon.Resource));
                },
                Hidden = true
            });

            EquipButton = bottomBar.AddChild(new Widget
            {
                Text = "EQUIP",
                OnLayout = (_) => _.Rect = new Rectangle(bottomBar.Rect.X + bottomBar.Rect.Width / 2, bottomBar.Rect.Y, bottomBar.Rect.Width / 2, bottomBar.Rect.Height),
                TextVerticalAlign = VerticalAlign.Center,
                ChangeColorOnHover = true,
                OnClick = (_sender, args) =>
                {
                    if (SelectedEquipIcon.Resource != null)
                        Employee.AssignTask(new FindAndEquipTask(SelectedEquipIcon.Resource.DisplayName)); // Todo: Since we had to enumerate to build the list - couldn't we save the location?
                },
                Hidden = true
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
                    OnClick = (sender, args) =>
                    {
                        SelectedSlot = sender.Tag as EquipmentSlotType;
                        SelectedEquipIcon.Resource = null;
                        EquipButton.Hidden = true;
                    },
                    OverrideTooltip = true
                }) as ResourceIcon;

                ResourceIcons.Add(slot.Name, slotIcon);
            }

            SelectedSlotIcon = comparisonPanel.AddChild(new ResourceIcon
            {
                AutoLayout = AutoLayout.FloatTopLeft
            }) as ResourceIcon;

            SelectedEquipIcon = comparisonPanel.AddChild(new ResourceIcon
            {
                AutoLayout = AutoLayout.FloatTopRight,
                MinimumSize = new Point(32, 32)
            }) as ResourceIcon;


            base.Construct();
        }
        
        protected override Gui.Mesh Redraw()
        {
            if (Employee != null && !Employee.IsDead)
            {
                Employee.World.Tutorial("equipment");

                Hidden = false;
                Text = "";

                foreach (var icon in ResourceIcons)
                {
                    icon.Value.Hilite = null;
                    icon.Value.Resource = null;
                }
                   
                if (Employee.Creature.Equipment.HasValue(out var equipment))
                {
                    SelectedSlotIcon.Resource = null;
                    RemoveButton.Hidden = true;

                    foreach (var slot in ResourceIcons)
                        if (equipment.GetItemInSlot(slot.Key).HasValue(out var tool))
                        {
                            slot.Value.Resource = tool;
                            if (SelectedSlot != null && SelectedSlot.Name == slot.Key)
                            {
                                slot.Value.Hilite = "selected-slot";
                                SelectedSlotIcon.Resource = tool;
                                RemoveButton.Hidden = false;
                                RemoveButton.Invalidate();
                            }
                        }                  

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
