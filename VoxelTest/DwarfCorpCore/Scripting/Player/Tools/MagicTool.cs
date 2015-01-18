using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorpCore;
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

            MagicMenu = new MagicMenu(PlayState.GUI, PlayState.GUI.RootComponent, Player)
            {
                LocalBounds = new Rectangle(PlayState.Game.GraphicsDevice.Viewport.Width - 750, PlayState.Game.GraphicsDevice.Viewport.Height - 512, 700, 350),
            };
            MagicMenu.SpellTriggered += MagicMenu_SpellTriggered;

            MagicMenu.IsVisible = true;
            MagicMenu.LocalBounds = new Rectangle(GameState.Game.GraphicsDevice.Viewport.Width - 750,
                GameState.Game.GraphicsDevice.Viewport.Height - 512, 700, 350);

            if (MagicBar == null)
            {
                MagicBar = new ProgressBar(PlayState.GUI, PlayState.GUI.RootComponent,
                    MagicMenu.Master.Spells.Mana/MagicMenu.Master.Spells.MaxMana)
                {
                    ToolTip = "Remaining Mana Pool",
                    LocalBounds = new Rectangle(GameState.Game.GraphicsDevice.Viewport.Width - 200, 10, 180, 32),
                    Tint = Color.Cyan
                };
                MagicBar.OnUpdate += MagicBar_OnUpdate;
            }

            MagicBar.IsVisible = true;
        }

        void MagicBar_OnUpdate()
        {
            MagicBar.Value = MagicMenu.Master.Spells.Mana/MagicMenu.Master.Spells.MaxMana;
            MagicBar.ToolTip = "Remaining Mana Pool " + (int)MagicMenu.Master.Spells.Mana;
            MagicBar.IsVisible = true;
        }

        public override void OnEnd()
        {
            MagicMenu.IsVisible = false;
            MagicBar.IsVisible = false;
            MagicMenu.Destroy();
            MagicMenu = null;
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
                PlayState.GUI.IsMouseVisible = false;
                Player.BodySelector.Enabled = false;
                return;
            }
            else
            {
                PlayState.GUI.IsMouseVisible = true;
            }

            if (CurrentSpell != null)
            {
                CurrentSpell.Update(time, Player.VoxSelector, Player.BodySelector);
            }

            PlayState.GUI.MouseMode = PlayState.GUI.IsMouseOver() ? GUISkin.MousePointer.Pointer : GUISkin.MousePointer.Magic;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            if (CurrentSpell != null)
            {
                CurrentSpell.Render(time);
            }
        }

    }
}
