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
    public enum MarketDialogResult
    {
        Pending,
        Cancel,
        Propose,
    }

    public class MarketPanel : Widget
    {
        public ITradeEntity Player;
        private ResourceColumns PlayerColumns;
        private Widget TotalDisplay;
        public Action<Widget> OnPlayerAction;

        public MarketDialogResult Result { get; private set; }
        public MarketTransaction Transaction { get; private set; }

        public void Reset()
        {
            Result = MarketDialogResult.Pending;
            Transaction = null;
            PlayerColumns.Reconstruct(Player.Resources, new List<ResourceAmount>(), (int)Player.Money);
            UpdateBottomDisplays();
            Layout();
        }

        private DwarfBux ComputeNetValue()
        {
            return Player.ComputeValue(PlayerColumns.SelectedResources) * 0.25f;
        }

        public override void Construct()
        {
            Transaction = null;
            Result = MarketDialogResult.Pending;

            Border = "border-fancy";

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
                    if (PlayerColumns.Valid)
                    {
                        if (PlayerColumns.SelectedResources.Count == 0)
                            Root.ShowModalMessage( "You've selected nothing to trade.");
                        else
                        {
                            Result = MarketDialogResult.Propose;
                            Transaction = new MarketTransaction
                            {
                                PlayerEntity = Player,
                                PlayerItems = PlayerColumns.SelectedResources,
                                PlayerMoney = ComputeNetValue()
                            };

                            Root.SafeCall(OnPlayerAction, this);
                        }
                    }
                    else
                        Root.ShowTooltip(Root.MousePosition, "Trade is invalid");
                }
            });

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
                    Result = MarketDialogResult.Cancel;
                    Root.SafeCall(OnPlayerAction, this);
                    //this.Close();
                }                    
            });

            TotalDisplay = bottomRow.AddChild(new Widget
            {
                MinimumSize = new Point(256, 64),
                AutoLayout = AutoLayout.DockFill,
                Font = "font10",
                TextColor = new Vector4(0, 0, 0, 1),
                TextVerticalAlign = VerticalAlign.Bottom,
                TextHorizontalAlign = HorizontalAlign.Left,
                Text = "Total Profit: $0"
            });

            var mainPanel = AddChild(new Columns
            {
                AutoLayout = AutoLayout.DockFill
            });

            PlayerColumns = mainPanel.AddChild(new ResourceColumns
            {
                TradeEntity = Player,
                ValueSourceEntity = Player,
                AutoLayout = AutoLayout.DockFill,
                ReverseColumnOrder = true,
                LeftHeader = "Our Items",
                RightHeader = "We Offer",
                MoneyLabel = "Our money",
                OnTotalSelectedChanged = (s) => UpdateBottomDisplays(),
                Tag = "trade_money",
                ShowMoneyField = false
            }) as ResourceColumns;

            UpdateBottomDisplays();
        }        

        private void UpdateBottomDisplays()
        {
            TotalDisplay.Text = String.Format("Total Profit: {0}", ComputeNetValue());
            TotalDisplay.Invalidate();
        }
    }
}
