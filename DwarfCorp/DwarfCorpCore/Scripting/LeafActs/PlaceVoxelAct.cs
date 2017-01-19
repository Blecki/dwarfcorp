﻿// PlaceVoxelAct.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature uses the item currently in its hands to construct a voxel.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlaceVoxelAct : CreatureAct
    {
        public Voxel Voxel { get; set; }
        public ResourceAmount Resource { get; set; }
        public PlaceVoxelAct(Voxel voxel, CreatureAI agent, ResourceAmount resource) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Build Voxel " + voxel.ToString();
            Resource = resource;
        }

        public override IEnumerable<Status> Run()
        {
            if (!Creature.Faction.WallBuilder.IsDesignation(Voxel))
            {
                yield return Status.Fail;
                yield break;
            }

            foreach (Status status in Creature.HitAndWait(1.0f, true))
            {
                if (status == Status.Running)
                {
                    yield return status;
                }
            }

            Body grabbed = Creature.Inventory.RemoveAndCreate(Resource).FirstOrDefault();

            if(grabbed == null)
            {
                yield return Status.Fail;
                yield break;
            }
            else
            {
                if(Creature.Faction.WallBuilder.IsDesignation(Voxel))
                {
                    TossMotion motion = new TossMotion(1.0f, 2.0f, grabbed.LocalTransform, Voxel.Position + new Vector3(0.5f, 0.5f, 0.5f));
                    motion.OnComplete += grabbed.Die;
                    grabbed.AnimationQueue.Add(motion);

                    WallBuilder put = Creature.Faction.WallBuilder.GetDesignation(Voxel);
                    put.Put(WorldManager.ChunkManager);


                    Creature.Faction.WallBuilder.Designations.Remove(put);
                    Creature.Stats.NumBlocksPlaced++;
                    Creature.AI.AddXP(1);
                    yield return Status.Success;
                }
                else
                {
                    Creature.Inventory.Resources.AddItem(grabbed);
                    grabbed.Die();
                    
                    yield return Status.Fail;
                }
            }
        }
    }

}