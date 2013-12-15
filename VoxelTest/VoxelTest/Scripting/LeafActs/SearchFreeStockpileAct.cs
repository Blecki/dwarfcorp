using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class SearchFreeStockpileAct : CreatureAct
    {
        public string StockpileName { get; set; }
        public string VoxelName { get; set; }

        public Stockpile Stockpile { get { return GetStockpile(); } set { SetStockpile(value);} }

        public VoxelRef Voxel { get { return GetVoxel(); } set { SetVoxel(value);} }

        public SearchFreeStockpileAct(CreatureAIComponent creature, string stockName, string voxName) :
            base(creature)
        {
            Name = "Search Stockpile " + stockName;
            StockpileName = stockName;
            VoxelName = voxName;
        }

        public VoxelRef GetVoxel()
        {
            return Agent.Blackboard.GetData<VoxelRef>(VoxelName);
        }


        public void SetVoxel(VoxelRef value)
        {
            Agent.Blackboard.SetData(VoxelName, value);
        }

        public Stockpile GetStockpile()
        {
            return Agent.Blackboard.GetData<Stockpile>(StockpileName);
        }

        public void SetStockpile(Stockpile value)
        {
            Agent.Blackboard.SetData(StockpileName, value);
        }

        public int CompareStockpiles(Stockpile A, Stockpile B)
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

            List<Stockpile> sortedPiles = new List<Stockpile>(Creature.Faction.Stockpiles);

            sortedPiles.Sort(CompareStockpiles);

            foreach(Stockpile s in sortedPiles)
            {
                VoxelRef v = s.GetNearestFreeVoxel(Creature.Physics.GlobalTransform.Translation);

                if(v == null)
                {
                    continue;
                }

                Voxel = v;
                Stockpile = s;
                if(Voxel == null)
                {
                    continue;
                }

                s.SetReserved(v, true);
                validTargetFound = true;
                break;
            }

            if(validTargetFound)
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