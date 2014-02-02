using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// This game state allows the player to buy/sell goods from a balloon with a drag/drop interface.
    /// </summary>
    public class OrderState : GameState
    {
        public DwarfGUI GUI { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public Drawer2D Drawer { get; set; }
        public GUIComponent MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }
        public float CurrentMoney { get; set; }
        public float OrderTotal { get; set; }
        public DragGrid FromMotherland { get; set; }
        public DragGrid FromColony { get; set; }
        public DragGrid BallonGrid { get; set; }
        public Label CurrentMoneyLabel { get; set; }
        public Label OrderTotalLabel { get; set; }
        public PlayState PlayState { get; set; }
        public DragManager DragManager { get; set; }
        public Texture2D BalloonTexture { get; set; }
        public OrderMode Mode { get; set; }

        public enum OrderMode
        {
            Buying,
            Selling
        }

        public OrderState(DwarfGame game, GameStateManager stateManager, PlayState play) :
            base(game, "OrderState", stateManager)
        {
            EdgePadding = 32;
            Input = new InputManager();
            CurrentMoney = 0.0f;
            OrderTotal = 0.0f;
            PlayState = play;
            Mode = OrderMode.Buying;
            EnableScreensaver = true;
        }

        public List<GItem> GetResources(GameMaster master, float priceMultiplier)
        {
            Dictionary<Resource, int> counts = new Dictionary<Resource, int>();

            foreach(Stockpile stockpile in master.Faction.Stockpiles)
            {
                foreach(Item i in stockpile.ListItems())
                {
                    LocatableComponent userData = i.UserData;
                    Resource r = ResourceLibrary.Resources[userData.Tags[0]];

                    if(!counts.ContainsKey(r))
                    {
                        counts[r] = 1;
                    }
                    else
                    {
                        counts[r] = counts[r] + 1;
                    }
                }
            }

            return (from r in counts.Keys
                where r.ResourceName != "Container"
                select new GItem(r.ResourceName, r.Image, 0, 32, counts[r], r.MoneyValue * priceMultiplier, r.Tags)).ToList();
        }

        public List<GItem> GetResources(float priceMultiplier)
        {
            return (from r in ResourceLibrary.Resources.Values
                where r.ResourceName != "Container"
                select new GItem(r.ResourceName, r.Image, 0, 1000, 1000, r.MoneyValue * priceMultiplier, r.Tags)).ToList();
        }

        public override void OnEnter()
        {
            BalloonTexture = TextureManager.GetTexture(ContentPaths.Entities.Balloon.Sprites.balloon);
            DragManager = new DragManager();
            DefaultFont = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
            IsInitialized = true;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            MainWindow = new GUIComponent(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2)
            };
            Layout = new GridLayout(GUI, MainWindow, 10, 4);

            CurrentMoney = PlayState.Master.Faction.Economy.CurrentMoney;
            OrderTotal = 0.0f;


            if(Mode == OrderMode.Buying)
            {
                Label motherlandLabel = new Label(GUI, Layout, "From Motherland", GUI.TitleFont)
                {
                    TextColor = Color.White,
                    StrokeColor = new Color(0, 0, 0, 100)
                };
                Layout.SetComponentPosition(motherlandLabel, 3, 0, 1, 1);

                List<GItem> resources = GetResources(PlayState.Master.Faction.Economy.BuyMultiplier);
                Panel motherlandPanel = new Panel(GUI, Layout);
                GridLayout motherLayout = new GridLayout(GUI, motherlandPanel, 1, 1);
                FromMotherland = new DragGrid(GUI, motherLayout, DragManager, 64, 64)
                {
                    DrawBackground = false,
                    ToolTip = "Items from the Dwarven Motherland.\nDrag to the balloon to order them."
                };
                motherLayout.SetComponentPosition(FromMotherland, 0, 0, 1, 1);
                Layout.SetComponentPosition(motherlandPanel, 3, 1, 1, 7);
                Layout.UpdateSizes();
                motherLayout.UpdateSizes();
                FromMotherland.SetupLayout();

                foreach(GItem item in resources)
                {
                    item.CurrentAmount = 99999;
                    FromMotherland.AddItem(item);
                }
            }

            Layout.UpdateSizes();
            GUIComponent fakePanel = new GUIComponent(GUI, Layout);
            Layout.SetComponentPosition(fakePanel, 1, 0, 2, 8);
            Layout.UpdateSizes();

            ImagePanel balloonPanel = new ImagePanel(GUI, GUI.RootComponent, BalloonTexture)
            {
                KeepAspectRatio = false,
                LocalBounds = new Rectangle(fakePanel.GlobalBounds.Center.X - BalloonTexture.Width, fakePanel.GlobalBounds.Center.Y - BalloonTexture.Height, BalloonTexture.Width * 2, BalloonTexture.Height * 2),
                ToolTip = "Items to be shipped."
            };
            balloonPanel.GlobalBounds = balloonPanel.LocalBounds;

            GridLayout balloonPanelLayout = new GridLayout(GUI, balloonPanel, 1, 1);

            BallonGrid = new DragGrid(GUI, balloonPanelLayout, DragManager, 64, 64);
            balloonPanelLayout.SetComponentPosition(BallonGrid, 0, 0, 1, 1);
            balloonPanelLayout.UpdateSizes();
            BallonGrid.SetupLayout();
            BallonGrid.DrawBackground = false;


            if(Mode == OrderMode.Selling)
            {
                Label colonyLabel = new Label(GUI, Layout, "From Colony", GUI.TitleFont)
                {
                    TextColor = Color.White,
                    StrokeColor = new Color(0, 0, 0, 100),
                    ToolTip = "Items from your colony.\n Drag to the balloon to sell them."
                };
                Layout.SetComponentPosition(colonyLabel, 0, 0, 1, 1);

                List<GItem> resources2 = GetResources(PlayState.Master, PlayState.Master.Faction.Economy.SellMultiplier);

                Panel colonyPanel = new Panel(GUI, Layout);
                GridLayout colonyLayout = new GridLayout(GUI, colonyPanel, 1, 1);
                FromColony = new DragGrid(GUI, colonyLayout, DragManager, 64, 64)
                {
                    DrawBackground = false
                };
                colonyLayout.SetComponentPosition(FromColony, 0, 0, 1, 1);
                Layout.SetComponentPosition(colonyPanel, 0, 1, 1, 7);
                Layout.UpdateSizes();
                colonyLayout.UpdateSizes();
                FromColony.SetupLayout();


                foreach(GItem item in resources2)
                {
                    FromColony.AddItem(item);
                }
            }


            Button orderButton = new Button(GUI, Layout, "Order!", GUI.DefaultFont, Button.ButtonMode.PushButton, null)
            {
                ToolTip = "Click to order buying/selling."
            };
            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.PushButton, null);

            BallonGrid.OnChanged += BallonGrid_OnChanged;

            Layout.SetComponentPosition(back, 2, 9, 1, 1);
            Layout.SetComponentPosition(orderButton, 3, 9, 1, 1);

            back.OnClicked += back_OnClicked;
            orderButton.OnClicked += orderButton_OnClicked;

            CurrentMoneyLabel = new Label(GUI, Layout, "Money: " + "$" + CurrentMoney, GUI.TitleFont)
            {
                ToolTip = "Total amount of money your corporation has."
            };
            Layout.SetComponentPosition(CurrentMoneyLabel, 0, 8, 1, 1);
            CurrentMoneyLabel.TextColor = Color.White;
            CurrentMoneyLabel.StrokeColor = new Color(0, 0, 0, 100);

            OrderTotalLabel = new Label(GUI, Layout, "Money: " + "$" + CurrentMoney, GUI.TitleFont)
            {
                ToolTip = "Total  transaction amount for this order."
            };

            Layout.SetComponentPosition(OrderTotalLabel, 0, 9, 1, 1);
            OrderTotalLabel.TextColor = Color.White;
            OrderTotalLabel.StrokeColor = new Color(0, 0, 0, 100);

            SetCurrentMoney(CurrentMoney);
            SetTransactionMoney(OrderTotal);

            DragManager.DisallowDragging(FromColony, FromColony);
            DragManager.DisallowDragging(FromColony, FromMotherland);
            DragManager.DisallowDragging(FromMotherland, FromMotherland);
            DragManager.DisallowDragging(FromMotherland, FromColony);


            base.OnEnter();
        }

        private void BallonGrid_OnChanged()
        {
            float money = 0;

            foreach(DraggableItem t in BallonGrid.Items)
            {
                if(Mode == OrderMode.Buying)
                {
                    money -= t.Item.CurrentAmount * t.Item.Price;
                }
                else
                {
                    money += t.Item.CurrentAmount * t.Item.Price;
                }
            }

            SetTransactionMoney(money);
        }


        private void orderButton_OnClicked()
        {
            if(!(Math.Abs(OrderTotal) > 0) || !(-OrderTotal < CurrentMoney))
            {
                if(Math.Abs(OrderTotal) > 0)
                {
                    Dialog.Popup(GUI, "Can't order!", "Can't order, not enough money!", Dialog.ButtonType.OK);
                }
                else
                {
                    Dialog.Popup(GUI, "Can't order!", "Nothing to buy/sell.", Dialog.ButtonType.OK);
                }
                return;
            }
            List<Room> ports = PlayState.Master.Faction.RoomDesignator.FilterRoomsByType("BalloonPort");

            if(ports.Count == 0)
            {
                Dialog.Popup(GUI, "Can't order!", "Can't order, no balloon ports.", Dialog.ButtonType.OK);
                return;
            }

            ShipmentOrder o = new ShipmentOrder(10.0f, ports[PlayState.Random.Next(0, ports.Count)])
            {
                OrderTotal = OrderTotal
            };

            if(Mode == OrderMode.Buying)
            {
                foreach(DraggableItem item in BallonGrid.Items)
                {
                    if(item.Item.CurrentAmount <= 0)
                    {
                        continue;
                    }

                    ResourceAmount r = new ResourceAmount
                    {
                        ResourceType = ResourceLibrary.Resources[item.Item.Name],
                        NumResources = item.Item.CurrentAmount
                    };
                    o.BuyOrder.Add(r);
                }
            }
            else
            {
                foreach(DraggableItem item in BallonGrid.Items)
                {
                    if(item.Item.CurrentAmount <= 0)
                    {
                        continue;
                    }

                    ResourceAmount r = new ResourceAmount
                    {
                        ResourceType = ResourceLibrary.Resources[item.Item.Name],
                        NumResources = item.Item.CurrentAmount
                    };
                    o.SellOrder.Add(r);
                }
            }

            PlayState.Master.Faction.Economy.OutstandingOrders.Add(o);
            PlayState.Paused = false;
            PlayState.GUI.RootComponent.IsVisible = true;
            StateManager.PopState();
        }


        public void SetCurrentMoney(float money)
        {
            CurrentMoney = money;
            CurrentMoneyLabel.Text = "Money: " + CurrentMoney.ToString("C");
            if(CurrentMoney < 0)
            {
                CurrentMoneyLabel.TextColor = Color.Red;
            }
            else
            {
                OrderTotalLabel.TextColor = Color.White;
                OrderTotalLabel.StrokeColor = new Color(0, 0, 0, 100);
            }
        }

        public void SetTransactionMoney(float money)
        {
            OrderTotal = money;
            OrderTotalLabel.Text = "Order Total: " + OrderTotal.ToString("C");
            if(OrderTotal < 0)
            {
                OrderTotalLabel.TextColor = Color.Red;
            }
            else
            {
                OrderTotalLabel.TextColor = Color.White;
                OrderTotalLabel.StrokeColor = new Color(0, 0, 0, 100);
            }
        }

        private void back_OnClicked()
        {
            PlayState.Paused = false;
            PlayState.GUI.RootComponent.IsVisible = true;
            StateManager.PopState();
        }

        public override void Update(GameTime gameTime)
        {
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Input.Update();
            GUI.Update(gameTime);
            base.Update(gameTime);
        }


        private void DrawGUI(GameTime gameTime, float dx)
        {
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            Drawer2D.FillRect(DwarfGame.SpriteBatch, Game.GraphicsDevice.Viewport.Bounds, new Color(0, 0, 0, 100));
            //spriteBatch.Draw(BalloonTexture, new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - BalloonTexture.Width / 2, Game.GraphicsDevice.Viewport.Height / 2 - BalloonTexture.Height / 2), null, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));


            DwarfGame.SpriteBatch.End();
        }

        public override void Render(GameTime gameTime)
        {
            if(Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
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