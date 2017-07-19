// DestroyBlockSpell.cs
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
using System.Runtime.Remoting.Channels;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class DestroyBlockSpell : Spell
    {
        public DestroyBlockSpell(WorldManager world) :
            base(world)
        {
            Texture2D icons = TextureManager.GetTexture(ContentPaths.GUI.icons);
            Description = "Magically destroys up to 8 stone, dirt, or other blocks.";
            Image = new ImageFrame(icons, 32, 2, 2);
            ManaCost = 10;
            Mode = Spell.SpellMode.SelectFilledVoxels;
            Name = "Destroy Blocks";
            Hint = "Click and drag to destroy blocks";
            Recharges = false;
            TileRef = 18;
        }
        public override void OnVoxelsSelected(SpellTree tree, List<TemporaryVoxelHandle> voxels)
        {
            bool destroyed = false;
            foreach (var selected in voxels)
            {
                if (!selected.IsEmpty && !selected.Type.IsInvincible)
                {
                    if (OnCast(tree))
                    {
                        Vector3 p = selected.WorldPosition + Vector3.One * 0.5f;
                        IndicatorManager.DrawIndicator("-" + ManaCost + " M", p, 1.0f, Color.Red);
                        World.ParticleManager.Trigger("star_particle", p,
                            Color.White, 4);
                        World.ChunkManager.KillVoxel(selected);
                        destroyed = true;
                    }
                }

            }
            if (destroyed)
            {
                SoundManager.PlaySound(ContentPaths.Audio.tinkle, World.CursorLightPos, true, 1.0f);
            }

            base.OnVoxelsSelected(tree, voxels);
        }
    }
}
