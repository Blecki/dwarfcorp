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
                Text = "$0"
            });

            SpaceDisplay = bottomRow.AddChild(new Widget
            {
                MinimumSize = new Point(128, 0),
                AutoLayout = AutoLayout.DockLeft
            });

            bottomRow.AddChild(new Widget
            {
                Font = "outline-font",
                Border = "border-button",
                TextColor = new Vector4(1, 1, 1, 1),
                Text = "Propose Trade",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
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
            });

            bottomRow.AddChild(new Widget
            {
                Font = "outline-font",
                Border = "border-button",
                TextColor = new Vector4(1, 1, 1, 1),
                Text = "Cancel",
                AutoLayout = AutoLayout.DockRight,
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
                OnTotalSelectedChanged = (s) => UpdateBottomDisplays()
            }) as ResourceColumns;

            UpdateBottomDisplays();
        }        

        private void UpdateBottomDisplays()
        {
            var net = (Envoy.ComputeValue(EnvoyColumns.SelectedResources) + EnvoyColumns.TradeMoney) -
                (Envoy.ComputeValue(PlayerColumns.SelectedResources) + PlayerColumns.TradeMoney);
            TotalDisplay.Text = String.Format("${0}", net);
            TotalDisplay.Invalidate();

            SpaceDisplay.Text = String.Format("{0}/{1}",
                EnvoyColumns.TotalSelectedItems - PlayerColumns.TotalSelectedItems,
                Player.AvailableSpace);
            SpaceDisplay.Invalidate();
        }
    }
}
