using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;
using System.Text;
using System;
using DwarfCorp.Scripting.Adventure;

namespace DwarfCorp.GameStates
{
    public class SelectEmployeesDialog : Widget
    {
        public WorldManager World;
        public Faction Faction;
        public List<CreatureAI> StayingCreatures = new List<CreatureAI>();
        public List<CreatureAI> GoingCreatures = new List<CreatureAI>();
        public WidgetListView LeftColumns = null;
        public WidgetListView RightColumns = null;
        public Action OnCanceled = null;
        public Action<SelectEmployeesDialog> OnProceed = null;

        public SelectEmployeesDialog()
        {
        }

        private void AddCreature(CreatureAI employee, WidgetListView column, List<CreatureAI> creaturesA, List<CreatureAI> creaturesB)
        {
            var bar = Root.ConstructWidget(new Widget
            {
                Background = new TileReference("basic", 0),
                TriggerOnChildClick = true,
                OnClick = (sender, args) =>
                {
                    creaturesA.Remove(employee);
                    creaturesB.Add(employee);
                    ReconstructColumns();
                }
            });
            var employeeSprite = employee.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();

            if (employeeSprite != null)
                bar.AddChild(new EmployeePortrait
                {
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(48, 40),
                    MaximumSize = new Point(48, 40),
                    Sprite = employeeSprite.GetLayers(),
                    AnimationPlayer = new AnimationPlayer(employeeSprite.GetAnimation("IdleFORWARD"))
                });

            var title = employee.Stats.Title ?? employee.Stats.CurrentLevel.Name;
            bar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                TextVerticalAlign = VerticalAlign.Center,
                MinimumSize = new Point(128, 64),
                Text = (employee.Stats.FullName) + " (" + title + ")"
            });

            column.AddItem(bar);
        }

        public void ReconstructColumns()
        {
            LeftColumns.ClearItems();
            RightColumns.ClearItems();
            foreach (var employee in StayingCreatures)
            {
                AddCreature(employee, LeftColumns, StayingCreatures, GoingCreatures);
            }

            foreach (var employee in GoingCreatures)
            {
                AddCreature(employee, RightColumns, GoingCreatures, StayingCreatures);
            }
        }

        public override void Construct()
        {
            Border = "border-one";
            Text = "Prepare for Expedition";
            Font = "font16";
            InteriorMargin = new Margin(32, 5, 5, 5);
            StayingCreatures.AddRange(Faction.Minions.Where(minion => !Faction.World.Diplomacy.Adventures.Any(adventure => adventure.Party.Contains(minion))));
            var rect = GetDrawableInterior();
            var leftSide = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(rect.Width / 2 - 28, rect.Height - 100),
            });
            leftSide.AddChild(new Widget()
            {
                Font= "font16",
                Text = "Staying",
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockTop
            });
            LeftColumns = leftSide.AddChild(new WidgetListView()
            {
                Font = "font10",
                ItemHeight = 40,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(rect.Width / 2 - 32, rect.Height - 132),
                InteriorMargin = new Margin(32, 5, 5, 5),
                ChangeColorOnSelected = false,
                Tooltip = "Click to select dwarves for the journey."
            }) as WidgetListView;


            var rightSide = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(rect.Width / 2 - 28, rect.Height - 100),
            });

            rightSide.AddChild(new Widget()
            {
                Font= "font16",
                Text = "Going",
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockTop
            });

            RightColumns = rightSide.AddChild(new WidgetListView()
            {
                Font = "font10",
                ItemHeight = 40,
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(rect.Width / 2 - 32, rect.Height - 132),
                InteriorMargin = new Margin(32, 5, 5, 5),
                ChangeColorOnSelected = false,
                Tooltip = "Click to leave dwarves at home."
            }) as WidgetListView;

            ReconstructColumns();

            leftSide.AddChild(new Button()
            {
                Text = "Back",
                Tooltip = "Go back to the factions view.",
                AutoLayout = AutoLayout.FloatBottomLeft,
                OnClick = (sender, args) =>
                {
                    if(OnCanceled != null) OnCanceled.Invoke();
                    Close();
                }
            });

            rightSide.AddChild(new Button()
            {
                Text = "Next",
                Tooltip = "Select resources for the expedition.",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    if (GoingCreatures.Count == 0)
                    {
                        Root.ShowModalPopup(new Gui.Widgets.Confirm()
                        {
                            Text = "Please select at least one employee for the expedition.",
                            CancelText = ""
                        });
                        return;
                    }
                    if (OnProceed != null) OnProceed.Invoke(this);
                    Close();
                }
            });

            Layout();
            base.Construct();
        }
    }
}