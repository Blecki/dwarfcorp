// FarmAct.cs
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
    public class FarmAct : CompoundCreatureAct
    {
        public Farm FarmToWork { get; set; }

        public FarmAct()
        {
            Name = "Work a farm";
        }

        public FarmAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Work a farm";
        }

        public IEnumerable<Status> FarmATile(string tileName)
        {
            Farm.FarmTile tile = Creature.AI.Blackboard.GetData<Farm.FarmTile>(tileName);
            if (tile == null) yield return Status.Fail;
            else if (tile.PlantExists())
            {
                tile.Farmer = null;
                yield return Status.Success;
            }
            else
            {
                if (tile.Plant != null && tile.Plant.IsDead) tile.Plant = null;
                while (tile.Progress < 100.0f && !tile.PlantExists())
                {

                    Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                    Creature.Physics.Velocity *= 0.1f;
                    tile.Progress += Creature.Stats.BaseFarmSpeed;

                    Drawer2D.DrawLoadBar(Agent.Position + Vector3.Up, Color.White, Color.Black, 100, 16,
                        tile.Progress/100.0f);
                    if (tile.Progress >= 100.0f && !tile.PlantExists())
                    {
                        tile.Progress = 0.0f;
                        FarmToWork.CreatePlant(tile);
                    }

                    yield return Status.Running;
                }
                Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                Creature.AI.AddThought(Thought.ThoughtType.Farmed);
                Creature.AI.AddXP(10);
                tile.Farmer = null;
                yield return Status.Success;
            }
        }

        public IEnumerable<Status> GetClosestTile()
        {
            Farm.FarmTile closestTile = FarmToWork.GetNearestFreeFarmTile(Creature.AI.Position);
            if (closestTile == null) yield return Status.Fail;
            else
            {
                closestTile.Farmer = Agent;
                Creature.AI.Blackboard.SetData("ClosestTile", closestTile);
                Creature.AI.Blackboard.SetData("ClosestVoxel", closestTile.Vox);
                yield return Status.Success;   
            }
        }

        public override void OnCanceled()
        {
            Farm.FarmTile tile = Creature.AI.Blackboard.GetData<Farm.FarmTile>("ClosestTile");

            if (tile != null)
            {
                tile.Farmer = null;
            }

            base.OnCanceled();
        }

        public override void Initialize()
        {
            if (FarmToWork != null)
            {
                Farm.FarmTile closestTile = FarmToWork.GetNearestFreeFarmTile(Creature.AI.Position);
                if (closestTile != null)
                {
                    Tree = new Sequence(
                        new Wrap(GetClosestTile),
                        new GoToVoxelAct("ClosestVoxel", PlanAct.PlanType.Adjacent, Creature.AI),
                        new StopAct(Creature.AI),
                        new Wrap(() => FarmATile("ClosestTile")));
                }
            }

            base.Initialize();
        }
    }
}
