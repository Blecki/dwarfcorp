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
        public MagicMenu MagicMenu { get; set; }
        public ProgressBar MagicBar { get; set; }


        public MagicTool()
        {
            
        }

        public override void OnBegin()
        {
            if (MagicMenu != null)
            {
                MagicMenu.Destroy();
            }

            MagicMenu = new MagicMenu(WorldManager.GUI, WorldManager.GUI.RootComponent, Player)
            {
                LocalBounds = new Rectangle(PlayState.Game.GraphicsDevice.Viewport.Width - 750, PlayState.Game.GraphicsDevice.Viewport.Height - 512, 700, 350),
                DrawOrder = 3
            };
            MagicMenu.SpellTriggered += MagicMenu_SpellTriggered;

            MagicMenu.IsVisible = true;
            MagicMenu.LocalBounds = new Rectangle(GameState.Game.GraphicsDevice.Viewport.Width - 750,
                GameState.Game.GraphicsDevice.Viewport.Height - 512, 700, 350);

            if (MagicBar != null)
            {
                MagicBar.Destroy();
            }

            MagicBar = new ProgressBar(WorldManager.GUI, WorldManager.GUI.RootComponent, MagicMenu.Master.Spells.Mana / MagicMenu.Master.Spells.MaxMana)
            {
                ToolTip = "Remaining Mana Pool",
                LocalBounds = new Rectangle(GameState.Game.GraphicsDevice.Viewport.Width - 200, 68, 180, 32),
                Tint = Color.Cyan,
                DrawOrder = 4
            };
            MagicBar.OnUpdate += MagicBar_OnUpdate;

            MagicBar.IsVisible = true;
            MagicMenu.TweenIn(Drawer2D.Alignment.Right, 0.25f);
            MagicBar.TweenIn(Drawer2D.Alignment.Right, 0.25f);
        }

        void MagicBar_OnUpdate()
        {
            if (MagicBar.IsVisible)
            {
                MagicBar.Value = MagicMenu.Master.Spells.Mana/MagicMenu.Master.Spells.MaxMana;
                MagicBar.ToolTip = "Remaining Mana Pool " + (int) MagicMenu.Master.Spells.Mana;
            }
        }

        public override void OnEnd()
        {
            MagicMenu.TweenOut(Drawer2D.Alignment.Right);
            MagicBar.TweenOut(Drawer2D.Alignment.Right);
        }


        public MagicTool(GameMaster master)
        {
            Player = master;
        }

        void MagicMenu_SpellTriggered(Spell spell)
        {
            CurrentSpell = spell;
        }

        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            if (CurrentSpell != null && (CurrentSpell.Mode == Spell.SpellMode.SelectFilledVoxels || CurrentSpell.Mode == Spell.SpellMode.SelectEmptyVoxels))
            {
                CurrentSpell.OnVoxelsSelected(MagicMenu.SpellTree.Tree, voxels);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (CurrentSpell != null && CurrentSpell.Mode == Spell.SpellMode.SelectEntities)
            {
                CurrentSpell.OnEntitiesSelected(MagicMenu.SpellTree.Tree, bodies);
            }
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            Player.BodySelector.Enabled = false;
            Player.VoxSelector.Enabled = false;
            
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                WorldManager.GUI.IsMouseVisible = false;
                Player.BodySelector.Enabled = false;
                return;
            }
            else
            {
                WorldManager.GUI.IsMouseVisible = true;
            }

            if (CurrentSpell != null)
            {
                CurrentSpell.Update(time, Player.VoxSelector, Player.BodySelector);
            }

            WorldManager.GUI.MouseMode = WorldManager.IsMouseOverGui ? GUISkin.MousePointer.Pointer : GUISkin.MousePointer.Magic;
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
