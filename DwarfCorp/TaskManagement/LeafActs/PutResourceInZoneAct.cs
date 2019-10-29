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
        public Stockpile Zone { get { return Agent.Blackboard.GetData<Stockpile>(StockpileName); } set { Agent.Blackboard.SetData(StockpileName, value); } }

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
        public Resource Resource { get { return GetResource(); } set { SetResource(value); } }

        public Resource GetResource()
        {
            return Agent.Blackboard.GetData<Resource>(ResourceName);
        }

        public void SetResource(Resource amount)
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

            var createdItems = Creature.Inventory.RemoveAndCreate(Resource, Inventory.RestockType.RestockResource);

            if (Zone.AddResource(Resource))
            {
                var toss = new TossMotion(1.0f, 2.5f, createdItems.LocalTransform, Zone.GetBoundingBox().Center() + new Vector3(0.5f, 0.5f, 0.5f));

                if (createdItems.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                    physics.CollideMode = Physics.CollisionMode.None;

                createdItems.AnimationQueue.Add(toss);
                toss.OnComplete += createdItems.Die;

                Creature.Stats.NumItemsGathered++;
            }
            else
            {
                Creature.Inventory.AddResource(Resource, Inventory.RestockType.RestockResource);
                createdItems.Delete();
            }

            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
            Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
            Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
            Creature.Sprite.PlayAnimations(Creature.Stats.CurrentClass.AttackMode);

            while (!Creature.Sprite.AnimPlayer.IsDone())
                yield return Status.Running;

            if (Library.GetResourceType(Resource.TypeName).HasValue(out var res) && res.Tags.Contains("Corpse"))
                Creature.AddThought("I laid a friend to rest.", new TimeSpan(0, 8, 0, 0), 10.0f);

            yield return Status.Running;
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
        }
    }

}