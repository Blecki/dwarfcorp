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
        private Widget LogoWidget;
        private Texture2D LogoTexture;

        public MainMenuState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
       
        }

        private Gui.Widget MakeMenuFrame(String Name)
        {
            LogoWidget = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(600 / GameSettings.Default.GuiScale, 348 / GameSettings.Default.GuiScale),
                Transparent = true,
                AutoLayout = Gui.AutoLayout.FloatTop
            });

            return GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(400, 280),
                Font = "font18-outline",
                Border = "basic",
                Background = new TileReference("sbasic", 0),
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
                Font = GameSettings.Default.GuiScale == 1 ? "font18-outline" : "font10",
                TextColor = Color.White.ToVector4(),
                HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4()
        });
        }

        public void MakeMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame(StringLibrary.GetString("main-menu-title"));

#if !DEMO
            string latestSave = SaveGame.GetLatestSaveFile();

            if (latestSave != null)
            {
                MakeMenuItem(frame, 
                    StringLibrary.GetString("continue"),
                    StringLibrary.GetString("continue-tooltip", latestSave), 
                    (sender, args) =>
                    StateManager.PushState(new LoadState(Game, Game.StateManager, new WorldGenerationSettings()
                    {
                        ExistingFile = latestSave
                    })));
            } 
#endif
            /*
            MakeMenuItem(frame, 
                StringLibrary.GetString("new-game"), 
                StringLibrary.GetString("new-game-tooltip"), 
                (sender, args) => StateManager.PushState(new LoadState(Game, Game.StateManager, new WorldGenerationSettings() {GenerateFromScratch = true})));
            */
            MakeMenuItem(frame, 
                StringLibrary.GetString("new-game"), 
                StringLibrary.GetString("new-game-tooltip"),
#if !DEMO
                (sender, args) => StateManager.PushState(new CompanyMakerState(Game, Game.StateManager)));
#else
                (sender, args) => this.GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm() { CancelText = "", Text = StringLibrary.GetString("advanced-world-creation-denied") }));
#endif
            MakeMenuItem(frame, 
                StringLibrary.GetString("load-game"),
                StringLibrary.GetString("load-game-tooltip"),
#if !DEMO
                (sender, args) => StateManager.PushState(new LoadSaveGameState(Game, StateManager)));
#else
                (sender, args) => this.GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm() { CancelText = "", Text = StringLibrary.GetString("save-load-denied") }));
#endif

            MakeMenuItem(frame, 
                StringLibrary.GetString("options"),
                StringLibrary.GetString("options-tooltip"),
                (sender, args) => StateManager.PushState(new OptionsState(Game, StateManager)));

            MakeMenuItem(frame,
                StringLibrary.GetString("manage-mods"),
                StringLibrary.GetString("manage-mods-tooltip"), 
                (sender, args) => StateManager.PushState(new ModManagement.ManageModsState(Game, StateManager)));

            MakeMenuItem(frame, 
                StringLibrary.GetString("credits"),
                StringLibrary.GetString("credits-tooltip"),
                (sender, args) => StateManager.PushState(new CreditsState(GameState.Game, StateManager)));

#if DEBUG
            MakeMenuItem(frame, "GUI Debug", "Open the GUI debug screen.",
                (sender, args) =>
                {
                    StateManager.PushState(new GuiDebugState(GameState.Game, StateManager));
                });

            MakeMenuItem(frame, "Dwarf Designer", "Open the dwarf designer.",
                (sender, args) =>
                {
                    StateManager.PushState(new DwarfDesignerState(GameState.Game, StateManager));
                });

            /*
            MakeMenuItem(frame, "Yarn test", "", (sender, args) =>
            {
                StateManager.PushState(new YarnState(null, "test.conv", "Start", new Yarn.MemoryVariableStore()));
            });
            */

            MakeMenuItem(frame, "Trailer script", "", (sender, args) =>
            {
                StateManager.PushState(new YarnState(null, "trailer.conv", "Start", new Yarn.MemoryVariableStore()) { TestBackground = Color.Blue });
            });
#endif

            MakeMenuItem(frame, 
                StringLibrary.GetString("quit"),
                StringLibrary.GetString("quit-tooltip"),
                (sender, args) => Game.Exit());

            GuiRoot.RootItem.AddChild(new Widget()
            {
                Font = "font8",
                TextColor = new Vector4(1, 1, 1, 0.5f),
                AutoLayout = AutoLayout.FloatBottomRight,
#if DEMO
                Text = "DwarfCorp " + Program.Version + " (DEMO)  "
#else
                Text = "DwarfCorp " + Program.Version + " (" + Program.Commit + ")"
#endif
            });

            GuiRoot.RootItem.Layout();
        }

        public override void OnEnter()
        {
            // Make sure that this memory gets cleaned up!!
            EntityFactory.Cleanup();
            Drawer3D.Cleanup();
            ParticleEmitter.Cleanup();
            Overworld.Cleanup();
            ResourceLibrary.Cleanup();
            CraftLibrary.Cleanup();
            VoxelLibrary.Cleanup();
            PlayState.Input = null;
            InputManager.Cleanup();
            LayeredSprites.LayerLibrary.Cleanup();
            
            LogoTexture = AssetManager.GetContentTexture("newgui/gamelogo");
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
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

            GuiRoot.Update(gameTime.ToRealTime());
            SoundManager.Update(gameTime, null, null);
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            if (LogoTexture.IsDisposed)
            {
                LogoTexture = AssetManager.GetContentTexture("newgui/gamelogo");
            }
            GuiRoot.DrawMesh(
                        Gui.Mesh.Quad()
                        .Scale(LogoWidget.Rect.Width, LogoWidget.Rect.Height)
                        .Translate(LogoWidget.Rect.X,LogoWidget.Rect.Y),
                        LogoTexture);
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}
