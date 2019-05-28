using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
    public class WrangleAnimalTask : Task
    {
        public Creature Animal { get; set; }
        public AnimalPen LastPen { get; set; }

        public WrangleAnimalTask()
        {
            Category = TaskCategory.Wrangle;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            Priority = PriorityType.Medium;
        }

        public WrangleAnimalTask(Creature animal)
        {
            Category = TaskCategory.Wrangle;
            Animal = animal;
            Name = "Wrangle animal" + animal.GlobalID;
            AutoRetry = true;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            Priority = PriorityType.Medium;
        }

        public IEnumerable<Act.Status> PenAnimal(CreatureAI agent, CreatureAI creature, AnimalPen animalPen)
        {
            foreach (var status in animalPen.AddAnimal(Animal.Physics, agent.Faction))
            {
                if (status == Act.Status.Fail)
                {
                    creature.ResetPositionConstraint();
                    agent.SetMessage("Failed to pen animal.");
                    yield return Act.Status.Fail;
                    yield break;
                }
            }
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> ReleaseAnimal(CreatureAI animal, CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                var designation = creature.Faction.Designations.GetEntityDesignation(animal.GetRoot().GetComponent<Physics>(), DesignationType.Wrangle);
                if (designation != null)
                {
                    if (creature.Faction == creature.World.PlayerFaction)
                    {
                        creature.World.MakeAnnouncement(String.Format("{0} stopped trying to catch {1} because it is unreachable.", creature.Stats.FullName, animal.Stats.FullName));
                        creature.World.TaskManager.CancelTask(designation.Task);
                    }
                }
            }

            animal.ResetPositionConstraint();
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> WrangleAnimal(CreatureAI agent, CreatureAI creature)
        {
            creature.PositionConstraint = new BoundingBox(agent.Position - new Vector3(1.0f, 0.5f, 1.0f), 
                agent.Position + new Vector3(1.0f, 0.5f, 1.0f));
            Drawer3D.DrawLine(creature.Position, agent.Position, Color.Black, 0.05f);
            yield return Act.Status.Success;
        }

        public AnimalPen GetClosestPen(Creature agent)
        {
            if (LastPen != null && (LastPen.Species == "" || LastPen.Species == Animal.Stats.CurrentClass.Name) && agent.World.EnumerateZones().Contains(LastPen) && LastPen.IsBuilt)
            {
                return LastPen;
            }

            var pens = agent.World.EnumerateZones().Where(room => room is AnimalPen && room.IsBuilt).Cast<AnimalPen>().Where(pen => pen.Species == "" || pen.Species == Animal.Stats.CurrentClass.Name);
            AnimalPen closestPen = null;
            float closestDist = float.MaxValue;

            foreach (var pen in pens)
            {
                var dist = (pen.GetBoundingBox().Center() - agent.Physics.Position).LengthSquared();
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPen = pen;
                }
            }

            if (closestPen == null)
                agent.World.UserInterface.MakeWorldPopup("Can't wrangle " + Animal.Stats.CurrentClass.Name + "s. Need more animal pens.", Animal.Physics, -10, 10);

            LastPen = closestPen;
            return closestPen;
        }

        public override Act CreateScript(Creature agent)
        {
            var closestPen = GetClosestPen(agent);
            if (closestPen == null)
                return null;


            closestPen.Species = Animal.Stats.CurrentClass.Name;

            return new Select(new Sequence(new Domain(() => IsFeasible(agent) == Feasibility.Feasible, new GoToEntityAct(Animal.Physics, agent.AI)),
                new Domain(() => IsFeasible(agent) == Feasibility.Feasible, new Parallel(new Repeat(new Wrap(() => WrangleAnimal(agent.AI, Animal.AI)), -1, false),
                new GoToZoneAct(agent.AI, closestPen)) { ReturnOnAllSucces = false}),
                new Domain(() => IsFeasible(agent) == Feasibility.Feasible, new Wrap(() => PenAnimal(agent.AI, Animal.AI, closestPen)))), 
                new Wrap(() => ReleaseAnimal(Animal.AI, agent.AI)));
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(Task.TaskCategory.Wrangle))
                return Feasibility.Infeasible;

            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            return Animal != null
                && !Animal.IsDead
                && GetClosestPen(agent) != null ? Feasibility.Feasible : Feasibility.Infeasible;
        }


        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (agent.AI.Position - Animal.Physics.Position).LengthSquared();
        }

        public override bool IsComplete(Faction faction)
        {
            return Animal == null || Animal.IsDead || (LastPen != null && LastPen.Animals.Contains(Animal.Physics));
        }

        public override void OnEnqueued(Faction Faction)
        {
            Faction.Designations.AddEntityDesignation(Animal.Physics, DesignationType.Wrangle, null, this);
        }

        public override void OnDequeued(Faction Faction)
        {
            Faction.Designations.RemoveEntityDesignation(Animal.Physics, DesignationType.Wrangle);
        }
    }
}
