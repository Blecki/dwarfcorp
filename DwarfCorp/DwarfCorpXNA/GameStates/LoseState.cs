// LoseState.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public class LoseState : GameState
    {
        public DwarfGUI GUI { get; set; }
        public GUIComponent MainWindow { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }
        public PlayState PlayState { get; set; }
        public int EdgePadding = 55;

        public LoseState(DwarfGame game, GameStateManager stateManager, PlayState play) :
            base(game, "EconomyState", stateManager)
        {
            Input = new InputManager();
            PlayState = play;
            EnableScreensaver = false;
            
        }

        void Initialize()
        {
            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input)
            {
                DebugDraw = false
            };
            IsInitialized = true;
            MainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2)
            };
            Layout = new GridLayout(GUI, MainWindow, 11, 4);
            Label title = new Label(GUI, Layout, "You Lose!", GUI.TitleFont);
            Layout.SetComponentPosition(title, 0, 0, 4, 4);

            Label text = new Label(GUI, Layout,
                "The heady days of exploration for " + WorldManager.Master.Faction.Economy.Company.Name +
                " are no more.\n Our stock is through the floor. Our investors have all jumped ship! \n We are going to have to sell the company. If only we had shipped more goods...",
                GUI.DefaultFont)
            {
                WordWrap = true
            };

            Layout.SetComponentPosition(text, 0, 4, 4, 4);

            Button okButton = new Button(GUI, Layout, "OK", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            okButton.OnClicked += okButton_OnClicked;
            Layout.SetComponentPosition(okButton, 2, 10, 2, 1);
            Layout.UpdateSizes();
        }

        void okButton_OnClicked()
        {
            PlayState.QuitGame();
        }

      
        public override void OnEnter()
        {
            WorldManager.GUI.ToolTipManager.ToolTip = "";
            Initialize();
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Input.Update();
            GUI.Update(gameTime);
            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, rasterizerState);

            Drawer2D.FillRect(DwarfGame.SpriteBatch, Game.GraphicsDevice.Viewport.Bounds, new Color(0, 0, 0, 200));

            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            Drawer2D.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();
        }

        public override void Render(DwarfTime gameTime)
        {
            DrawGUI(gameTime, 0);
            base.Render(gameTime);
        }
    }

}