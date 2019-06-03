using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class PriorityCommand : ContextCommand
    {
        [ContextCommand]
        public static ContextCommand __factory() { return new PriorityCommand(); }

        public PriorityCommand()
        {
            Name = "Priority";
            Description = "Click to set the priority of the selected command(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(GameComponent Entity, WorldManager World)
        {
            return World.PlayerFaction.Designations.EnumerateEntityDesignations(Entity).Any();
        }

        public override void Apply(GameComponent Entity, WorldManager World)
        {
            foreach (var des in World.PlayerFaction.Designations.EnumerateEntityDesignations(Entity).ToList())
                if (des.Task != null)
                {
                    CreateTaskPrioritySelector(World, des.Task, des);
                }
        }

        public class PrioritySelector : Widget
        {
            public Task Task;
            public Vector3 ScreenPos;
            public PrioritySelector()
            {

            }

            public override void Construct()
            {
                Rect = new Rectangle((int)ScreenPos.X, (int)ScreenPos.Y, 150, 100);
                Border = "border-one";
                AddChild(new Widget()
                {
                    Font = "font8",
                    Text = Task.Name,
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(100, 25),
                });

                var combobox = AddChild(new ComboBox()
                {
                    MinimumSize = new Point(90, 20),
                    Items = Enum.GetValues(typeof(Task.PriorityType)).Cast<Task.PriorityType>().Select(s => s.ToString()).ToList(),
                    OnSelectedIndexChanged = (sender) =>
                    {
                        var type = (Task.PriorityType)((sender as ComboBox).SelectedIndex);
                        if (type != Task.Priority)
                        {
                            Task.Priority = (Task.PriorityType)((sender as ComboBox).SelectedIndex);
                            sender.Parent.Close();
                        }
                    },
                    SelectedIndex = (int)(Task.Priority),
                    AutoLayout = AutoLayout.DockTop
                }) as ComboBox;

                AddChild(new Button()
                {
                    Text = "Close",
                    MaximumSize = new Point(100, 30),
                    AutoLayout = AutoLayout.DockTop,
                    OnClick = (sender, args) =>
                    {
                        sender.Parent.Close();
                    }
                });

                combobox.SelectedIndex = (int)(Task.Priority);
                Layout();
                base.Construct();
            }
        }


        public void CreateTaskPrioritySelector(WorldManager world, Task task, DesignationSet.EntityDesignation des)
        {
            Vector3 pos = des.Body.Position;
            Vector3 screenPos = world.Renderer.Camera.Project(pos);
            if (screenPos.Z > 0.999)
            {
                return;
            }

            world.UserInterface.MakeWorldPopup(new PrioritySelector()
            {
                Task = task,
                ScreenPos = screenPos
            }, des.Body, Vector2.Zero);
        }
    }
}