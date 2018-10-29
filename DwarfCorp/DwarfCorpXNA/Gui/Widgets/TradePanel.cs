using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using DwarfCorp.Trade;

namespace DwarfCorp.Gui.Widgets
{
    public enum TradeDialogResult
    {
        Pending,
        Cancel,
        Propose,
        RejectProfit,
        RejectOffense
    }

    public class Balance : Widget
    {
        private float _balance = 0.0f;
        public float TradeBalance
        {
            get { return _balance; }
            set { _balance = value; Update(); }
        }

        public Widget LeftWidget;
        public Widget RightWidget;
        public Widget LeftHook;
        public Widget RightHook;
        //public Widget CenterWidget;
        public Widget Bar;
        public Widget LeftItems;
        public Widget RightItems;

        private IEnumerable<ResourceAmount> GetTopResources(List<ResourceAmount> resources, int num = 3)
        {
            var copy = new List<ResourceAmount>();
            copy.AddRange(resources);

            copy.Sort((a, b) => a.NumResources.CompareTo(b.NumResources));
            for (int i = 0; i < Math.Min(copy.Count, num); i++)
            {
                if (copy[i].NumResources > 0)
                    yield return copy[i];
            }
        }

        public void SetTradeItems(List<ResourceAmount> leftResources, List<ResourceAmount> rightResources, DwarfBux leftMoney, DwarfBux rightMoney)
        {
            Update();
            LeftItems.Clear();
            RightItems.Clear();

            var left = GetTopResources(leftResources).ToList();
            int leftCount = left.Count + (leftMoney > 0.0m ? 1 : 0);

            int k = 0;
            foreach (var resource in left)
            {
                var resourceType = ResourceLibrary.GetResourceByName(resource.ResourceType);
                LeftItems.AddChild(new ResourceIcon()
                {
                    Layers = resourceType.GuiLayers,
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Rect = new Rectangle(LeftWidget.Rect.X + 16 + k * 4 - leftCount * 2, LeftWidget.Rect.Y + 5, 32, 32)
                });
                k++;
            }

            if (leftMoney > 0.0m)
            {
                LeftItems.AddChild(new Widget()
                {
                    Background = new TileReference("coins", 1),
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Rect = new Rectangle(LeftWidget.Rect.X + 16 + k * 4 - leftCount * 2, LeftWidget.Rect.Y + 5, 32, 32)
                });
            }

            var right = GetTopResources(rightResources).ToList();
            int rightCount = right.Count + (rightMoney > 0.0m ? 1 : 0);
            k = 0;
            foreach (var resource in GetTopResources(rightResources))
            {
                var resourceType = ResourceLibrary.GetResourceByName(resource.ResourceType);
                RightItems.AddChild(new ResourceIcon()
                {
                    Layers = resourceType.GuiLayers,
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Rect = new Rectangle(RightWidget.Rect.X + 16 + k * 4 - rightCount * 2, RightWidget.Rect.Y + 5, 32, 32)
                });
                k++;
            }

            if (rightMoney > 0.0m)
            {
                RightItems.AddChild(new Widget()
                {
                    Background = new TileReference("coins", 1),
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Rect = new Rectangle(RightWidget.Rect.X + 16 + k * 4 - rightCount * 2, RightWidget.Rect.Y + 5, 32, 32)
                });
            }
            LeftItems.Invalidate();
            RightItems.Invalidate();
        }

        public void Update()
        {
            var rect = Rect;
            //CenterWidget.Rect = new Rectangle(rect.Center.X - 16, rect.Top, 32, 32);
            Bar.Rect = new Rectangle(rect.Center.X - 32, rect.Top - 32, 64, 48);
            Bar.Rotation = -_balance * 0.5f;
            Bar.Invalidate();
            float dy = 32 * (float)Math.Sin(_balance * 0.5f);
            LeftWidget.Rect = new Rectangle(rect.Center.X - 28 - 32, rect.Top - 12 + (int)dy, 64, 48);
            LeftHook.Rect = new Rectangle(LeftWidget.Rect.X, LeftWidget.Rect.Y - 3, LeftWidget.Rect.Width, LeftWidget.Rect.Height);
            LeftWidget.Invalidate();
            LeftHook.Invalidate();
            RightWidget.Rect = new Rectangle(rect.Center.X + 28 - 32, rect.Top - 12 - (int)dy, 64, 48);
            RightHook.Rect = new Rectangle(RightWidget.Rect.X, RightWidget.Rect.Y - 3, RightWidget.Rect.Width, RightWidget.Rect.Height);
            RightWidget.Invalidate();
            RightHook.Invalidate();
            Layout();
        }

