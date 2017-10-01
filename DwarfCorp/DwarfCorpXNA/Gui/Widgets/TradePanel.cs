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
        Reject
    }

    public class TradePanel : Widget
    {
        public ITradeEntity Player;
        public ITradeEntity Envoy;
        private ResourceColumns PlayerColumns;
        private ResourceColumns EnvoyColumns;
        private Widget TotalDisplay;
        private Widget SpaceDisplay;

        public TradeDialogResult Result { get; private set; }
        public TradeTransaction Transaction { get; private set; }


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
                var tradeTarget = envoyOut*0.25;

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

            var bottomRow = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });

            TotalDisplay = bottomRow.AddChild(new Widget
            {
                MinimumSize = new Point(128, 0),
                AutoLayout = AutoLayout.DockLeft,
                Font = "font10",
                TextColor = new Vector4(0, 0, 0, 1),
                TextVerticalAlign = VerticalAlign.Center
            });

            SpaceDisplay = bottomRow.AddChild(new Widget
            {
                MinimumSize = new Point(128, 0),
                AutoLayout = AutoLayout.DockLeft,
                Font = "font10",
                TextColor = new Vector4(0, 0, 0, 1),
                TextVerticalAlign = VerticalAlign.Center
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
                        var tradeTarget = envoyOut * 0.25;

                        if (PlayerColumns.SelectedResources.Count == 0 && EnvoyColumns.SelectedResources.Count == 0
                            && EnvoyColumns.TradeMoney == 0 && PlayerColumns.TradeMoney == 0)
                        {
                            Result = TradeDialogResult.Cancel;
                            this.Close();
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
                            this.Close();
                        }
                        else
                        {
                            Result = TradeDialogResult.Reject;
                            this.Close();
                        }
                    }
                    else
                    {
                        Root.ShowTooltip(Root.MousePosition, "Trade is invalid");
                    }
                }
            });

            bottomRow.AddChild(new Gui.Widgets.Button
            {
                Font = "font10",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "What will make this work?",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) => EqualizeColumns()
            });

            bottomRow.AddChild(new Gui.Widgets.Button
            {
                Font = "font10",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "Cancel",
                AutoLayout = AutoLayout.DockRight,
                OnLayout = (sender) => sender.Rect.X -= 16,
                OnClick = (sender, args) =>
                {
                    Result = TradeDialogResult.Cancel;
                    this.Close();
                }                    
            });

            var mainPanel = AddChild(new TwoColumns
            {
                AutoLayout = AutoLayout.DockFill
            });

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

            PlayerColumns = mainPanel.AddChild(new ResourceColumns
            {
                TradeEntity = Player,
                ValueSourceEntity = Envoy,
                AutoLayout = AutoLayout.DockFill,
                ReverseColumnOrder = true,
                LeftHeader = "Our Items",
                RightHeader = "We Offer",
                MoneyLabel = "Our money",
                OnTotalSelectedChanged = (s) => UpdateBottomDisplays()
            }) as ResourceColumns;

            UpdateBottomDisplays();
        }        

        private void UpdateBottomDisplays()
        {
            // Todo: Satisfactory trade threshold calculated in two different spots.
            var net = (Envoy.ComputeValue(PlayerColumns.SelectedResources) + PlayerColumns.TradeMoney)
                - (Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney);
            var envoyOut = Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney;
            var tradeTarget = envoyOut * 0.25;
            TotalDisplay.Text = String.Format("{0} [{1}]", net, tradeTarget);
            TotalDisplay.Text = String.Format("Their {2} {0}\n[need {1}]", net, tradeTarget, net >= 0 ? "Profit" : "Loss");
            TotalDisplay.Tooltip = String.Format("They are {1} with this trade.\nTheir {0} is " + net + ".\nThey need at least " + tradeTarget + " to be happy.", net >= 0 ? "profit" : "loss",
                net >= 0 ? "happy" : "unhappy");
            if (net >= tradeTarget)
                TotalDisplay.TextColor = new Vector4(0, 0, 0, 1);
            else
                TotalDisplay.TextColor = Color.DarkRed.ToVector4();

            TotalDisplay.Invalidate();

            SpaceDisplay.Text = String.Format("Stockpile space used: {0}/{1}",
                Math.Max(EnvoyColumns.TotalSelectedItems - PlayerColumns.TotalSelectedItems, 0),
                Player.AvailableSpace);

            if (EnvoyColumns.TotalSelectedItems - PlayerColumns.TotalSelectedItems > Player.AvailableSpace)
            {
                SpaceDisplay.TextColor = Color.DarkRed.ToVector4();
            }
            else
            {
                SpaceDisplay.TextColor = Color.Black.ToVector4();
            }

            SpaceDisplay.Tooltip = "We need this much space to make this trade.";
            SpaceDisplay.Invalidate();
        }
    }
}
