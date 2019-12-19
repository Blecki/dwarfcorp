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
                    EnableDragAndDrop = true,
                    CreateDraggableItem = (sender) =>
                    {
                        if (Employee == null)
                            return null;

                        var resource = (sender as ResourceIcon).Resource;
                        if (resource == null)
                            return null;

                        // Remove from equipment.
                        if (Employee.Creature.Equipment.HasValue(out var equipment))
                            equipment.UnequipItem(resource);

                        return new DraggedResourceIcon
                        {
                            Resource = resource,
                            MinimumSize = new Point(32, 32),
                            CanDropHere = CanDropHere,
                            OnDropCancelled = (dragItem) => 
                            {
                                if (Employee.Creature.Equipment.HasValue(out var _equipment))
                                    _equipment.EquipItem((dragItem as DraggedResourceIcon).Resource);
                            },
                            OnDropped = OnDropped
                        };
                    },                    
                    Tag = slot
                }) as ResourceIcon;

                ResourceIcons.Add(slot.Name, slotIcon);
            }

            ContentsPanel = AddChild(new ContentsPanel
            {
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(256, 0),
                EnableDragAndDrop = true,
                CreateDraggableItem = (sender) =>
                {
                    if (Employee == null)
                        return null;

                    var resource = (sender as ResourceIcon).Resource;
                    if (resource == null)
                        return null;

                    // Remove from inventory.
                    Employee.Creature.Inventory.Remove(resource, Inventory.RestockType.None);

                    return new DraggedResourceIcon
                    {
                        Resource = resource,
                        MinimumSize = new Point(32, 32),
                        CanDropHere = CanDropHere,
                        OnDropCancelled = (dragItem) => Employee.Creature.Inventory.AddResource(resource, Inventory.RestockType.None),
                        OnDropped = OnDropped
                    };
                }
            }) as ContentsPanel;

            base.Construct();
        }

        private bool CanDropHere(Widget Dragged, Widget Target)
        {
            if (Target is ContentsPanel)
                return true;
            else if (Target is ResourceIcon icon && icon.Tag is EquipmentSlotType slot)
            {
                if (Dragged is DraggedResourceIcon res && res.Resource.Equipment_Slot == slot.Name)
                    return true;
            }

            return false;
        }

        private void OnDropped(Widget Dragged, Widget Target)
        {
            if (Target is ContentsPanel contents)
                contents.Resources.Add((Dragged as DraggedResourceIcon).Resource);
            else if (Target is ResourceIcon icon && icon.Tag is EquipmentSlotType slot)
            {
                var resource = (Dragged as DraggedResourceIcon).Resource;
                if (resource.Equipment_Slot != slot.Name)
                    throw new InvalidProgramException();

                if (Employee.Creature.Equipment.HasValue(out var equipment))
                {
                    if (equipment.GetItemInSlot(slot.Name).HasValue(out var existing))
                        Employee.Creature.Inventory.AddResource(existing, Inventory.RestockType.None);

                    equipment.EquipItem(resource);
                }
            }
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
