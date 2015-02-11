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
        public Drawer2D Drawer { get; set; }
        public GUIComponent MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }
        public PlayState PlayState { get; set; }
        public Texture2D Icons { get; set; }
        public List<Button> TabButtons { get; set; }
        public Dictionary<string, GUIComponent> Tabs { get; set; } 
      

        public EconomyState(DwarfGame game, GameStateManager stateManager, PlayState play) :
            base(game, "EconomyState", stateManager)
        {
            
            EdgePadding = 32;
            Input = new InputManager();
            PlayState = play;
            EnableScreensaver = false;
            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;
           
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
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
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
            EmployeeDisplay employeeDisplay = new EmployeeDisplay(GUI, Layout, PlayState.Master.Faction)
            {
                IsVisible = false
            };
            Tabs["Employees"] = employeeDisplay;
           
           CreateTabButton(tabLayout, "Assets", "Buy/Sell goods", 3, 1);
           GoodsPanel assetsPanel = new GoodsPanel(GUI, Layout, PlayState.Master.Faction)
           {
               IsVisible = false
           };
           Tabs["Assets"] = assetsPanel;


           CreateTabButton(tabLayout, "Capital", "Financial report", 2, 1);
           CapitalPanel capitalPanel = new CapitalPanel(GUI, Layout, PlayState.Master.Faction)
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
            PlayState.GUI.ToolTipManager.ToolTip = "";
            Initialize();
            base.OnEnter();
        }

     
        private void back_OnClicked()
        {
            PlayState.GUI.RootComponent.IsVisible = true;
            StateManager.PopState();
        }

        public override void Update(DwarfTime DwarfTime)
        {
            CompositeLibrary.Update();
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Input.Update();
            GUI.Update(DwarfTime);
            base.Update(DwarfTime);
        }


        private void DrawGUI(DwarfTime DwarfTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            GUI.PreRender(DwarfTime, DwarfGame.SpriteBatch);

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState);

            Drawer2D.FillRect(DwarfGame.SpriteBatch, Game.GraphicsDevice.Viewport.Bounds, new Color(0, 0, 0, 200));
         
            GUI.Render(DwarfTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);   

            DwarfGame.SpriteBatch.End();
            GUI.PostRender(DwarfTime);
        }

        public override void Render(DwarfTime DwarfTime)
        {
            DrawGUI(DwarfTime, 0);
            base.Render(DwarfTime);
        }
    }

}