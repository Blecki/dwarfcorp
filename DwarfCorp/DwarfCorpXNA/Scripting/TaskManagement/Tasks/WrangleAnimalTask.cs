// BuildTool.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
        }

        public WrangleAnimalTask(Creature animal)
        {
            Category = TaskCategory.Wrangle;
            Animal = animal;
            Name = "Wrangle animal" + animal.GlobalID;
            AutoRetry = true;
        }

        public IEnumerable<Act.Status> PenAnimal(CreatureAI agent, CreatureAI creature, AnimalPen animalPen)
        {
            foreach (var status in animalPen.AddAnimal(Animal.Physics, agent.Faction))
            {
                if (status == Act.Status.Fail)
                {
                    creature.ResetPositionConstraint();
                    yield return Act.Status.Fail;
                    yield break;
                }
            }
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> ReleaseAnimal(CreatureAI animal)
        {
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
            if (LastPen != null && (LastPen.Species == "" || LastPen.Species == Animal.Species) && agent.Faction.GetRooms().Contains(LastPen) && LastPen.IsBuilt)
            {
                return LastPen;
            }

            var pens = agent.Faction.GetRooms().Where(room => room is AnimalPen && room.IsBuilt).Cast<AnimalPen>().Where(pen => pen.Species == "" || pen.Species == Animal.Species);
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
            {
                agent.World.MakeAnnouncement("Can't wrangle " + Animal.Species + "s. Need more animal pens.");
            }
            LastPen = closestPen;
            return closestPen;
        }

        public override Act CreateScript(Creature agent)
        {
            var closestPen = GetClosestPen(agent);
            if (closestPen == null)
            {
                return null;
            }

            closestPen.Species = Animal.Species;

            return new Select(new Sequence(new Domain(() => IsFeasible(agent) == Feasibility.Feasible, new GoToEntityAct(Animal.Physics, agent.AI)),
                new Domain(() => IsFeasible(agent) == Feasibility.Feasible, new Parallel(new Repeat(new Wrap(() => WrangleAnimal(agent.AI, Animal.AI)), -1, false),
                new GoToZoneAct(agent.AI, closestPen)) { ReturnOnAllSucces = false}),
                new Domain(() => IsFeasible(agent) == Feasibility.Feasible, new Wrap(() => PenAnimal(agent.AI, Animal.AI, closestPen)))), 
                new Wrap(() => ReleaseAnimal(Animal.AI)));
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(Task.TaskCategory.Wrangle))
                return Feasibility.Infeasible;

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            return Animal != null
                && !Animal.IsDead
                && agent.Faction.Designations.IsDesignation(Animal.GetRoot().GetComponent<Physics>(), DesignationType.Wrangle)
                && GetClosestPen(agent) != null ? Feasibility.Feasible : Feasibility.Infeasible;
        }


        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (agent.AI.Position - Animal.Physics.Position).LengthSquared();
        }

        public override bool IsComplete(Faction faction)
        {
            return Animal == null || Animal.IsDead || (LastPen != null && LastPen.ZoneBodies.Contains(Animal.Physics));
        }
    }
}
