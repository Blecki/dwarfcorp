﻿// Stockpile.cs
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
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// A stockpile is a kind of zone which contains items on top of it.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Stockpile : Room
    {
        private static uint maxID = 0;
        public List<Body> Boxes { get; set; }
        public static string StockpileName = "Stockpile";
        public Faction Faction { get; set; }
        public static uint NextID()
        {
            maxID++;
            return maxID;
        }

        public Stockpile()
        {
            Boxes = new List<Body>();
            ReplacementType = VoxelLibrary.GetVoxelType("Stockpile");
        }


        public Stockpile(Faction faction) :
            base(false, new List<Voxel>(), RoomLibrary.GetData(StockpileName), WorldManager.ChunkManager)
        {
            Boxes = new List<Body>();
            ReplacementType = VoxelLibrary.GetVoxelType("Stockpile");
            faction.Stockpiles.Add(this);
            Faction = faction;
        }

        public Stockpile(Faction faction, IEnumerable<Voxel> voxels, RoomData data, ChunkManager chunks) :
            base(voxels, data, chunks)
        {
            Boxes = new List<Body>();
            faction.Stockpiles.Add(this);
            Faction = faction;
        }

        public Stockpile(Faction faction, bool designation, IEnumerable<Voxel> designations, RoomData data, ChunkManager chunks) :
            base(designation, designations, data, chunks)
        {
            Boxes = new List<Body>();
            faction.Stockpiles.Add(this);
            Faction = faction;
        }

        public void KillBox(Body component)
        {
            EaseMotion deathMotion = new EaseMotion(0.8f, component.LocalTransform, component.LocalTransform.Translation + new Vector3(0, -1, 0));
            component.AnimationQueue.Add(deathMotion);
            deathMotion.OnComplete += component.Die;
            SoundManager.PlaySound(ContentPaths.Audio.whoosh, component.LocalTransform.Translation);
            WorldManager.ParticleManager.Trigger("puff", component.LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 90);
        }

        public void CreateBox(Vector3 pos)
        {
            Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);
            Vector3 endPos = pos + new Vector3(0.0f, 0.9f, 0.0f);

            Body crate = EntityFactory.CreateEntity<Body>("Crate", startPos);
            crate.AnimationQueue.Add(new EaseMotion(0.8f, crate.LocalTransform, endPos));
            Boxes.Add(crate);
            SoundManager.PlaySound(ContentPaths.Audio.whoosh, startPos);
            WorldManager.ParticleManager.Trigger("puff", pos + new Vector3(0.5f, 1.5f, 0.5f), Color.White, 90);
        }

        public void HandleBoxes()
        {
            if (Voxels == null)
            {
               Voxels = new List<Voxel>();
            }

            if (Boxes == null)
            {
                Boxes = new List<Body>();
            }

            if(Voxels.Count == 0)
            {
                foreach(Body component in Boxes)
                {
                    KillBox(component);
                }
                Boxes.Clear();
            }

            int numBoxes = Math.Min(Math.Max(Resources.CurrentResourceCount / ResourcesPerVoxel, 1), Voxels.Count);

            if (Boxes.Count > numBoxes)
            {
                for (int i = Boxes.Count - 1; i >= numBoxes; i--)
                {
                    KillBox(Boxes[i]);
                    Boxes.RemoveAt(i);
                }
            }
            else if (Boxes.Count < numBoxes)
            {
                for (int i = Boxes.Count; i < numBoxes; i++)
                {
                    CreateBox(Voxels[i].Position + VertexNoise.GetNoiseVectorFromRepeatingTexture(Voxels[i].Position));
                }
            }
        }

       

        public override bool AddItem(Body component)
        {
            bool worked =  base.AddItem(component);
            HandleBoxes();

            TossMotion toss = new TossMotion(1.0f, 2.5f, component.LocalTransform, Boxes[Boxes.Count - 1].LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f));
            component.AnimationQueue.Add(toss);
            toss.OnComplete += component.Die;

            return worked;
        }


        public override void Destroy()
        {
            BoundingBox box = GetBoundingBox();
            foreach (ResourceAmount resource in Resources)
            {
                for (int i = 0; i < resource.NumResources; i++)
                {
                    Physics body = EntityFactory.CreateEntity<Physics>(resource.ResourceType.Type + " Resource",
                        Vector3.Up + MathFunctions.RandVector3Box(box)) as Physics;

                    if (body != null)
                    {
                        body.Velocity = MathFunctions.RandVector3Cube();
                    }
                }
            }

            if (Faction != null)
            {
                Faction.Stockpiles.Remove(this);
            }
            base.Destroy();
        }

        public override void RecalculateMaxResources()
        {

            HandleBoxes();
            base.RecalculateMaxResources();
        }

        public static RoomData InitializeData()
        {
           List<RoomTemplate> stockpileTemplates = new List<RoomTemplate>();
           Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>()
            {
            };
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(StockpileName, 0, "Stockpile", roomResources, stockpileTemplates, new ImageFrame(roomIcons, 16, 0, 0))
            {
                Description = "Dwarves can stock resources here",
            };
        }
    }

}