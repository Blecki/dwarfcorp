using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.ContextCommands
{
    public class ChopCommand : ContextCommand
    {
        public ChopCommand()
        {
            Name = "Harvest";
            Description = "Click to harvest the selected plant(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return Entity.Tags.Contains("Vegetation");
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var minions = Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions, Task.TaskCategory.Chop);
            if (minions.Count > 0)
                World.Master.TaskManager.AddTask(new ChopEntityTask(Entity));
        }
    }

    public class AttackCommand : ContextCommand
    {
        public AttackCommand()
        {
            Name = "Attack";
            Description = "Click to attack the selected creature(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.Attack] as AttackTool).CanAttack(Entity);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var minions = Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions, Task.TaskCategory.Attack);
            if (minions.Count > 0)
                World.Master.TaskManager.AddTask(new KillEntityTask(Entity, KillEntityTask.KillType.Attack));
        }
    }

    public class WrangleCommand : ContextCommand
    {
        public WrangleCommand()
        {
            Name = "Catch";
            Description = "Click to catch the selected creature(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.Wrangle] as WrangleTool).CanCatch(Entity);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var minions = Faction.FilterMinionsWithCapability(World.PlayerFaction.Minions, Task.TaskCategory.Wrangle);
            if (minions.Count > 0)
                World.Master.TaskManager.AddTask(new WrangleAnimalTask(Entity.GetRoot().GetComponent<Creature>()) { Priority = Task.PriorityType.Medium });
        }
    }

    public class CancelCommand : ContextCommand
    {
        public CancelCommand()
        {
            Name = "Cancel";
            Description = "Click to cancel the selected command(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return World.Master.Faction.Designations.EnumerateEntityDesignations(Entity).Any();
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            foreach (var des in World.PlayerFaction.Designations.EnumerateEntityDesignations(Entity).ToList())
                if (des.Task != null)
                    World.Master.TaskManager.CancelTask(des.Task);
        }
    }

    public class PriorityCommand : ContextCommand
    {
        public PriorityCommand()
        {
            Name = "Priority";
            Description = "Click to set the priority of the selected command(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return World.Master.Faction.Designations.EnumerateEntityDesignations(Entity).Any();
        }

        public override void Apply(Body Entity, WorldManager World)
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
            Vector3 screenPos = world.Camera.Project(pos);
            if (screenPos.Z > 0.999)
            {
                return;
            }

            world.MakeWorldPopup(new PrioritySelector()
            {
                Task = task,
                ScreenPos = screenPos
            }, des.Body, Vector2.Zero);
        }
    }

    public class GatherCommand : ContextCommand
    {
        public GatherCommand()
        {
            Name = "Gather";
            Description = "Click to gather the selected object(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.Gather] as GatherTool).CanGather(Entity);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            World.Master.TaskManager.AddTask(new GatherItemTask(Entity));
        }
    }

    public class DestroyCommand : ContextCommand
    {
        public DestroyCommand()
        {
            Name = "Destroy";
            Description = "Click to destroy the selected object(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.DeconstructObjects] as DeconstructObjectTool).CanDestroy(Entity);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            Entity.GetRoot().Die();
        }
    }

    public class MoveCommand : ContextCommand
    {
        public MoveCommand()
        {
            Name = "Move";
            Description = "Click to move the selected object";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.MoveObjects] as MoveObjectTool).CanMove(Entity);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            World.Master.ChangeTool(GameMaster.ToolMode.MoveObjects);
        }
    }

    public class FireCommand : ContextCommand
    {
        public FireCommand()
        {
            Name = "Fire";
            Description = "Click to fire the selected dwarf(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature);
        }
        
        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            World.PlayerFaction.Minions.Remove(creature);
            World.PlayerFaction.SelectedMinions.Remove(creature);
            Entity.GetRoot().Delete();
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
        }
    }

    public class ChatCommand : ContextCommand
    {
        public ChatCommand()
        {
            Name = "Chat";
            Description = "Click to talk to the selected dwarf.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            var thoughts = Entity.GetComponent<DwarfThoughts>();
            if (thoughts == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            creature.Chat();
        }
    }

    public class PromoteCommand : ContextCommand
    {
        public PromoteCommand()
        {
            Name = "Promote";
            Description = "Click to promote the selected dwarf(s)";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature) && creature.Stats.IsOverQualified;
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var Employee = Entity.GetComponent<CreatureAI>();
            var prevLevel = Employee.Stats.CurrentLevel;
            Employee.Stats.LevelUp();
            if (Employee.Stats.CurrentLevel.HealingPower > prevLevel.HealingPower)
            {
                World.MakeAnnouncement(String.Format("{0}'s healing power increased to {1}!", Employee.Stats.FullName, Employee.Stats.CurrentLevel.HealingPower));
            }

            if (Employee.Stats.CurrentLevel.ExtraAttacks.Count > prevLevel.ExtraAttacks.Count)
            {
                World.MakeAnnouncement(String.Format("{0} learned to cast {1}!", Employee.Stats.FullName, Employee.Stats.CurrentLevel.ExtraAttacks.Last().Name));
            }
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
            Employee.Creature.AddThought(Thought.ThoughtType.GotPromoted);
        }
    }

    public class ViewAllowedTasksCommand : ContextCommand
    {

        public ViewAllowedTasksCommand()
        {
            Name = "Allowed tasks...";
            Description = "Click to view the selected dwarfs allowed tasks.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return;

            var screen = World.Gui.RenderData.VirtualScreen;
            World.Gui.ShowMinorPopup(new AllowedTaskFilter
            {
                Employee = creature,
                Tag = "selected-employee-allowable-tasks",
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(256, 256),
                Border = "border-fancy",
                Rect = new Rectangle(screen.Center.X - 128, screen.Center.Y - 128, 256, 256)
            });

            base.Apply(Entity, World);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature);
        }
    }


    public class EmptyBackpackCommand  : ContextCommand
    {
        public EmptyBackpackCommand()
        {
            Name = "Empty backpack";
            Description = "Click to force the selected dwarf(s) to empty their backpacks.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature) && creature.Creature.Inventory.Resources.Any();
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return;

            creature.Creature.RestockAllImmediately(true);
        }
    }

    public class CancelDwarfCommand : ContextCommand
    {
        public CancelDwarfCommand()
        {
            Name = "Cancel Task";
            Description = "Click to force the selected dwarf(s) to cancel their current task.";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return false;
            return World.Master.Faction.Minions.Contains(creature) && creature.CurrentTask != null;
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            var creature = Entity.GetComponent<CreatureAI>();
            if (creature == null)
                return;

            creature.CancelCurrentTask();
        }
    }
}
