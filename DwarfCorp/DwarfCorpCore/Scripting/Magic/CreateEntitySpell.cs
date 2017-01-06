// CreateEntitySpell.cs
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
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CreateEntitySpell : Spell
    {
        public string Entity { get; set; }
        public bool Transmute { get; set; }

        public CreateEntitySpell()
        {
            
        }

        public CreateEntitySpell(string entity, bool transmute)
        {
            Entity = entity;
            Transmute = transmute;
            ManaCost = 50;
            Mode = transmute ? SpellMode.SelectEntities : SpellMode.SelectEmptyVoxels;
            Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 4, 2);
        }

        public void CreateEntity(Vector3 position)
        {
            EntityFactory.CreateEntity<Body>(Entity, position);
            PlayState.ParticleManager.Trigger("star_particle", position, Color.White, 4);
            Vector3 p = position + Vector3.Up;
            IndicatorManager.DrawIndicator("-" + ManaCost + " M", p, 1.0f, Color.Red);
        }

        public override void OnEntitiesSelected(SpellTree tree, List<Body> entities)
        {
            if (!Transmute) return;

            foreach (Body body in entities)
            {
                if (OnCast(tree))
                {
                    CreateEntity(body.Position);
                    body.Delete();
                }
            }

            base.OnEntitiesSelected(tree, entities);
        }

        public override void OnVoxelsSelected(SpellTree tree, List<Voxel> voxels)
        {
            if (Transmute) return;
            bool got = false;
            foreach (Voxel voxel in voxels)
            {
                if (voxel.IsEmpty)
                {
                    if (OnCast(tree))
                    {
                        CreateEntity(voxel.Position + Vector3.One*0.5f);
                        got = true;
                    }
                }
            }

            if (got)
            {
                SoundManager.PlaySound(ContentPaths.Audio.tinkle, PlayState.CursorLightPos, true, 1.0f);
            }

            base.OnVoxelsSelected(tree, voxels);
        }
    }
}
