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
        private OverworldGenerationSettings Settings;
        private Gui.Widgets.WidgetListView EmployeeList;
        private EmbarkmentResourceColumns ResourceColumns;

        public EmbarkmentEditor(OverworldGenerationSettings Settings) 
        {
            this.Settings = Settings;
        }

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
                    Settings.InitalEmbarkment.Employees.RemoveAt(Index);
                    RebuildEmployeeList();
                }
            });

            bar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Text = String.Format("{0} {1} - {2}", Applicant.SigningBonus, Applicant.Name, Applicant.Class.Name)
            });

            return bar;
        }

        private void RebuildEmployeeList()
        {
            EmployeeList.ClearItems();

            for (var i = 0; i < Settings.InitalEmbarkment.Employees.Count; ++i)
                EmployeeList.AddItem(CreateEmployeeListing(Settings.InitalEmbarkment.Employees[i], i));
        }

        public override void Construct()
        {
            PopupDestructionType = PopupDestructionType.Keep;
            Padding = new Margin(2, 2, 2, 2);
            //Set size and center on screen.
            Rect = Root.RenderData.VirtualScreen;

            Border = "border-fancy";

            var bottomBar = AddChild(new Widget
            {
                MinimumSize = new Point(0, 48),
                AutoLayout = AutoLayout.DockBottom
            });

            bottomBar.AddChild(new Gui.Widget
            {
                Text = "Okay",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    foreach (var resource in ResourceColumns.SelectedResources)
                        Settings.InitalEmbarkment.Resources.Add(resource);

                    this.Close();
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
                            }
                        });
                    Root.ShowModalPopup(dialog);
                }
            });

            var moneyField = employeeStack.AddChild(new Gui.Widgets.MoneyEditor
            {
                MaximumValue = (int)Settings.PlayerCorporationFunds,
                MinimumSize = new Point(0, 33),
                AutoLayout = AutoLayout.DockBottom,
                OnValueChanged = (sender) =>
                {
                    Settings.InitalEmbarkment.Money = (sender as Gui.Widgets.MoneyEditor).CurrentValue;
                },
                Tooltip = "Money to take."
            }) as Gui.Widgets.MoneyEditor;

            EmployeeList = employeeStack.AddChild(new Gui.Widgets.WidgetListView
            {
                AutoLayout = Gui.AutoLayout.DockFill,
                SelectedItemForegroundColor = new Vector4(0,0,0,1)
                
            }) as Gui.Widgets.WidgetListView;

            RebuildEmployeeList();

            ResourceColumns = columns.AddChild(new EmbarkmentResourceColumns
            {
                ComputeValue = (resourceType) =>
                {
                    var r = ResourceLibrary.GetResourceByName(resourceType);
                    return r.MoneyValue;
                },
                SourceResources = Settings.PlayerCorporationResources.Enumerate().ToList(),
                SelectedResources = Settings.InitalEmbarkment.Resources.Enumerate().ToList(),
                LeftHeader = "Available",
                RightHeader = "Taking"
            }) as EmbarkmentResourceColumns;

            // Todo: Capital display

            Layout();

            base.Construct();

        }
    }
}