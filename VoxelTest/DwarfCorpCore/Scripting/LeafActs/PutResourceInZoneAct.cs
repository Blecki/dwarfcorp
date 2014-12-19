using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature puts a specified resource (in its inventory) into a zone.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PutResourceInZone : CreatureAct
    {
        [JsonIgnore]
        public Zone Zone { get { return GetZone(); } set { SetZone(value); } }

        public Zone GetZone()
        {
            return Agent.Blackboard.GetData<Stockpile>(StockpileName);
        }

        public void SetZone(Zone pile)
        {
            Agent.Blackboard.SetData(StockpileName, pile);
        }


        [JsonIgnore]
        public Voxel Voxel { get { return GetVoxel(); } set { SetVoxel(value); } }

        public Voxel GetVoxel()
        {
            return Agent.Blackboard.GetData<Voxel>(VoxelName);
        }

        public void SetVoxel(Voxel voxel)
        {
            Agent.Blackboard.SetData(VoxelName, voxel);
        }

        public string StockpileName { get; set; }
        public string VoxelName { get; set; }

        [JsonIgnore]
        public ResourceAmount Resource { get { return GetResource(); } set { SetResource(value); } }

        public ResourceAmount GetResource()
        {
            return Agent.Blackboard.GetData<ResourceAmount>(ResourceName);
        }

        public void SetResource(ResourceAmount amount)
        {
            Agent.Blackboard.SetData(ResourceName, amount);
        }

        public string ResourceName { get; set; }

        public PutResourceInZone(CreatureAI agent, string stockpileName, string voxelname, string resourceName) :
            base(agent)
        {
            VoxelName = voxelname;
            Name = "Put Item";
            StockpileName = stockpileName;
            ResourceName = resourceName;
        }

        public override IEnumerable<Status> Run()
        {
            if (Zone == null)
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
                yield break;
            }

            List<Body> createdItems = Creature.Inventory.RemoveAndCreate(Resource);
            if(createdItems.Count == 0)
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
            }

            foreach (Body b in createdItems)
            {
                if (Zone.AddItem(b))
                {
                    Creature.NoiseMaker.MakeNoise("Hurt", Creature.AI.Position);
                    Creature.Stats.NumItemsGathered++;
                    yield return Status.Running;
                }
                else
                {
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Status.Fail;
                }   
            }

            yield return Status.Success;
        }
    }

}