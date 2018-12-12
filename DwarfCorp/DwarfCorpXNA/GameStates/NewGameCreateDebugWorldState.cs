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
    public class NewGameCreateDebugWorldState : GameState
    {
        private Gui.Root GuiRoot;
        private Widget LogoWidget;
        private Texture2D LogoTexture;

        public NewGameCreateDebugWorldState(DwarfGame game, GameStateManager stateManager) :
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
                MinimumSize = new Point(400, 200),
                Font = "font18-outline",
                Border = "basic",
                Background = new TileReference("basic", 0),
                BackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f),
                AutoLayout = Gui.AutoLayout.FloatBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gui.Margin(24, 0, 0, 0),
                Padding = new Gui.Margin(2, 2, 2, 2),
                TextColor = Color.White.ToVector4()
            });
        }

        public void MakeDebugWorldMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("SPECIAL WORLDS");
            MakeMenuItem(frame, "Hills", "Create a hilly world.", (sender, args) =>
                {
                    Overworld.CreateHillsLand(Game.GraphicsDevice);
                    StateManager.ClearState();
                    WorldGenerationSettings settings = new WorldGenerationSettings()
                    {
                        ExistingFile = null,
                        ColonySize = new Point3(8, 1, 8),
                        WorldScale =  2.0f,
                        WorldOrigin = new Vector2(Overworld.Map.GetLength(0)/2.0f,
                            Overworld.Map.GetLength(1)/2.0f)*0.5f,
                        SpawnRect = new Rectangle((int)(Overworld.Map.GetLength(0) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            (int)(Overworld.Map.GetLength(1) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            8 * VoxelConstants.ChunkSizeX, 8 * VoxelConstants.ChunkSizeX)
                    };
                    StateManager.PushState(new LoadState(Game, StateManager, settings));
                });

            MakeMenuItem(frame, "Cliffs", "Create a cliff-y world.", (sender, args) =>
                {
                    Overworld.CreateCliffsLand(Game.GraphicsDevice);
                    StateManager.ClearState();
                    WorldGenerationSettings settings = new WorldGenerationSettings()
                    {
                        ExistingFile = null,
                        ColonySize = new Point3(8, 1, 8),
                        WorldScale = 2.0f,
                        WorldOrigin = new Vector2(Overworld.Map.GetLength(0) / 2.0f,
                            Overworld.Map.GetLength(1) / 2.0f) * 0.5f,
                        SpawnRect = new Rectangle((int)(Overworld.Map.GetLength(0) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            (int)(Overworld.Map.GetLength(1) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            8 * VoxelConstants.ChunkSizeX, 8 * VoxelConstants.ChunkSizeX)
                    };
                    StateManager.PushState(new LoadState(Game, StateManager, settings));
                });

            MakeMenuItem(frame, "Flat", "Create a flat world.", (sender, args) =>
                {
                    Overworld.CreateUniformLand(Game.GraphicsDevice);
                    StateManager.ClearState();
                    WorldGenerationSettings settings = new WorldGenerationSettings()
                    {
                        ExistingFile = null,
                        ColonySize = new Point3(8, 1, 8),
                        WorldScale = 2.0f,
                        WorldOrigin = new Vector2(Overworld.Map.GetLength(0) / 2.0f,
                            Overworld.Map.GetLength(1) / 2.0f) * 0.5f,
                        SpawnRect = new Rectangle((int)(Overworld.Map.GetLength(0) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            (int)(Overworld.Map.GetLength(1) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            8 * VoxelConstants.ChunkSizeX, 8 * VoxelConstants.ChunkSizeX)
                    };
                    StateManager.PushState(new LoadState(Game, StateManager, settings));
                });

            MakeMenuItem(frame, "Ocean", "Create an ocean world", (sender, args) =>
                {
                    Overworld.CreateOceanLand(Game.GraphicsDevice, 0.17f);
                    StateManager.ClearState();
                    WorldGenerationSettings settings = new WorldGenerationSettings()
                    {
                        ExistingFile = null,
                        ColonySize = new Point3(8, 1, 8),
                        WorldScale = 2.0f,
                        WorldOrigin = new Vector2(Overworld.Map.GetLength(0) / 2.0f,
                            Overworld.Map.GetLength(1) / 2.0f) * 0.5f,
                        SpawnRect = new Rectangle((int)(Overworld.Map.GetLength(0) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            (int)(Overworld.Map.GetLength(1) / 2.0f - 8 * VoxelConstants.ChunkSizeX),
                            8 * VoxelConstants.ChunkSizeX, 8 * VoxelConstants.ChunkSizeX)
                    };
                    StateManager.PushState(new LoadState(Game, StateManager, settings));
                });

            MakeMenuItem(frame, "Back", "Go back to the main menu.", (sender, args) => StateManager.PopState());

            GuiRoot.RootItem.Layout();
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

        public override void OnEnter()
        {
            LogoTexture = AssetManager.GetContentTexture("newgui/gamelogo");

            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();
                GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
                GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
                MakeDebugWorldMenu();

                // Must be true or Render will not be called.
                IsInitialized = true;

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

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.DrawMesh(
               Gui.Mesh.Quad()
               .Scale(LogoWidget.Rect.Width, LogoWidget.Rect.Height)
               .Translate(LogoWidget.Rect.X, LogoWidget.Rect.Y),
               LogoTexture);

            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}
