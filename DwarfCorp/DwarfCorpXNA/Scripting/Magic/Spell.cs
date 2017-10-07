// Spell.cs
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
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Spell
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ImageFrame Image { get; set; }
        public int TileRef { get; set; }
        public float ManaCost { get; set; }
        public SpellMode Mode { get; set; }
        public Timer RechargeTimer { get; set; }
        public string Hint { get; set; }
        public bool Recharges { get; set; }
        [JsonIgnore]
        public WorldManager World { get; set; }

        public enum SpellMode
        {
            SelectFilledVoxels,
            SelectEmptyVoxels,
            SelectEntities,
            Button,
            Continuous
        }


        public Spell(WorldManager world)
        {
            World = world;
        }

        public virtual bool OnCast(SpellTree tree)
        {
            bool canCast = tree.CanCast(this);

            if (canCast)
            {
                tree.UseMagic(ManaCost);
            }
            else
            {
                SoundManager.PlaySound(ContentPaths.Audio.wurp, World.CursorLightPos, true, 0.25f);
                World.ShowToolPopup("Not enough mana. Need " + (int)ManaCost + " but only have " + (int)tree.Mana);
            }
            return canCast;
        }

        public virtual void OnVoxelsSelected(SpellTree tree, List<VoxelHandle> voxels)
        {
            
        }

        public virtual void OnEntitiesSelected(SpellTree tree, List<Body> entities)
        {
            
        }

        public virtual void OnButtonTriggered()
        {
            World.ShowToolPopup(Hint);
        }

        public virtual void OnContinuousUpdate(DwarfTime time)
        {
            
        }

        public virtual void Update(DwarfTime time, VoxelSelector voxSelector, BodySelector bodySelector)
        {
            if(Recharges)
                RechargeTimer.Update(time);

            switch (Mode)
            {
                case SpellMode.Button:
                    break;
                case SpellMode.Continuous:
                    OnContinuousUpdate(time);
                    break;
                case SpellMode.SelectEmptyVoxels:
                    voxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                    voxSelector.Enabled = true;
                    bodySelector.Enabled = false;
                    break;
                case SpellMode.SelectFilledVoxels:
                    voxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                    voxSelector.Enabled = true;
                    bodySelector.Enabled = false;
                    break;
                case SpellMode.SelectEntities:
                    bodySelector.Enabled = true;
                    break;
            }

            if (!Recharges || RechargeTimer.HasTriggered)
            {
                World.ParticleManager.Trigger("star_particle", World.CursorLightPos + Vector3.Up * 0.5f, Color.White, 2);
            }
        }

        public virtual void Render(DwarfTime time)
        {
            // Todo: Do this with GUI stuff?
            if (Recharges && !RechargeTimer.HasTriggered)
            {
                Drawer2D.DrawLoadBar(World.Camera, World.CursorLightPos - Vector3.Up, Color.Cyan, Color.Black, 64, 4, RechargeTimer.CurrentTimeSeconds / RechargeTimer.TargetTimeSeconds);
                Drawer2D.DrawTextBox("Charging...", World.CursorLightPos + Vector3.Up * 2);
            }
        }


    }
}
