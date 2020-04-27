using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class SearchFreeStockpileAct : CreatureAct
    {
        public string StockpileBlackboardName;
        public string VoxelBlackboardName;
        public Resource Item;

        public SearchFreeStockpileAct(CreatureAI creature, string StockpileBlackboardName, string VoxelBlackboardName, Resource itemToStock) :
            base(creature)
        {
            Name = "Search Stockpile " + StockpileBlackboardName;
            this.StockpileBlackboardName = StockpileBlackboardName;
            this.VoxelBlackboardName = VoxelBlackboardName;
            Item = itemToStock;
        }

        public int CompareStockpiles(Zone A, Zone B)
        {
            if(A == B)
                return 0;
            else
            {
                BoundingBox boxA = A.GetBoundingBox();
                Vector3 centerA = (boxA.Min + boxA.Max) * 0.5f;
                float costA = (Creature.Physics.GlobalTransform.Translation - centerA).LengthSquared();

                BoundingBox boxB = B.GetBoundingBox();
                Vector3 centerB = (boxB.Min + boxB.Max) * 0.5f;
                float costB = (Creature.Physics.GlobalTransform.Translation - centerB).LengthSquared();

                if(costA < costB)
                    return -1;
                else
                    return 1;
            }
        }

        public override IEnumerable<Status> Run()
        {
            bool validTargetFound = false;

            var sortedPiles = Creature.World.EnumerateZones().OfType<Stockpile>().Where(pile => pile.IsAllowed(Item.TypeName)).ToList();
            sortedPiles.Sort(CompareStockpiles);

            foreach (var s in sortedPiles)
            {
                if(s.IsFull())
                    continue;
            
                var v = s.GetNearestVoxel(Creature.Physics.GlobalTransform.Translation);

                if(!v.IsValid || v.IsEmpty)
                    continue;

                Agent.Blackboard.SetData<VoxelHandle>(VoxelBlackboardName, v);
                Agent.Blackboard.SetData<Zone>(StockpileBlackboardName, s);

                validTargetFound = true;
                break;
            }

            if(validTargetFound)
                yield return Status.Success;
            else
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
            }
        }
    }

}