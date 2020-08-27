using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;

namespace DwarfCorp.GameStates
{
    public class EmbarkmentEditor : Widget
    {
        private Overworld Settings;
        private Gui.Widgets.WidgetListView EmployeeList;
        private EmbarkmentResourceColumns ResourceColumns;
        private Widget EmployeeCost;
        private Widget TotalCost;
        private Widget Cash;
        private Widget ValidationLabel;

        public EmbarkmentEditor(Overworld Settings) 
        {
            this.Settings = Settings;
        }

        private DwarfBux FundsAvailable => Settings.PlayerCorporationFunds - Settings.InstanceSettings.CalculateLandValue();

        private Widget CreateEmployeeListing(Applicant Applicant, int Index)
        {
            var bar = Root.ConstructWidget(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 32)
            });

            bar.AddChild(new Widget
            {
                Text = "Remove",
                ChangeColorOnHover = true,
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(16, 0),
                TextVerticalAlign = VerticalAlign.Center,
                OnClick = (sender, args) =>
                {
                    Settings.InstanceSettings.InitalEmbarkment.Employees.RemoveAt(Index);
                    RebuildEmployeeList();
                    UpdateCost();
                }
            });

            bar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Text = String.Format("{0} {1} - {2}", Applicant.SigningBonus, Applicant.Name, Applicant.Loadout.HasValue(out var loadout) ? loadout.Name : "Dunno")
            });

            return bar;
        }

        private void RebuildEmployeeList()
        {
            EmployeeList.ClearItems();

            for (var i = 0; i < Settings.InstanceSettings.InitalEmbarkment.Employees.Count; ++i)
                EmployeeList.AddItem(CreateEmployeeListing(Settings.InstanceSettings.InitalEmbarkment.Employees[i], i));
        }

        private Widget CreateBar(String Label)
        {
            var r = Root.ConstructWidget(new Widget
            {
                MinimumSize = new Point(0, 32),
                Text = Label,
                AutoLayout = AutoLayout.DockTop,
                Font = "font16"
            });
            return r;
        }

        private void UpdateCost()
        {
            Cash.Text = Settings.InstanceSettings.InitalEmbarkment.Funds.ToString();
            Cash.Invalidate();

            EmployeeCost.Text = (new DwarfBux(Settings.InstanceSettings.InitalEmbarkment.Employees.Sum(e => e.SigningBonus))).ToString();
            EmployeeCost.Invalidate();

            TotalCost.Text = (Settings.InstanceSettings.InitalEmbarkment.Funds + Settings.InstanceSettings.InitalEmbarkment.Employees.Sum(e => e.SigningBonus)).ToString();
            TotalCost.Invalidate();

            var s = "";
            InstanceSettings.ValidateEmbarkment(Settings, out s);
            ValidationLabel.Text = s;
            ValidationLabel.Invalidate();
        }

        public override void Construct()
        {
            PopupDestructionType = PopupDestructionType.Keep;
            Padding = new Margin(2, 2, 2, 2);
            //Set size and center on screen.
            Rect = Root.RenderData.VirtualScreen;

            Border = "border-fancy";

            ValidationLabel = AddChild(new Widget
            {
                MinimumSize = new Point(0, 48),
                AutoLayout = AutoLayout.DockBottom,
                Font = "font16"
            });

            ValidationLabel.AddChild(new Gui.Widget
            {
                Text = "Okay",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    foreach (var res in ResourceColumns.SelectedResources.SelectMany(i => i.Resources))
                        Settings.InstanceSettings.InitalEmbarkment.Resources.Add(res);

                    var message = "";
                    var valid = Embarkment.ValidateEmbarkment(Settings, out message);
                    if (valid == InstanceSettings.ValidationResult.Pass)
                        this.Close();
                    else if (valid == InstanceSettings.ValidationResult.Query)
                    {
                        var popup = new Gui.Widgets.Confirm()
                        {
                            Text = message,
                            OnClose = (_sender) =>
                            {
                                if ((_sender as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                                    this.Close();
                            }
                        };
                        Root.ShowModalPopup(popup);
                    }
                    else if (valid == InstanceSettings.ValidationResult.Reject)
                    {
                        var popup = new Gui.Widgets.Confirm()
                        {
                            Text = message,
                            CancelText = ""
                        };
                        Root.ShowModalPopup(popup);
                    }
                }
            });

            var columns = AddChild(new Gui.Widgets.Columns
            {
                ColumnCount = 2,
                AutoLayout = AutoLayout.DockFill
            });

            var employeeStack = columns.AddChild(new Widget
            {
                MinimumSize = new Point(256, 0)
            });

            employeeStack.AddChild(new Widget
            {
                Text = "Employees",
                Font = "font16",
                AutoLayout = AutoLayout.DockTop
            });

            employeeStack.AddChild(new Widget
            {
                Text = "+ new employee",
                Font = "font16",
                ChangeColorOnHover = true,
                AutoLayout = AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    var dialog = Root.ConstructWidget(
                        new ChooseEmployeeTypeDialog()
                        {
                            Settings = Settings,
                            OnClose = (_s) =>
                            {
                                RebuildEmployeeList();
                                UpdateCost();
                            }
                        });
                    Root.ShowModalPopup(dialog);
                }
            });

            var costPanel = employeeStack.AddChild(new Widget
            {
                MinimumSize = new Point(0, 128),
                AutoLayout = AutoLayout.DockBottom
            });

            var availableFunds = costPanel.AddChild(CreateBar("Funds Available:"));
            availableFunds.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockRight,
                Text = FundsAvailable.ToString(),
                MinimumSize = new Point(128, 0),
                TextHorizontalAlign = HorizontalAlign.Right
            });

            var moneyBar = costPanel.AddChild(CreateBar("Ready Cash:"));

            Cash = moneyBar.AddChild(new Gui.Widgets.MoneyEditor
            {
                MaximumValue = (int)FundsAvailable,
                MinimumSize = new Point(128, 33),
                AutoLayout = AutoLayout.DockRight,
                OnValueChanged = (sender) =>
                {
                    Settings.InstanceSettings.InitalEmbarkment.Funds = (sender as Gui.Widgets.MoneyEditor).CurrentValue;
                    UpdateCost();
                },
                Tooltip = "Money to take.",
                TextHorizontalAlign = HorizontalAlign.Right
            }) as Gui.Widgets.MoneyEditor;

            var employeeCostBar = costPanel.AddChild(CreateBar("Signing Bonuses:"));
            EmployeeCost = employeeCostBar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockRight,
                Text = "$0",
                MinimumSize = new Point(128, 0),
                TextHorizontalAlign = HorizontalAlign.Right
            });

            var totalBar = costPanel.AddChild(CreateBar("Total Cost:"));
            TotalCost = totalBar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockRight,
                Text = "$0",
                MinimumSize = new Point(128, 0),
                TextHorizontalAlign = HorizontalAlign.Right
            });

            EmployeeList = employeeStack.AddChild(new Gui.Widgets.WidgetListView
            {
                AutoLayout = Gui.AutoLayout.DockFill,
                SelectedItemForegroundColor = new Vector4(0,0,0,1)
                
            }) as Gui.Widgets.WidgetListView;

            RebuildEmployeeList();

            ResourceColumns = columns.AddChild(new EmbarkmentResourceColumns
            {
                SourceResources = Play.Trading.Helper.AggregateResourcesIntoTradeableItems(Settings.PlayerCorporationResources),
                SelectedResources = Play.Trading.Helper.AggregateResourcesIntoTradeableItems(Settings.InstanceSettings.InitalEmbarkment.Resources),
                LeftHeader = "Available",
                RightHeader = "Taking"
            }) as EmbarkmentResourceColumns;

            Layout();
            UpdateCost();
            base.Construct();
        }
    }
}