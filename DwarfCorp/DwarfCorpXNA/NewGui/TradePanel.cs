using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;
using DwarfCorp.Trade;

namespace DwarfCorp.NewGui
{
    public enum TradeDialogResult
    {
        Pending,
        Cancel,
        Propose
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
                Font = "font",
                TextColor = new Vector4(0, 0, 0, 1),
                TextVerticalAlign = VerticalAlign.Center
            });

            SpaceDisplay = bottomRow.AddChild(new Widget
            {
                MinimumSize = new Point(128, 0),
                AutoLayout = AutoLayout.DockLeft,
                Font = "font",
                TextColor = new Vector4(0, 0, 0, 1),
                TextVerticalAlign = VerticalAlign.Center
            });

            bottomRow.AddChild(new Gum.Widgets.Button
            {
                Font = "font",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "Propose Trade",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    if (EnvoyColumns.Valid && PlayerColumns.Valid)
                    {
                        var net =
                            (Envoy.ComputeValue(PlayerColumns.SelectedResources)
                            + PlayerColumns.TradeMoney)
                            -(Envoy.ComputeValue(EnvoyColumns.SelectedResources) 
                            + EnvoyColumns.TradeMoney);
                        var player = Envoy.ComputeValue(PlayerColumns.SelectedResources) + PlayerColumns.TradeMoney;
                        var tradeTarget = player * 0.25;

                        if (net > tradeTarget)
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
                            Root.ShowTooltip(Root.MousePosition, "Make us a better offer.");
                        }
                    }
                    else
                    {
                        Root.ShowTooltip(Root.MousePosition, "Trade is invalid");
                    }
                }
            });

            bottomRow.AddChild(new Gum.Widgets.Button
            {
                Font = "font",
                Border = "border-button",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = "Cancel",
                AutoLayout = AutoLayout.DockRight,
                Padding = new Margin(0,0,0,16),
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
            var net = (Envoy.ComputeValue(PlayerColumns.SelectedResources) + PlayerColumns.TradeMoney)
                - (Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney);
            var player = Envoy.ComputeValue(PlayerColumns.SelectedResources) + PlayerColumns.TradeMoney;
            var tradeTarget = player * 0.25;
            TotalDisplay.Text = String.Format("{0} [{1}]", net, tradeTarget);
            TotalDisplay.Text = String.Format("Their Profit {0} [{1}]", net, tradeTarget);

            if (net > tradeTarget)
                TotalDisplay.TextColor = new Vector4(0, 0, 0, 1);
            else
                TotalDisplay.TextColor = new Vector4(1, 0, 0, 1);

            TotalDisplay.Invalidate();

            SpaceDisplay.Text = String.Format("Stockpile space: {0}/{1}",
                EnvoyColumns.TotalSelectedItems - PlayerColumns.TotalSelectedItems,
                Player.AvailableSpace);
            SpaceDisplay.Invalidate();
        }
    }
}