        public override void Construct()
        {
            LeftWidget = AddChild(new Widget()
            {
                Background = new TileReference("balance", 0),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            LeftHook = LeftWidget.AddChild(new Widget()
            {
                Background = new TileReference("balance", 1),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            LeftItems = LeftWidget.AddChild(new Widget()
            {
                MaximumSize = new Point(32, 32),
                MinimumSize = new Point(32, 32)
            });
            RightWidget = AddChild(new Widget()
            {
                Background = new TileReference("balance", 0),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            RightHook = RightWidget.AddChild(new Widget()
            {
                Background = new TileReference("balance", 2),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48)
            });
            RightItems = RightWidget.AddChild(new Widget()
            {
                MinimumSize = new Point(32, 32),
                MaximumSize =  new Point(32, 32)
            });
            /*
            CenterWidget = AddChild(new Widget()
            {
                Background = new TileReference("balance", 1),
                MaximumSize = new Point(32, 32),
                MinimumSize = new Point(32, 32)
            });*/
            Bar = AddChild(new Widget()
            {
                Background = new TileReference("balance", 3),
                MaximumSize = new Point(64, 48),
                MinimumSize = new Point(64, 48),
                Tag = "trade_balance"
        });
            Update();
            base.Construct();
        }
    }
    
    public class TradePanel : Widget
    {
        public ITradeEntity Player;
        public ITradeEntity Envoy;
        private ResourceColumns PlayerColumns;
        private ResourceColumns EnvoyColumns;
        private Widget TotalDisplay;
        private Widget SpaceDisplay;
        public Action<Widget> OnPlayerAction;

        public TradeDialogResult Result { get; private set; }
        public TradeTransaction Transaction { get; private set; }

        public Balance Balance { get; set; }

        public void Reset()
        {
            Result = TradeDialogResult.Pending;
            Transaction = null;
            EnvoyColumns.Reconstruct(Envoy.Resources, new List<ResourceAmount>(), (int)Envoy.Money);
            PlayerColumns.Reconstruct(Player.Resources, new List<ResourceAmount>(), (int)Player.Money);
            UpdateBottomDisplays();
            Balance.TradeBalance = 0.0f;
            Layout();
        }

        private DwarfBux ComputeNetValue(List<ResourceAmount> playerResources, DwarfBux playerTradeMoney,
            List<ResourceAmount> envoyResources, DwarfBux envoyMoney)
        {
            return (Envoy.ComputeValue(playerResources) + playerTradeMoney) - (Envoy.ComputeValue(envoyResources) + envoyMoney);   
        }

        private DwarfBux ComputeNetValue()
        {
            return ComputeNetValue(PlayerColumns.SelectedResources,
                PlayerColumns.TradeMoney, EnvoyColumns.SelectedResources, EnvoyColumns.TradeMoney);
        }

        private void MoveRandomValue(IEnumerable<ResourceAmount> source, List<ResourceAmount> destination,
            ITradeEntity trader)
        {
            foreach (var amount in source)
            {
                Resource r = ResourceLibrary.GetResourceByName(amount.ResourceType);
                if (trader.TraderRace.HatedResources.Any(tag => r.Tags.Contains(tag)))
                {
                    continue;
                }
                if (amount.NumResources == 0) continue;
                ResourceAmount destAmount =
                    destination.FirstOrDefault(resource => resource.ResourceType == amount.ResourceType);
                if (destAmount == null)
                {
                    destAmount = new ResourceAmount(amount.ResourceType, 0);
                    destination.Add(destAmount);
                }

                int numToMove = MathFunctions.RandInt(1, amount.NumResources + 1);
                amount.NumResources -= numToMove;
                destAmount.NumResources += numToMove;
                break;
            }
        }

        private bool IsReasonableTrade(DwarfBux envoyOut, DwarfBux net)
        {
            var tradeMin = envoyOut*0.25;
            var tradeMax = envoyOut*3.0;
            return net >= tradeMin && net <= tradeMax && Math.Abs(net) > 1;
        }

        private void EqualizeColumns()
        {
            if (EnvoyColumns.Valid && PlayerColumns.Valid)
            {
                var net = ComputeNetValue();
                var envoyOut = Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney;
                var tradeTarget = 1.0m;

                if (IsReasonableTrade(envoyOut, net))
                {
                    Root.ShowTooltip(Root.MousePosition, "This works fine.");
                    return;
                }

                List<ResourceAmount> sourceResourcesEnvoy = EnvoyColumns.SourceResources;
                List<ResourceAmount> selectedResourcesEnvoy = EnvoyColumns.SelectedResources;
                DwarfBux selectedMoneyEnvoy = EnvoyColumns.TradeMoney;
                DwarfBux remainingMoneyEnvoy = Envoy.Money - selectedMoneyEnvoy;
                List<ResourceAmount> sourceResourcesPlayer = PlayerColumns.SourceResources;
                List<ResourceAmount> selectedResourcesPlayer = PlayerColumns.SelectedResources;
                DwarfBux selectedMoneyPlayer = PlayerColumns.TradeMoney;
                DwarfBux remainingMoneyPlayer = Player.Money - selectedMoneyPlayer;

                int maxIters = 1000;
                int iter = 0;
                while (!IsReasonableTrade(envoyOut, net) && iter < maxIters)
                {
                    float t = MathFunctions.Rand();
                    if (envoyOut > net)
                    {
                        if (t < 0.05f && selectedMoneyEnvoy > 1)
                        {
                            DwarfBux movement = Math.Min((decimal) MathFunctions.RandInt(1, 5), selectedMoneyEnvoy);
                            selectedMoneyEnvoy -= movement;
                            remainingMoneyEnvoy += movement;
                        }
                        else if (t < 0.1f)
                        {
                            MoveRandomValue(selectedResourcesEnvoy, sourceResourcesEnvoy, Player);
                        }
                        else if (t < 0.15f && remainingMoneyPlayer > 1)
                        {
                            DwarfBux movement = Math.Min((decimal) MathFunctions.RandInt(1, Math.Max((int)(envoyOut - net), 2)), remainingMoneyPlayer);
                            selectedMoneyPlayer += movement;
                            remainingMoneyPlayer -= movement;
                        }
                        else
                         {
                            MoveRandomValue(sourceResourcesPlayer, selectedResourcesPlayer, Envoy);
                        }
                    }
                    else
                    {
                        if (t < 0.05f && selectedMoneyPlayer > 1)
                        {
                            DwarfBux movement = Math.Min((decimal)MathFunctions.RandInt(1, 5), selectedMoneyPlayer);
                            selectedMoneyPlayer -= movement;
                            remainingMoneyPlayer += movement;
                        }
                        else if (t < 0.1f)
                        {
                            MoveRandomValue(selectedResourcesPlayer, sourceResourcesPlayer, Envoy);
                        }
                        else if (t < 0.15f && remainingMoneyEnvoy > 1)
                        {
                            DwarfBux movement = Math.Min((decimal)MathFunctions.RandInt(1, Math.Max((int)(net - envoyOut), 2)), remainingMoneyEnvoy);
                            selectedMoneyEnvoy += movement;
                            remainingMoneyEnvoy -= movement;
                        }
                        else
                        {
                            MoveRandomValue(selectedResourcesEnvoy, sourceResourcesEnvoy, Player);
                        }
                    }
                    envoyOut = Envoy.ComputeValue(selectedResourcesEnvoy) + selectedMoneyEnvoy;
                    tradeTarget = envoyOut * 0.25;
                    net = ComputeNetValue(selectedResourcesPlayer, selectedMoneyPlayer, selectedResourcesEnvoy,
                        selectedMoneyEnvoy);
                }

                if (IsReasonableTrade(envoyOut, net))
                {
                    Root.ShowTooltip(Root.MousePosition, "How does this work?");
                    PlayerColumns.Reconstruct(sourceResourcesPlayer, selectedResourcesPlayer, (int)selectedMoneyPlayer);
                    PlayerColumns.TradeMoney = (int) selectedMoneyPlayer;
                    EnvoyColumns.Reconstruct(sourceResourcesEnvoy, selectedResourcesEnvoy, (int)selectedMoneyEnvoy);
                    EnvoyColumns.TradeMoney = (int) selectedMoneyEnvoy;
                    Layout();
                    return;
                }
                else
                {
                    Root.ShowTooltip(Root.MousePosition, "We don't see how this could work.");
                    return;
                }
                
            }
        }

        public override void Construct()
        {
            Transaction = null;
            Result = TradeDialogResult.Pending;

            Border = "border-fancy";

            Balance = AddChild(new Balance()
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(32 * 3, 64),
            })as Balance;
            var bottomRow = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });


            bottomRow.AddChild(new Gui.Widgets.Button
            {
                Font = "font10",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "Propose Trade",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    if (EnvoyColumns.Valid && PlayerColumns.Valid)
                    {
                        var net = ComputeNetValue();
                        var envoyOut = Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney;
                        var tradeTarget = 1.0m;

                        if (PlayerColumns.SelectedResources.Count == 0 && EnvoyColumns.SelectedResources.Count == 0
                            && EnvoyColumns.TradeMoney == 0 && PlayerColumns.TradeMoney == 0)
                        {
                            Root.ShowTooltip(Root.MousePosition, "You've selected nothing to trade.");
                        }
                        else if (net >= tradeTarget)
                        {
                            Result = TradeDialogResult.Propose;
                            Transaction = new TradeTransaction
                            {
                                EnvoyEntity = Envoy,
                                EnvoyItems = EnvoyColumns.SelectedResources,
                                EnvoyMoney = EnvoyColumns.TradeMoney,
                                PlayerEntity = Player,
                                PlayerItems = PlayerColumns.SelectedResources,
                                PlayerMoney = PlayerColumns.TradeMoney
                            };
                            Root.SafeCall(OnPlayerAction, this);
                        }
                        else
                        {
                            Result = TradeDialogResult.RejectProfit;
                            Root.SafeCall(OnPlayerAction, this);
                        }
                    }
                    else
                    {
                        Root.ShowTooltip(Root.MousePosition, "Trade is invalid");
                    }
                }
            });

            var autoButton = bottomRow.AddChild(new Gui.Widgets.Button
            {
                Font = "font10",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "Make this work",
                Tooltip = "What will make this work?",
                AutoLayout = AutoLayout.DockRight,
                OnUpdate = (sender, gameTime) =>
                {
                    DwarfBux net, tradeTarget;
                    CalculateTradeAmount(out net, out tradeTarget);
                    (sender as Gui.Widgets.Button).Enabled = net < tradeTarget;
                    sender.Invalidate();
                },
                OnClick = (sender, args) =>
                {
                    if ((sender as Gui.Widgets.Button).Enabled)
                        EqualizeColumns();
                }
            });
            Root.RegisterForUpdate(autoButton);

            bottomRow.AddChild(new Gui.Widgets.Button
            {
                Font = "font10",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "Clear",
                Tooltip = "Clear trade",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    EnvoyColumns.Reconstruct(Envoy.Resources, new List<ResourceAmount>(), (int)Envoy.Money);
                    PlayerColumns.Reconstruct(Player.Resources, new List<ResourceAmount>(), (int)Player.Money);
                    UpdateBottomDisplays();
                    Layout();
                }
            });

            bottomRow.AddChild(new Gui.Widgets.Button
            {
                Font = "font10",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "Stop",
                Tooltip = "Stop trading.",
                AutoLayout = AutoLayout.DockRight,
                //OnLayout = (sender) => sender.Rect.X -= 16,
                OnClick = (sender, args) =>
                {
                    Result = TradeDialogResult.Cancel;
                    Root.SafeCall(OnPlayerAction, this);
                    //this.Close();
                }                    
            });


            TotalDisplay = Balance.AddChild(new Widget
            {
                MinimumSize = new Point(128, 64),
                AutoLayout = AutoLayout.DockBottom,
                Font = "font10",
                TextColor = new Vector4(0, 0, 0, 1),
                TextVerticalAlign = VerticalAlign.Bottom,
                TextHorizontalAlign = HorizontalAlign.Center
            });

            SpaceDisplay = bottomRow.AddChild(new Widget
            {
                //MinimumSize = new Point(128, 0),
                AutoLayout = AutoLayout.DockFill,
                Font = "font10",
                TextColor = new Vector4(0, 0, 0, 1),
                TextVerticalAlign = VerticalAlign.Center,
                MaximumSize = new Point(256, 32),
                Tag = "trade_stocks"
            });

            var mainPanel = AddChild(new Columns
            {
                AutoLayout = AutoLayout.DockFill
            });


            PlayerColumns = mainPanel.AddChild(new ResourceColumns
            {
                TradeEntity = Player,
                ValueSourceEntity = Envoy,
                AutoLayout = AutoLayout.DockFill,
                ReverseColumnOrder = true,
                LeftHeader = "Our Items",
                RightHeader = "We Offer",
                MoneyLabel = "Our money",
                OnTotalSelectedChanged = (s) => UpdateBottomDisplays(),
                Tag = "trade_money"
            }) as ResourceColumns;

            EnvoyColumns = mainPanel.AddChild(new ResourceColumns
            {
                TradeEntity = Envoy,
                ValueSourceEntity = Envoy,
                AutoLayout = AutoLayout.DockFill,
                LeftHeader = "Their Items",
                RightHeader = "They Offer",
                MoneyLabel = "Their money",
                OnTotalSelectedChanged = (s) => UpdateBottomDisplays()
            }) as ResourceColumns;

            UpdateBottomDisplays();
        }        

