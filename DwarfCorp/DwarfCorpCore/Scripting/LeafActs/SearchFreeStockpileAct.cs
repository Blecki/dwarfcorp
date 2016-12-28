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

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A creature looks for the nearest, free stockpile and puts that information onto the blackboard.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class SearchFreeStockpileAct : CreatureAct
    {
        public SearchFreeStockpileAct(CreatureAI creature, string stockName, string voxName) :
            base(creature)
        {
            Name = "Search Stockpile " + stockName;
            StockpileName = stockName;
            VoxelName = voxName;
        }

        public string StockpileName { get; set; }
        public string VoxelName { get; set; }

        public Stockpile Stockpile
        {
            get { return GetStockpile(); }
            set { SetStockpile(value); }
        }

        public Voxel Voxel
        {
            get { return GetVoxel(); }
            set { SetVoxel(value); }
        }

        public Voxel GetVoxel()
        {
            return Agent.Blackboard.GetData<Voxel>(VoxelName);
        }


        public void SetVoxel(Voxel value)
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
            if (A == B)
            {
                return 0;
            }
            BoundingBox boxA = A.GetBoundingBox();
            Vector3 centerA = (boxA.Min + boxA.Max)*0.5f;
            float costA = (Creature.Physics.GlobalTransform.Translation - centerA).LengthSquared();

            BoundingBox boxB = B.GetBoundingBox();
            Vector3 centerB = (boxB.Min + boxB.Max)*0.5f;
            float costB = (Creature.Physics.GlobalTransform.Translation - centerB).LengthSquared();

            if (costA < costB)
            {
                return -1;
            }
            return 1;
        }

        public override IEnumerable<Status> Run()
        {
            bool validTargetFound = false;

            var sortedPiles = new List<Stockpile>(Creature.Faction.Stockpiles);

            sortedPiles.Sort(CompareStockpiles);

            foreach (Stockpile s in sortedPiles)
            {
                if (s.IsFull())
                {
                    continue;
                }

                Voxel v = s.GetNearestVoxel(Creature.Physics.GlobalTransform.Translation);

                if (v.IsEmpty)
                {
                    continue;
                }

                Voxel = v;
                Stockpile = s;
                if (Voxel == null)
                {
                    continue;
                }

                validTargetFound = true;
                break;
            }

            if (validTargetFound)
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