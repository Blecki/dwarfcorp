using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class EmployeeInfo : Widget
    {
        private CreatureAI _employee;
        public CreatureAI Employee
        {
            get { return _employee; }
            set { _employee = value;  Invalidate(); }
        }

        private Widget NameLabel;

        private Widget StatDexterity;
        private Widget StatStrength;
        private Widget StatWisdom;
        private Widget StatConstitution;
        private Widget StatIntelligence;
        private Widget StatSize;

        private Gum.Widgets.ProgressBar Hunger;
        private Gum.Widgets.ProgressBar Energy;
        private Gum.Widgets.ProgressBar Happiness;
        private Gum.Widgets.ProgressBar Health;

        private Widget LevelLabel;
        private Widget TitleLabel;
        private Widget ExperienceLabel;
        private Widget PayLabel;
        private Widget LevelButton;

        public Action<Widget> OnFireClicked;

        public override void Construct()
        {
            NameLabel = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            var columns = AddChild(new NewGui.TwoColumns
            {
                AutoLayout = AutoLayout.DockFill
            });
            
            var left = columns.AddChild(new Gum.Widget());
            var right = columns.AddChild(new Gum.Widget());

            #region Stats
            var statParent = left.AddChild(new NewGui.TwoColumns
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 90)
            });

            var statsLeft = statParent.AddChild(new Widget());
            var statsRight = statParent.AddChild(new Widget());

            StatDexterity = statsLeft.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StatStrength = statsLeft.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StatWisdom = statsLeft.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StatConstitution = statsRight.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StatIntelligence = statsRight.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StatSize = statsRight.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });
            #endregion

            #region status bars
            Hunger = CreateStatusBar(left, "Hunger");
            Energy = CreateStatusBar(left, "Energy");
            Happiness = CreateStatusBar(left, "Happiness");
            Health = CreateStatusBar(left, "Health");
            #endregion

            LevelLabel = right.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            TitleLabel = right.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            ExperienceLabel = right.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            PayLabel = right.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            left.AddChild(new Widget
            {
                Text = "Fire",
                Border = "border-button",
                AutoLayout = AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    Root.SafeCall(OnFireClicked, this);
                }
            });

            LevelButton = right.AddChild(new Widget
            {
                Text = "Level Up!",
                Border = "border-button",
                AutoLayout = AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    Employee.Stats.LevelUp();
                    SoundManager.PlaySound(ContentPaths.Audio.change);
                    Invalidate();
                    Employee.AddThought(Thought.ThoughtType.GotPromoted);
                }
            });

            base.Construct();
        }

        private Gum.Widgets.ProgressBar CreateStatusBar(Widget AddTo, String Label)
        {
            var row = AddTo.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            row.AddChild(new Widget
            {
                Text = Label,
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(100, 30)
            });

            return row.AddChild(new Gum.Widgets.ProgressBar
            {
                AutoLayout = AutoLayout.DockFill
            }) as Gum.Widgets.ProgressBar;
        }

        protected override Gum.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null)
            {
                NameLabel.Text = Employee.Stats.FullName;
                StatDexterity.Text = String.Format("Dex: {0}", Employee.Stats.BuffedDex);
                StatStrength.Text = String.Format("Str: {0}", Employee.Stats.BuffedStr);
                StatWisdom.Text = String.Format("Wis: {0}", Employee.Stats.BuffedWis);
                StatConstitution.Text = String.Format("Con: {0}", Employee.Stats.BuffedCon);
                StatIntelligence.Text = String.Format("Int: {0}", Employee.Stats.BuffedInt);
                StatSize.Text = String.Format("Size: {0}", Employee.Stats.BuffedSiz);

                SetStatusBar(Hunger, Employee.Status.Hunger);
                SetStatusBar(Energy, Employee.Status.Energy);
                SetStatusBar(Happiness, Employee.Status.Happiness);
                SetStatusBar(Health, Employee.Status.Health);

                LevelLabel.Text = String.Format("Level {0} {1}", Employee.Stats.LevelIndex,
                    Employee.Stats.CurrentClass.Name);
                TitleLabel.Text = Employee.Stats.CurrentLevel.Name;

                if (Employee.Stats.CurrentClass.Levels.Count > Employee.Stats.LevelIndex + 1)
                {
                    var nextLevel = Employee.Stats.CurrentClass.Levels[Employee.Stats.LevelIndex + 1];
                    var diff = nextLevel.XP - Employee.Stats.XP;

                    if (diff > 0)
                    {
                        ExperienceLabel.Text = String.Format("XP: {0}\n({1} to next level)",
                            Employee.Stats.XP, diff);
                        LevelButton.Hidden = true;
                    }
                    else
                    {
                        ExperienceLabel.Text = String.Format("XP: {0}\n({1} to next level)",
                            Employee.Stats.XP, "(Overqualified)");
                        LevelButton.Hidden = false;
                        LevelButton.Tooltip = "Promote to " + nextLevel.Name;
                    }
                }
                else
                {
                    ExperienceLabel.Text = String.Format("XP: {0}", Employee.Stats.XP);
                }

                PayLabel.Text = String.Format("Pay: {0}/day\nWealth: {1}", Employee.Stats.CurrentLevel.Pay,
                    Employee.Status.Money);
            }

            foreach (var child in Children)
                child.Invalidate();

            return base.Redraw();
        }

        private void SetStatusBar(Gum.Widgets.ProgressBar Bar, CreatureStatus.Status Status)
        {
            Bar.Percentage = (float)Status.Percentage / 100.0f;
        }
    }
}
