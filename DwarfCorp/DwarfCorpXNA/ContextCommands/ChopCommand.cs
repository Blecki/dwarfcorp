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


    public class GoToCommand : ContextCommand
    {
        public GoToCommand()
        {
            Name = "Go Here";
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return World.Master.SelectedMinions.Count > 0;
        }

        public override void Apply(Body Entity, WorldManager World)
        {
           foreach(var minion in World.Master.SelectedMinions)
            {
                minion.AssignTask(new GoToEntityAct(Entity, minion).AsTask());
            }
        }
    }

    public class CancelCommand : ContextCommand
    {
        public CancelCommand()
        {
            Name = "Cancel";
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

    public class GatherCommand : ContextCommand
    {
        public GatherCommand()
        {
            Name = "Gather";
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
            Icon = new Gui.TileReference("tool-icons", 1);
        }

        public override bool CanBeAppliedTo(Body Entity, WorldManager World)
        {
            return (World.Master.Tools[GameMaster.ToolMode.MoveObjects] as MoveObjectTool).CanMove(Entity);
        }

        public override void Apply(Body Entity, WorldManager World)
        {
            World.Master.ChangeTool(GameMaster.ToolMode.MoveObjects);
            (World.Master.Tools[GameMaster.ToolMode.MoveObjects] as MoveObjectTool).StartDragging(Entity);
        }
    }

    public class FireCommand : ContextCommand
    {
        public FireCommand()
        {
            Name = "Fire";
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

    public class PromoteCommand : ContextCommand
    {
        public PromoteCommand()
        {
            Name = "Promote";
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
            Employee.Stats.LevelUp();
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
            Employee.Creature.AddThought(Thought.ThoughtType.GotPromoted);
        }
    }
}
