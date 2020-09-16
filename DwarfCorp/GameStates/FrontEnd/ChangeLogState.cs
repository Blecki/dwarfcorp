using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.IO;

namespace DwarfCorp.GameStates
{
    public class ChangeLogState : GameState
    {
        private bool LaunchedYarn = false;

        public ChangeLogState(DwarfGame game) :
            base(game)
        {
       
        }

        public override void OnEnter()
        {
            this.IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            if (LaunchedYarn)
            {
                GameStateManager.PopState();
                GameStateManager.PushState(new MainMenuState(Game));
            }
            else
            {
                LaunchedYarn = true;
                GameSettings.Current.LastVersionChangesDisplayed = Program.Version;
                GameSettings.Save();
                GameStateManager.PushState(new YarnState(null, "whats-new.conv", "Start", new Yarn.MemoryVariableStore()));
            }
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            base.Render(gameTime);
        }
    }

}
