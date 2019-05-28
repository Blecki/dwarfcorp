using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature looks for the nearest, free stockpile and puts that information onto the blackboard.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class SearchFreeStockpileAct : CreatureAct
    {
        public string StockpileName { get; set; }
        public string VoxelName { get; set; }

        // Todo: Most of these properties are useless cruft.
        public Zone Stockpile { get { return GetStockpile(); } set { SetStockpile(value);} }

        public VoxelHandle Voxel { get { return GetVoxel(); } set { SetVoxel(value);} }

        public ResourceAmount Item { get; set; }

        public SearchFreeStockpileAct(CreatureAI creature, string stockName, string voxName, ResourceAmount itemToStock) :
            base(creature)
        {
            Name = "Search Stockpile " + stockName;
            StockpileName = stockName;
            VoxelName = voxName;
            Item = itemToStock;
        }

        public VoxelHandle GetVoxel()
        {
            return Agent.Blackboard.GetData<VoxelHandle>(VoxelName);
        }

        public void SetVoxel(VoxelHandle value)
        {
            Agent.Blackboard.SetData(VoxelName, value);
        }

        public Zone GetStockpile()
        {
            return Agent.Blackboard.GetData<Stockpile>(StockpileName);
        }

        public void SetStockpile(Zone value)
        {
            Agent.Blackboard.SetData(StockpileName, value);
        }

        public int CompareStockpiles(Zone A, Zone B)
        {
            if(A == B)
            {
                return 0;
            }
            else
            {
                BoundingBox boxA = A.GetBoundingBox();
                Vector3 centerA = (boxA.Min + boxA.Max) * 0.5f;
                float costA = (Creature.Physics.GlobalTransform.Translation - centerA).LengthSquared();

                BoundingBox boxB = B.GetBoundingBox();
                Vector3 centerB = (boxB.Min + boxB.Max) * 0.5f;
                float costB = (Creature.Physics.GlobalTransform.Translation - centerB).LengthSquared();

                if(costA < costB)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        public override IEnumerable<Status> Run()
        {
            bool validTargetFound = false;

            List<Zone> sortedPiles;

                sortedPiles =
                    new List<Zone>(Creature.World.EnumerateZones().Where(pile => pile is Stockpile && (pile as Stockpile).IsAllowed(Item.Type)));
                sortedPiles.Sort(CompareStockpiles);

            foreach (Zone s in sortedPiles)
            {
                if(s.IsFull())
                {
                    continue;
                }

                var v = s.GetNearestVoxel(Creature.Physics.GlobalTransform.Translation);

                if(!v.IsValid || v.IsEmpty)
                    continue;

                Voxel = v;
                Stockpile = s;

                validTargetFound = true;
                break;
            }

            if(validTargetFound)
            {
                yield return Status.Success;
            }
            else
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
            }
        }
    }

}