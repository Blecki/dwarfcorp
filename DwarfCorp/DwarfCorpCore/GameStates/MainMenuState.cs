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
        public DwarfGUI GUI { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public ListSelector ListSelect { get; set; }
        public Drawer2D Drawer { get; set; }
        public InputManager Input { get; set; }
        public bool IsGameRunning { get; set; }
        public bool MaintainState { get; set; }


        public MainMenuState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
            ResourceLibrary library = new ResourceLibrary();
            Embarkment.Initialize();
            VoxelChunk.InitializeStatics();
            IsGameRunning = false;
            MaintainState = false;
        }

        public void DebugWorldItems()
        {
            ListSelect.ClearItems();
            ListSelect.AddItem("Hills World", "Create a hilly (Debug) world.");
            ListSelect.AddItem("Cliffs World", "Create a cliff-y (Debug) world.");
            ListSelect.AddItem("Flat World", "Create a flat (Debug) world.");
            ListSelect.AddItem("Ocean World", "Create an ocean (Debug) world.");
            ListSelect.AddItem("Back", "Back to the Main Menu");
        }

        public void DefaultItems()
        {
            ListSelect.ClearItems();

            if(IsGameRunning)
            {
                ListSelect.AddItem("Continue Game", "Keep playing DwarfCorp");
            }

            ListSelect.AddItem("New Game", "Start a new game of DwarfCorp.");
            ListSelect.AddItem("Load Game", "Load DwarfCorp game from a file.");
            ListSelect.AddItem("Options", "Change game settings.");
            ListSelect.AddItem("Credits", "View the credits");
            ListSelect.AddItem("Quit", "Exit the game.");
        }

        public void PlayItems()
        {
            ListSelect.ClearItems();
            ListSelect.AddItem("Generate World", "Create a new world from scratch!");
            ListSelect.AddItem("Load World", "Load a continent from an existing file!");
            ListSelect.AddItem("Debug World", "Create a debug world");
            ListSelect.AddItem("Back", "Back to the Main Menu");
        }

        public void OnItemClicked(ListItem item)
        {
            switch (item.Label)
            {
                case "Continue Game":
                    StateManager.PopState();
                    break;
                case "New Game":
                    PlayItems();
                    StateManager.PushState("CompanyMakerState");
                    MaintainState = true;
                    break;
                case "Credits":
                    if (StateManager.States.ContainsKey("CreditsState"))
                    {
                        StateManager.PushState("CreditsState");
                    }
                    else
                        StateManager.PushState(new CreditsState(GameState.Game, "CreditsState", StateManager));
                    break;
                case "Quit":
                    Game.Exit();
                    break;
                case "Generate World":
                    MaintainState = true;
                    StateManager.PushState("WorldGeneratorState");
                    break;
                case "Options":
                    MaintainState = true;
                    StateManager.PushState("OptionsState");
                    break;
                case "Back":
                    DefaultItems();
                    break;
                case "Debug World":
                    DebugWorldItems();
                    break;
                case "Flat World":
                {
                    MaintainState = false;
                    Overworld.CreateUniformLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    GUI.MouseMode = GUISkin.MousePointer.Wait;
            
                    IsGameRunning = true;
                }
                    break;
                case "Hills World":
                {
                    MaintainState = false;
                    Overworld.CreateHillsLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    GUI.MouseMode = GUISkin.MousePointer.Wait;
            
                    IsGameRunning = true;
                }
                    break;
                case "Cliffs World":
                {
                    MaintainState = false;
                    Overworld.CreateCliffsLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    GUI.MouseMode = GUISkin.MousePointer.Wait;
                    PlayState.Natives = new List<Faction>();
                    FactionLibrary library = new FactionLibrary();
                    library.Initialize(null, "fake", "fake", null, Color.Blue);
                    for (int i = 0; i < 10; i++)
                    {
                        PlayState.Natives.Add(library.GenerateFaction(i, 10));
                    }

                    IsGameRunning = true;
                }
                    break;
                case "Ocean World":
                {
                    MaintainState = false;
                    Overworld.CreateOceanLand(Game.GraphicsDevice);
                    StateManager.PushState("PlayState");
                    PlayState.WorldSize = new Point3(8, 1, 8);
                    GUI.MouseMode = GUISkin.MousePointer.Wait;
            
                    IsGameRunning = true;
                }
                    break;
                case "Load World":
                    MaintainState = true;
                    StateManager.PushState("WorldLoaderState");
                    break;
                case "Load Game":
                    MaintainState = true;
                    StateManager.PushState("GameLoaderState");
                    break;
            }
        }

        public override void OnEnter()
        {
            if (!MaintainState)
            {
                DefaultFont = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
                GUI = new DwarfGUI(Game, DefaultFont,
                    Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default),
                    Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
                IsInitialized = true;
                Logo = TextureManager.GetTexture(ContentPaths.Logos.gamelogo);
                GUIComponent mainComponent = new GUIComponent(GUI, GUI.RootComponent)
                {
                    LocalBounds = new Rectangle(0, 0, Logo.Width, Logo.Height + 180)
                };

                AlignLayout layout = new AlignLayout(GUI, GUI.RootComponent)
                {
                    HeightSizeMode = GUIComponent.SizeMode.Fit,
                    WidthSizeMode = GUIComponent.SizeMode.Fit,
                    Mode = AlignLayout.PositionMode.Percent
                };

                ListSelect = new ListSelector(GUI, mainComponent)
                {
                    Label = "- Main Menu -",
                    LocalBounds =
                        new Rectangle(Logo.Width/2 - 150/2, Logo.Height + 20,
                            150, 150)
                };


                ImagePanel logoPanel = new ImagePanel(GUI, mainComponent, Logo)
                {
                    KeepAspectRatio = true,
                    ConstrainSize = true,
                    LocalBounds = new Rectangle(0, 0, Logo.Width, Logo.Height)
                };

                layout.Add(mainComponent, AlignLayout.Alignment.Center, AlignLayout.Alignment.Top, Vector2.Zero);
                DefaultItems();

                ListSelect.OnItemClicked += ItemClicked;
                Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
                Input = new InputManager();
            }

            base.OnEnter();
        }

        public void ItemClicked()
        {
            ListItem selectedItem = ListSelect.SelectedItem;
            OnItemClicked(selectedItem);
        }

        public override void Update(DwarfTime gameTime)
        {
            Input.Update();
            GUI.Update(gameTime);
            GUI.IsMouseVisible = true;

            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, null, rasterizerState);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            DwarfGame.SpriteBatch.DrawString(GUI.DefaultFont, Program.Version, new Vector2(15, 15), Color.White);
            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();
            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
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
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }

            base.Render(gameTime);
        }
    }

}