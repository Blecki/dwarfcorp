// SearchFreeStockpileAct.cs
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

        public int CompareTreasurys(Zone A, Zone B)
        {
            if (A == B)
            {
                return 0;
            }
            else
            {
                BoundingBox boxA = A.GetBoundingBox();
                Vector3 centerA = (boxA.Min + boxA.Max) * 0.5f;
                float costA = (Creature.Physics.GlobalTransform.Translation - centerA).LengthSquared() * (float)(decimal)(A as Treasury).Money;

                BoundingBox boxB = B.GetBoundingBox();
                Vector3 centerB = (boxB.Min + boxB.Max) * 0.5f;
                float costB = (Creature.Physics.GlobalTransform.Translation - centerB).LengthSquared() * (float)(decimal)(B as Treasury).Money;

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

            List<Zone> sortedPiles;

            if (ResourceLibrary.GetResourceByName(this.Item.ResourceType).Tags.Contains(Resource.ResourceTags.Money))
            {
                sortedPiles = new List<Zone>(Creature.Faction.Treasurys);
                sortedPiles.Sort(CompareTreasurys);
            }
            else
            {
                sortedPiles =
                    new List<Zone>(Creature.Faction.Stockpiles.Where(pile => pile.IsAllowed(Item.ResourceType)));
                sortedPiles.Sort(CompareStockpiles);
            }

            foreach (Zone s in sortedPiles)
            {
                if(s.IsFull())
                {
                    continue;
                }

                var v = s.GetNearestVoxel(Creature.Physics.GlobalTransform.Translation);

                if(!v.IsValid || v.IsEmpty)
                    continue;

                Voxel = new VoxelHandle(v.Coordinate.GetLocalVoxelCoordinate(), v.Chunk);
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