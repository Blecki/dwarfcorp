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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state is just the set of menus at the start of the game. Allows navigation to other game states.
    /// </summary>
    public class MainMenuState : GameState
    {
        public Texture2D Logo { get; set; }
        public bool IsGameRunning { get; set; }
        public bool MaintainState { get; set; }

        private Gum.Root GuiRoot;


        public MainMenuState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
            ResourceLibrary library = new ResourceLibrary();
            Embarkment.Initialize();
            VoxelChunk.InitializeStatics();
            IsGameRunning = false;
            MaintainState = false;
        }

        private Gum.Widget MakeMenuFrame(String Name)
        {
            GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(600, 348),
                Background = new Gum.TileReference("logo", 0),
                AutoLayout = Gum.AutoLayout.FloatTop
            });

            return GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(256, 200),
                Border = "border-fancy",
                AutoLayout = Gum.AutoLayout.FloatTop,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                Text = Name,
                TextSize = 2,
                TopMargin = 16,
                OnLayout = (sender) => { sender.Rect.Y = 350; }
            });
        }

        public void MakeDebugWorldMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("DEBUG WORLDS");
            MakeMenuItem(frame, "Hills", "Create a hilly world.", (sender, args) =>
                {
                    MaintainState = false;
                    Overworld.CreateHillsLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    //GUI.MouseMode = GUISkin.MousePointer.Wait;

                    IsGameRunning = true;
                });

            MakeMenuItem(frame, "Cliffs", "Create a cliff-y world.", (sender, args) =>
                {
                    MaintainState = false;
                    Overworld.CreateCliffsLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    //GUI.MouseMode = GUISkin.MousePointer.Wait;
                    PlayState.Natives = new List<Faction>();
                    FactionLibrary library = new FactionLibrary();
                    library.Initialize(null, "fake", "fake", null, Color.Blue);
                    for (int i = 0; i < 10; i++)
                    {
                        PlayState.Natives.Add(library.GenerateFaction(i, 10));
                    }

                    IsGameRunning = true;
                });

            MakeMenuItem(frame, "Flat", "Create a flat world.", (sender, args) =>
                {
                    MaintainState = false;
                    Overworld.CreateUniformLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    //GUI.MouseMode = GUISkin.MousePointer.Wait;

                    IsGameRunning = true;
                });

            MakeMenuItem(frame, "Ocean", "Create an ocean world", (sender, args) =>
                {
                    MaintainState = false;
                    Overworld.CreateOceanLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    //GUI.MouseMode = GUISkin.MousePointer.Wait;

                    IsGameRunning = true;
                });

            MakeMenuItem(frame, "Back", "Go back to the main menu.", (sender, args) => MakeDefaultMenu());

            GuiRoot.RootItem.Layout();
        }

        public void MakeDefaultMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("MAIN MENU");

            if (IsGameRunning)
                MakeMenuItem(frame, "Continue", "Return to your game.", (sender, args) =>
                    StateManager.PopState());

            MakeMenuItem(frame, "New Game", "Start a new game of DwarfCorp.", (sender, args) =>
                {
                    MakePlayMenu();
                    StateManager.PushState("CompanyMakerState");
                    MaintainState = true;
                });

            MakeMenuItem(frame, "Load Game", "Load DwarfCorp game from a file.", (sender, args) =>
                {
                    MaintainState = true;
                    StateManager.PushState("GameLoaderState");
                });

            MakeMenuItem(frame, "Options", "Change game settings.", (sender, args) =>
                {
                    MaintainState = true;
                    StateManager.PushState("OptionsState");
                });

            MakeMenuItem(frame, "New Options", "Change game settings.", (sender, args) =>
            {
                MaintainState = true;
                StateManager.PushState("NewOptionsState");
            });

            MakeMenuItem(frame, "Credits", "View the credits.", (sender, args) =>
                {
                    if (StateManager.States.ContainsKey("CreditsState"))
                        StateManager.PushState("CreditsState");
                    else
                        StateManager.PushState(new CreditsState(GameState.Game, "CreditsState", StateManager));
                });

            MakeMenuItem(frame, "Quit", "Goodbye.", (sender, args) => Game.Exit());

            GuiRoot.RootItem.Layout();
        }

        private void MakeMenuItem(Gum.Widget Menu, string Name, string Tooltip, Action<Gum.Widget, Gum.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Border = "border-thin",
                TextSize = 2,
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                TextVerticalAlign = Gum.VerticalAlign.Center
            });
        }

        public void MakePlayMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("PLAY DWARFCORP");

            MakeMenuItem(frame, "Generate World", "Create a new world from scratch.", (sender, args) =>
                {
                    MaintainState = true;
                    StateManager.PushState("WorldGeneratorState");
                });

            MakeMenuItem(frame, "Load World", "Load a continent from an existing file.", (sender, args) =>
                {
                    MaintainState = true;
                    StateManager.PushState("WorldLoaderState");
                });

            MakeMenuItem(frame, "Debug World", "Create a debug world.", (sender, args) => MakeDebugWorldMenu());

            MakeMenuItem(frame, "Back", "Go back to main menu.", (sender, args) => MakeDefaultMenu());

            GuiRoot.RootItem.Layout();
        }


        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInput.GetInputQueue();

            if (!MaintainState)
            {
                GuiRoot = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
                GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

                MakeDefaultMenu();

                // Must be true or Render will not be called.
                IsInitialized = true;
            }

            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInput.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());

            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            GuiRoot.Draw(new Point((int)dx, 0));
        }

        public override void Render(DwarfTime gameTime)
        {

            if(Transitioning == TransitionMode.Running)
            {
                DrawGUI(gameTime, 0);
            }
            else if(Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if(Transitioning == TransitionMode.Exiting)
            {
                // Doesn't actually hide GUI during world gen... just draws it off screen. WTF!
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }

            base.Render(gameTime);
        }
    }

}