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
    public class NewGameCreateDebugWorldState : GameState
    {
        private Gum.Root GuiRoot;


        public NewGameCreateDebugWorldState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
        }

        private Gum.Widget MakeMenuFrame(String Name)
        {
            GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(600, 348),
                Background = new Gum.TileReference("logo", 0),
                AutoLayout = Gum.AutoLayout.FloatTop,
            });

            return GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(256, 200),
                Border = "border-fancy",
                AutoLayout = Gum.AutoLayout.FloatBottom,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gum.Margin(12,0,0,0),
                Padding = new Gum.Margin(2, 2, 2, 2)
            });
        }

        public void MakeDebugWorldMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("DEBUG WORLDS");
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
                            Overworld.Map.GetLength(1)/2.0f)*0.5f
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
                            Overworld.Map.GetLength(1) / 2.0f) * 0.5f
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
                            Overworld.Map.GetLength(1) / 2.0f) * 0.5f
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
                            Overworld.Map.GetLength(1) / 2.0f) * 0.5f
                    };
                    StateManager.PushState(new LoadState(Game, StateManager, settings));
                });

            MakeMenuItem(frame, "Back", "Go back to the main menu.", (sender, args) => StateManager.PopState());

            GuiRoot.RootItem.Layout();
        }


        private void MakeMenuItem(Gum.Widget Menu, string Name, string Tooltip, Action<Gum.Widget, Gum.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gum.Widgets.Button
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Border = "border-thin",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                TextVerticalAlign = Gum.VerticalAlign.Center
            });
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();
                GuiRoot = new Gum.Root(Gum.Root.MinimumSize, DwarfGame.GumSkin);
                GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

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

            GuiRoot.Update(gameTime.ToGameTime());

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}
