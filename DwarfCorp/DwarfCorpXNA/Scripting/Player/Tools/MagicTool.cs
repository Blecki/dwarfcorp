// MagicTool.cs
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
    public class MagicTool : PlayerTool
    {
        public Spell CurrentSpell { get; set; }

        public MagicTool()
        {
            
        }

        public MagicTool(GameMaster master)
        {
            Player = master;
        }

        public override void OnBegin()
        {
            
        }

        public override void OnEnd()
        {

        }

        void MagicMenu_SpellTriggered(Spell spell)
        {
            CurrentSpell = spell;
        }

        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            if (CurrentSpell != null && (CurrentSpell.Mode == Spell.SpellMode.SelectFilledVoxels || CurrentSpell.Mode == Spell.SpellMode.SelectEmptyVoxels))
            {
                CurrentSpell.OnVoxelsSelected(Player.Spells, voxels);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (CurrentSpell != null && CurrentSpell.Mode == Spell.SpellMode.SelectEntities)
            {
                CurrentSpell.OnEntitiesSelected(Player.Spells, bodies);
            }
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }

        public void Research(SpellTree.Node spell)
        {
            List<CreatureAI> wizards = Faction.FilterMinionsWithCapability(Player.SelectedMinions, GameMaster.ToolMode.Magic);
            var body = Player.Faction.FindNearestItemWithTags("Research", Vector3.Zero, false);

            if (body != null)
            { 
                Player.World.ShowToolPopup(string.Format("{0} wizard{2} sent to research {1}", wizards.Count, spell.Spell.Name, wizards.Count > 1 ? "s" : ""));

                foreach (CreatureAI wizard in wizards)
                {
                    wizard.Tasks.Add(new ActWrapperTask(new GoResearchSpellAct(wizard, spell))
                    {
                        Priority = Task.PriorityType.Low
                    });
                }
            }
            else
            {
                Player.World.ShowToolPopup(string.Format("Can't research {0}, no library has been built.",
                    spell.Spell.Name));
            }
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            Player.BodySelector.Enabled = false;
            Player.VoxSelector.Enabled = false;

            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }
            else
                Player.World.SetMouse(Player.World.MousePointer);

            if (CurrentSpell != null)
            {
                CurrentSpell.Update(time, Player.VoxSelector, Player.BodySelector);
            }

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gum.MousePointer("mouse", 1, 8));

        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            if (CurrentSpell != null)
            {
                CurrentSpell.Render(time);
            }
        }

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }

    }
}
