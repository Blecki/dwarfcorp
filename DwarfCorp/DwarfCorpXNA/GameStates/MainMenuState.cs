// MainMenuState.cs
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
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state is just the set of menus at the start of the game. Allows navigation to other game states.
    /// </summary>
    public class MainMenuState : GameState
    {
        private Gui.Root GuiRoot;

        public MainMenuState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
        }

        private Gui.Widget MakeMenuFrame(String Name)
        {
            GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(600, 348),
                Background = new Gui.TileReference("logo", 0),
                AutoLayout = Gui.AutoLayout.FloatTop
            });

            return GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(400, 250),
                Font = "outline-font",
                Border = "basic",
                Background = new TileReference("basic", 0),
                BackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f),
                AutoLayout = Gui.AutoLayout.FloatBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gui.Margin(24,0,0,0),
                Padding = new Gui.Margin(2, 2, 2, 2),
                TextColor = Color.White.ToVector4()
            });
        }

        private void MakeMenuItem(Gui.Widget Menu, string Name, string Tooltip, Action<Gui.Widget, Gui.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gui.Widgets.Button
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Text = Name,
                Border = "none",
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "outline-font",
                TextColor = Color.White.ToVector4(),
                HoverTextColor = Color.Red.ToVector4()
            });
        }

        public void MakeMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("MAIN MENU");

            string latestSave = SaveGame.GetLatestSaveFile();

            if (latestSave != null)
            {
                MakeMenuItem(frame, "Continue", "Continue last save " + latestSave, (sender, args) =>
                    StateManager.PushState(new LoadState(Game, Game.StateManager, new WorldGenerationSettings()
                    {
                        ExistingFile = latestSave
                    })));
            }

            MakeMenuItem(frame, "New Game", "Start a new game of DwarfCorp.", (sender, args) => StateManager.PushState(new LoadState(Game, Game.StateManager, new WorldGenerationSettings() {GenerateFromScratch = true})));

            MakeMenuItem(frame, "New Game (Advanced)", "Start a new game of DwarfCorp.", (sender, args) => StateManager.PushState(new CompanyMakerState(Game, Game.StateManager)));

            MakeMenuItem(frame, "Load Game", "Load DwarfCorp game from a file.", (sender, args) => StateManager.PushState(new LoadSaveGameState(Game, StateManager)));

            MakeMenuItem(frame, "Options", "Change game settings.", (sender, args) => StateManager.PushState(new OptionsState(Game, StateManager)));

            MakeMenuItem(frame, "Credits", "View the credits.", (sender, args) => StateManager.PushState(new CreditsState(GameState.Game, StateManager)));

#if DEBUG
            MakeMenuItem(frame, "GUI Debug", "Open the GUI debug screen.",
                (sender, args) =>
                {
                    StateManager.PushState(new GuiDebugState(GameState.Game, StateManager));
                });
#endif

            MakeMenuItem(frame, "Quit", "Goodbye.", (sender, args) => Game.Exit());

            GuiRoot.RootItem.Layout();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            MakeMenu();
            IsInitialized = true;

            DwarfTime.LastTime.Speed = 1.0f;
            SoundManager.PlayMusic("menu_music");
            SoundManager.StopAmbience();
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());
            SoundManager.Update(gameTime, null, null);
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}
