// Farm.cs
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
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /*
    [JsonObject(IsReference = true)]
    public class Farm : Room
    {
        public class FarmTile
        {
            public Voxel Vox = null;
            public Body Plant = null;
            public float Progress = 0.0f;
            public CreatureAI Farmer = null;
            public bool IsFree()
            {
                return (Plant == null || Plant.IsDead) && Farmer == null;
            }

            public bool PlantExists()
            {
                return !(Plant == null || Plant.IsDead);
            }
        }
        public List<FarmTile> FarmTiles = new List<FarmTile>();

        public virtual Body CreatePlant(Vector3 position)
        {
            return null;
        }

        public override void AddVoxel(Voxel voxel)
        {
            FarmTiles.Add(new FarmTile(){Vox = voxel, Plant = null}); 
            base.AddVoxel(voxel);
        }

        public override void RemoveVoxel(Voxel voxel)
        {
            FarmTile toRemove = FarmTiles.FirstOrDefault(tile => tile.Vox.Equals(voxel));
            if (toRemove != null)
            {
                if(toRemove.Plant != null)
                    toRemove.Plant.Die();
                FarmTiles.Remove(toRemove);
            }
            base.RemoveVoxel(voxel);
        }

        [JsonIgnore]
        public Button FarmButton { get; set; }

        public Farm()
        {

        }

         public Farm(bool designation, IEnumerable<Voxel> designations, RoomData data, ChunkManager chunks) :
            base(designation, designations, data, chunks)
        {

        }

         public Farm(IEnumerable<Voxel> voxels, RoomData data, ChunkManager chunks) :
            base(voxels, data, chunks)
        {

        }

        public void CreatePlant(FarmTile tile)
        {
            tile.Plant = CreatePlant(tile.Vox.Position + new Vector3(0.0f,1.75f, 0.0f));
            Matrix original = tile.Plant.LocalTransform;
            original.Translation += Vector3.Down;
            tile.Plant.AnimationQueue.Add(new EaseMotion(0.5f, original, tile.Plant.LocalTransform.Translation));
            PlayState.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);
            SoundManager.PlaySound(ContentPaths.Audio.pluck, tile.Vox.Position, true);
            AddBody(tile.Plant);
        }

        public FarmTile GetNearestFreeFarmTile(Vector3 position)
        {
            float closestDist = float.MaxValue;
            FarmTile closest = null;
            foreach (FarmTile tile in FarmTiles)
            {
                if (tile.IsFree())
                {
                    float dist = (tile.Vox.Position - position).LengthSquared();
                    if (dist < closestDist)
                    {
                        closest = tile;
                        closestDist = dist;
                    }
                }
            }

            return closest;
        }

        public override void CreateGUIObjects()
        {
            FarmButton = new Button(World.GUI, World.GUI.RootComponent, "Farm", World.GUI.DefaultFont, Button.ButtonMode.ImageButton, new NamedImageFrame(ContentPaths.GUI.icons, 32, 5, 1))
            {
                LocalBounds = new Rectangle(0, 0, 32, 32),
                DrawFrame = true,
                TextColor = Color.White,
                ToolTip = "Click to make selected employees work this " + RoomData.Name,
                DrawOrder = -100
            };
            FarmButton.OnClicked += farmButton_OnClicked;

            if (GUIObject != null)
            {
                GUIObject.Die();
            }

            GUIObject = new WorldGUIObject(PlayState.ComponentManager.RootComponent, FarmButton)
            {
                IsVisible = true,
                LocalTransform = Matrix.CreateTranslation(GetBoundingBox().Center())
            };
        }

        public override void OnBuilt()
        {
            CreateGUIObjects();
            base.OnBuilt();
        }

        void farmButton_OnClicked()
        {
            
            List<CreatureAI> minions = PlayState.Master.SelectedMinions.Where(minion => minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm)).ToList();
            foreach (CreatureAI creature in minions)
            {
                FarmTask task = new FarmTask(this);

                if (!creature.Tasks.Contains(task))
                    creature.Tasks.Add(task);
            }

            if (minions.Count == 0)
            {
                World.GUI.ToolTipManager.Popup("None of the selected units can farm.");
            }
        }

        public override void Destroy()
        {
            if (GUIObject != null)
            {
                GUIObject.Die();
            }
            base.Destroy();
        }
    }
     */
}
