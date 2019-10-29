using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class TransferResourcesTask : Task
    {
        public Stockpile Stockpile;
        public Resource Resource;

        [JsonIgnore] public WorldManager World;
        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ctx.Context as WorldManager;
        }

        public TransferResourcesTask()
        {

        }

        public TransferResourcesTask(WorldManager World, Stockpile Stockpile, Resource Resource)
        {
            this.World = World;
            Priority = TaskPriority.Medium;
            this.Stockpile = Stockpile;
            this.Resource = Resource;
            Name = String.Format("Transfer {0} from {1}", Resource.TypeName, Stockpile);
            AutoRetry = true;
            ReassignOnDeath = true;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (Stockpile.GetBoundingBox().Center() - agent.AI.Position).LengthSquared();
        }

        public override bool IsComplete(WorldManager World)
        {
            return !Stockpile.Resources.Contains(Resource);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || agent.Stats.IsAsleep || !agent.Active)
                return Feasibility.Infeasible;

            return Feasibility.Infeasible;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            return new TransferResourcesAct(agent.AI, Stockpile, Resource) { Name = "Transfer Resources" };
        }
    }
}