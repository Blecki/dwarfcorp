// EconomyState.cs
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

    /// <summary>
    /// This game state allows the player to buy/sell goods from a balloon with a drag/drop interface.
    /// </summary>
    public class EconomyState : GameState
    {
        public DwarfGUI GUI { get; set; }
        public GUIComponent MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }
        public Texture2D Icons { get; set; }
        public List<Button> TabButtons { get; set; }
        public Dictionary<string, GUIComponent> Tabs { get; set; }
        public WorldManager World { get; set; }

        public EconomyState(DwarfGame game, GameStateManager stateManager, WorldManager world) :
            base(game, "EconomyState", stateManager)
        {
            World = world;
            EdgePadding = 32;
            Input = new InputManager();
            EnableScreensaver = false;
            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;
           
        }

        public static void PushEconomyState(WorldManager world)
        {
            if (Game.StateManager.NextState == null)
            {
                Game.StateManager.PushState(new EconomyState(Game, Game.StateManager, world));
            }
        }

        void Initialize()
        {
            Tabs = new Dictionary<string, GUIComponent>();
            Icons = TextureManager.GetTexture(ContentPaths.GUI.icons);
            TabButtons = new List<Button>();
            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input)
            {
                DebugDraw = false
            };
            IsInitialized = true;
            MainWindow = new GUIComponent(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2)
            };
            Layout = new GridLayout(GUI, MainWindow, 11, 4);
            Layout.UpdateSizes();

            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            Layout.SetComponentPosition(back, 0, 10, 1, 1);
            back.OnClicked += back_OnClicked;

            Panel tabPanel = new Panel(GUI, Layout);
            Layout.SetComponentPosition(tabPanel, 0, 0, 4, 1);

            GridLayout tabLayout = new GridLayout(GUI, tabPanel, 1, 10)
            {
                EdgePadding = 10
            };


           CreateTabButton(tabLayout, "Employees", "Hire and fire dwarves", 5, 0);
            EmployeeDisplay employeeDisplay = new EmployeeDisplay(GUI, Layout, World.Master.Faction, World.PlayerCompany.Information)
            {
                IsVisible = false
            };
            Tabs["Employees"] = employeeDisplay;
           

           CreateTabButton(tabLayout, "Capital", "Financial report", 2, 1);
           CapitalPanel capitalPanel = new CapitalPanel(GUI, Layout, World.Master.Faction)
           {
               IsVisible = false
           };
           Tabs["Capital"] = capitalPanel;

          ButtonClicked(TabButtons[0]);
        }

        public Button CreateTabButton(GridLayout parent, string name, string tooltip, int x, int y)
        {
            Button button = new Button(GUI, parent, name, GUI.SmallFont, Button.ButtonMode.ImageButton, new ImageFrame(Icons, 32, x, y))
            {
                CanToggle = true,
                IsToggled = false,
                KeepAspectRatio = true,
                DontMakeBigger = true,
                ToolTip = tooltip,
                TextColor = Color.Black,
                HoverTextColor = Color.DarkRed,
                DrawFrame = true
            };
            button.OnClicked += () => ButtonClicked(button);

            parent.SetComponentPosition(button, TabButtons.Count, 0, 1, 1);
            TabButtons.Add(button);

            
            
            return button;
        }

        public void SetTab(string tabName)
        {
            foreach(KeyValuePair<string, GUIComponent> tab in Tabs)
            {
                if(tabName == tab.Key)
                {

                    Layout.SetComponentPosition(tab.Value, 0, 2, 4, 7);
                    tab.Value.IsVisible = true;
                }
                else
                {
                    tab.Value.IsVisible = false;
                }
              
            }
        }

        public void ButtonClicked(Button button)
        {
            foreach(Button b in TabButtons)
            {
                if(button != b)
                {
                    b.IsToggled = false;
                }
                else
                {
                    SetTab(b.Text);
                }
            }
        }

        void InputManager_KeyReleasedCallback(Microsoft.Xna.Framework.Input.Keys key)
        {
            if(!IsActiveState)
            {
                return;
            }

            if(key == Keys.Escape)
            {
                back_OnClicked();
            }
        }


        public override void OnEnter()
        {
            Initialize();
            base.OnEnter();
        }

     
        private void back_OnClicked()
        {
            World.SetMouse(World.MousePointer);
            StateManager.PopState();
        }

        public override void Update(DwarfTime gameTime)
        {
            CompositeLibrary.Update();
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