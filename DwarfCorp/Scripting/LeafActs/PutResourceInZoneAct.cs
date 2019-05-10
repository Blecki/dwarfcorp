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
        public VoxelHandle Voxel { get { return GetVoxel(); } set { SetVoxel(value); } }

        public VoxelHandle GetVoxel()
        {
            return Agent.Blackboard.GetData<VoxelHandle>(VoxelName);
        }

        public void SetVoxel(VoxelHandle voxel)
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

            if (Resource.Count <= 0)
            {
                yield return Status.Success;
                yield break;
            }

            List<GameComponent> createdItems = Creature.Inventory.RemoveAndCreate(Resource, Inventory.RestockType.RestockResource);
            if(createdItems.Count == 0)
            {
                yield return Status.Success;
            }

            foreach (GameComponent b in createdItems)
            {
                if (Zone.AddItem(b))
                {
                    Creature.Stats.NumItemsGathered++;
                }
                else
                {
                    Creature.Inventory.AddResource(new ResourceAmount(Resource.Type, 1), Inventory.RestockType.RestockResource);
                    b.Delete();
                }
            }

            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
            Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
            Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
            Creature.Sprite.PlayAnimations(Creature.Stats.CurrentClass.AttackMode);
            while (!Creature.Sprite.AnimPlayer.IsDone())
            {
                yield return Status.Running;
            }

            var resource = ResourceLibrary.GetResourceByName(Resource.Type);
            if (resource.Tags.Contains(DwarfCorp.Resource.ResourceTags.Corpse))
            {
                Creature.AddThought(Thought.ThoughtType.BuriedDead);
            }

            yield return Status.Running;
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
        }
    }

}