        private void UpdateBottomDisplays()
        {
            DwarfBux net, tradeTarget;
            CalculateTradeAmount(out net, out tradeTarget);
            if (net == 0)
            {
                Balance.TradeBalance = 0.0f;
            }
            else
            {
                Balance.TradeBalance = net > 0 ? Math.Min(0.01f * (float)(decimal)net, 1.0f) : Math.Max(0.01f * (float)(decimal)net, -1.0f);
            }
            Balance.SetTradeItems(PlayerColumns.SelectedResources, EnvoyColumns.SelectedResources, PlayerColumns.TradeMoney, EnvoyColumns.TradeMoney);
            TotalDisplay.Text = String.Format("Their {1} {0}", net, net >= 0 ? "Profit" : "Loss");
            TotalDisplay.Tooltip = String.Format("They are {1} with this trade.\nTheir {0} is " + net + ".\nThey need at least " + tradeTarget + " to be happy.", net >= 0 ? "profit" : "loss",
                net >= 0 ? "happy" : "unhappy");
            if (net >= tradeTarget)
                TotalDisplay.TextColor = GameSettings.Default.Colors.GetColor("Positive", GameSettings.Default.Colors.GetColor("Positive", Color.Green)).ToVector4();
            else
                TotalDisplay.TextColor = GameSettings.Default.Colors.GetColor("Negative", GameSettings.Default.Colors.GetColor("Negative", Color.Red)).ToVector4();

            TotalDisplay.Invalidate();

            SpaceDisplay.Text = String.Format("Stockpile space used: {0}/{1}",
                Math.Max(EnvoyColumns.TotalSelectedItems - PlayerColumns.TotalSelectedItems, 0),
                Player.AvailableSpace);

            if (EnvoyColumns.TotalSelectedItems - PlayerColumns.TotalSelectedItems > Player.AvailableSpace)
            {
                SpaceDisplay.TextColor = GameSettings.Default.Colors.GetColor("Negative", Color.Red).ToVector4();
            }
            else
            {
                SpaceDisplay.TextColor = Color.Black.ToVector4();
            }

            SpaceDisplay.Tooltip = "We need this much space to make this trade.";
            SpaceDisplay.Invalidate();
        }

        private void CalculateTradeAmount(out DwarfBux net, out DwarfBux tradeTarget)
        {
            net = (Envoy.ComputeValue(PlayerColumns.SelectedResources) + PlayerColumns.TradeMoney)
                - (Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney);
            var envoyOut = Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney;
            tradeTarget = 1.0m;
        }
    }
}
