// PlaceBlockSpell.cs
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
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class PlaceBlockSpell : Spell
    {
        public string VoxelType { get; set; }
        public bool Transmute { get; set; }

        public PlaceBlockSpell(WorldManager world, string voxelType, bool transmute) :
            base(world)
        {
            Texture2D icons = TextureManager.GetTexture(ContentPaths.GUI.icons);
            Image = new ImageFrame(icons, 32, 1, 2);
            VoxelType = voxelType;
            Transmute = transmute;
            if (!transmute)
            {
                Name = VoxelType + " Wall";
                Description = "Create a wall of " + VoxelType;
                Hint = "Click and drag to place " + VoxelType;
                Recharges = false;
                RechargeTimer = new Timer(5.0f, false);
                ManaCost = 15;
                TileRef = 17;
                Mode = SpellMode.SelectEmptyVoxels;
            }
            else
            {
                Name = "Transmute " + VoxelType;
                Description = "Transmute any block into " + VoxelType;
                Hint = "Click and drag to transmute.";
                Recharges = false;
                RechargeTimer = new Timer(5.0f, false);
                ManaCost = 25;
                TileRef = 18;
                Mode = SpellMode.SelectFilledVoxels;
            }

        }

        public override void OnVoxelsSelected(SpellTree tree, List<VoxelHandle> voxels)
        {
            var chunksToRebuild = new HashSet<GlobalVoxelCoordinate>();
            bool placed = false;
            foreach (VoxelHandle selected in voxels)
            {

                if (selected != null && ((!Transmute && selected.IsEmpty) || Transmute && !selected.IsEmpty) && OnCast(tree))
                {
                    Vector3 p = selected.WorldPosition + Vector3.One*0.5f;
                    IndicatorManager.DrawIndicator("-" + ManaCost + " M",p, 1.0f, Color.Red);
                    World.ParticleManager.Trigger("star_particle", p, Color.White, 4);
                    VoxelLibrary.PlaceType(VoxelLibrary.GetVoxelType(VoxelType), selected);

                    if (VoxelType == "Magic")
                    {
                        World.ComponentManager.RootComponent.AddChild(new VoxelListener(World.ComponentManager, World.ChunkManager, selected.tvh)
                        {
                            DestroyOnTimer = true,
                            DestroyTimer = new Timer(5.0f + MathFunctions.Rand(-0.5f, 0.5f), true)
                        });
                    }
                    placed = true;
                    chunksToRebuild.Add(selected.Coordinate);
                }
            }

            foreach (var point in chunksToRebuild)
                World.ChunkManager.ChunkData.NotifyRebuild(point);

            if (placed)
                SoundManager.PlaySound(ContentPaths.Audio.tinkle, World.CursorLightPos, true, 1.0f);

            RechargeTimer.Reset(RechargeTimer.TargetTimeSeconds);
            base.OnVoxelsSelected(tree, voxels);
        }
    }
}
