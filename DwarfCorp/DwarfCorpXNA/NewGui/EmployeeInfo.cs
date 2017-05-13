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

        private Widget Icon;
        private Widget NameLabel;

        private Widget StatDexterity;
        private Widget StatStrength;
        private Widget StatWisdom;
        private Widget StatConstitution;
        private Widget StatIntelligence;
        private Widget StatSize;

        private Gum.Widgets.TextProgressBar Hunger;
        private Gum.Widgets.TextProgressBar Energy;
        private Gum.Widgets.TextProgressBar Happiness;
        private Gum.Widgets.TextProgressBar Health;

        private Widget LevelLabel;
        private Widget PayLabel;
        private Widget LevelButton;

        public Action<Widget> OnFireClicked;

        public override void Construct()
        {
            var top = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 96)
            });



            Icon = top.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 96),
            });

        

            NameLabel = top.AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Font = "font-hires"
            });

            LevelLabel = top.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });
            
            var columns = AddChild(new NewGui.TwoColumns
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 120)
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
            Hunger = CreateStatusBar(right, "Hunger", "Starving", "Hungry", "Peckish", "Okay");
            Energy = CreateStatusBar(right, "Energy", "Exhausted", "Tired", "Okay", "Energetic");
            Happiness = CreateStatusBar(right, "Happiness", "Miserable", "Unhappy", "So So", "Happy", "Euphoric");
            Health = CreateStatusBar(right, "Health", "Near Death", "Critical", "Hurt", "Uncomfortable", "Fine", "Perfect");
            #endregion

           

            PayLabel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            AddChild(new Widget
            {
                Text = "Fire",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    Root.SafeCall(OnFireClicked, this);
                }
            });

            LevelButton = right.AddChild(new Widget
            {
                Text = "Level Up!",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomLeft,
                OnClick = (sender, args) =>
                {
                    Employee.Stats.LevelUp();
                    SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
                    Invalidate();
                    Employee.AddThought(Thought.ThoughtType.GotPromoted);
                }
            });

            base.Construct();
        }

        private Gum.Widgets.TextProgressBar CreateStatusBar(Widget AddTo, String Label, params String[] PercentageLabels)
        {
            return AddTo.AddChild(new Gum.Widgets.TextProgressBar
            {
                AutoLayout = AutoLayout.DockTop,
                Label = Label,
                SegmentCount = 10,
                PercentageLabels = PercentageLabels,
                Font = "font-hires"
            }) as Gum.Widgets.TextProgressBar;
        }

        protected override Gum.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null)
            {
                Icon.Background = new TileReference("dwarves", EmployeePanel.GetIconIndex(Employee.Stats.CurrentClass.Name));
                Icon.Invalidate();
                
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

                LevelLabel.Text = String.Format("{0}: Level {1} {2} ({3} xp)", Employee.Stats.CurrentLevel.Name,
                    Employee.Stats.LevelIndex,
                    Employee.Stats.CurrentClass.Name,
                    Employee.Stats.XP);

                if (Employee.Stats.CurrentClass.Levels.Count > Employee.Stats.LevelIndex + 1)
                {
                    var nextLevel = Employee.Stats.CurrentClass.Levels[Employee.Stats.LevelIndex + 1];
                    var diff = nextLevel.XP - Employee.Stats.XP;

                    if (diff > 0)
                    {
                        //ExperienceLabel.Text = String.Format("XP: {0}\n({1} to next level)",
                        //    Employee.Stats.XP, diff);
                        LevelButton.Hidden = true;
                    }
                    else
                    {
                        //ExperienceLabel.Text = String.Format("XP: {0}\n({1} to next level)",
                        //    Employee.Stats.XP, "(Overqualified)");
                        LevelButton.Hidden = false;
                        LevelButton.Tooltip = "Promote to " + nextLevel.Name;
                    }
                }
                else
                {
                    //ExperienceLabel.Text = String.Format("XP: {0}", Employee.Stats.XP);
                }

                PayLabel.Text = String.Format("Pay: {0}/day\nWealth: {1}", Employee.Stats.CurrentLevel.Pay,
                    Employee.Status.Money);
            }

            foreach (var child in Children)
                child.Invalidate();

            return base.Redraw();
        }

        private void SetStatusBar(Gum.Widgets.TextProgressBar Bar, CreatureStatus.Status Status)
        {
            Bar.Percentage = (float)Status.Percentage / 100.0f;
        }
    }
}
