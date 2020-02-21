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

            var currentEquipPanel = AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(128, 0)
            });

            ContentsPanel = AddChild(new ContentsPanel
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(128, 0),
                Border = "border-one",
                EnableDragAndDrop = false,
                Resources = new ResourceSet(),
                OnIconClicked = (sender, args) =>
                {
                    foreach (var icon in ContentsPanel.Children.OfType<ResourceIcon>())
                    {
                        icon.Hilite = null;
                        icon.Invalidate();
                    }
                    SelectedEquipIcon.Resource = (sender as ResourceIcon).Resource;
                    (sender as ResourceIcon).Hilite = new TileReference("equipment_sheet", 2);
                    sender.Invalidate();
                    EquipButton.Hidden = false;
                    EquipButton.Invalidate();
                }

            }) as ContentsPanel;

            var equipmentSlotPanel = currentEquipPanel.AddChild(new Widget
            {
                MinimumSize = new Point(104, 104),
                MaximumSize = new Point(104, 104),
                AutoLayout = AutoLayout.DockTopCentered
            });

            equipmentSlotPanel.AddChild(new Widget
            {
                Background = new TileReference("equipment_sheet", 0),
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = AutoLayout.FloatCenter
            });

            var comparisonPanel = currentEquipPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Border = "border-thin"
            });

            var bottomBar = comparisonPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });

            RemoveButton = bottomBar.AddChild(new Widget
            {
                Text = "remove",
                TextVerticalAlign = VerticalAlign.Center,
                MinimumSize = new Point(64, 32),
                MaximumSize = new Point(64, 32),
                AutoLayout = AutoLayout.DockLeft,
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
                Text = "equip",
                TextVerticalAlign = VerticalAlign.Center,
                MinimumSize = new Point(64, 32),
                MaximumSize = new Point(64, 32),
                AutoLayout = AutoLayout.DockRight,
                ChangeColorOnHover = true,
                OnClick = (_sender, args) =>
                {
                    if (SelectedEquipIcon.Resource != null)
                        Employee.AssignTask(new FindAndEquipTask(SelectedEquipIcon.Resource.DisplayName)); // Todo: Since we had to enumerate to build the list - couldn't we save the location?
                },
                Hidden = true
            });

            foreach (var slot in Library.EnumerateEquipmentSlotTypes())
            {
                var slotIcon = AddChild(new ResourceIcon
                {
                    OnLayout = (_) => _.Rect = new Rectangle(equipmentSlotPanel.Rect.X + slot.GuiOffset.X, equipmentSlotPanel.Rect.Y + slot.GuiOffset.Y, 32, 32),
                    EnableDragAndDrop = false,
                    Tag = slot,
                    OnClick = (sender, args) =>
                    {
                        SelectedSlot = sender.Tag as EquipmentSlotType;
                        SelectedEquipIcon.Resource = null;
                        EquipButton.Hidden = true;
                        EquipButton.Invalidate();
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

                if (Employee.Creature.Equipment.HasValue(out var equipment))
                {
                    if (SelectedSlot != null && equipment.GetItemInSlot(SelectedSlot.Name).HasValue(out var selectedTool))
                    {
                        SelectedSlotIcon.Resource = selectedTool;
                        RemoveButton.Hidden = false;
                    }
                    else
                    {
                        SelectedSlotIcon.Resource = null;
                        RemoveButton.Hidden = true;
                    }
                    RemoveButton.Invalidate();

                    foreach (var slot in ResourceIcons)
                    {
                        var tool = equipment.GetItemInSlot(slot.Key);
                        var slotType = Library.FindEquipmentSlotType(slot.Key);

                        // Set the icon background
                        if (tool.HasValue(out var t))
                        {
                            slot.Value.Resource = t;

                            if (SelectedSlot != null && SelectedSlot.Name == slot.Key)
                                slot.Value.Hilite = new TileReference("equipment_sheet", 2);
                            else
                                slot.Value.Hilite = new TileReference("equipment_sheet", 1);
                        }
                        else if (slotType.HasValue(out var st))
                        {
                            slot.Value.Resource = null;

                            if (SelectedSlot != null && SelectedSlot.Name == slot.Key)
                                slot.Value.Hilite = st.SelectedBackground;
                            else
                                slot.Value.Hilite = st.UnselectedBackground;
                        }
                        else
                        {
                            slot.Value.Resource = null;

                            if (SelectedSlot != null && SelectedSlot.Name == slot.Key)
                                slot.Value.Hilite = new TileReference("equipment_sheet", 2);
                            else
                                slot.Value.Hilite = new TileReference("equipment_sheet", 1);
                        }

                        slot.Value.Invalidate();
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
