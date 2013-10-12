using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class SearchFreeStockpileAct : CreatureAct
    {
        public SearchFreeStockpileAct(CreatureAIComponent creature) :
            base(creature)
        {
            Name = "Search Stockpile";
        }

        public int CompareStockpiles(Stockpile A, Stockpile B)
        {
            if (A == B)
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

                if (costA < costB)
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

            List<Stockpile> sortedPiles = new List<Stockpile>(Creature.Master.Stockpiles);

            sortedPiles.Sort(CompareStockpiles);

            foreach (Stockpile s in sortedPiles)
            {
                VoxelRef v = s.GetNearestFreeVoxel(Creature.Physics.GlobalTransform.Translation);

                if (v != null)
                {
                    Agent.TargetVoxel = v;
                    Agent.TargetStockpile = s;
                    if (Agent.TargetVoxel != null)
                    {
                        s.SetReserved(v, true);
                        validTargetFound = true;
                        break;
                    }
                }
            }

            if (validTargetFound)
            {
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }
    }
}
