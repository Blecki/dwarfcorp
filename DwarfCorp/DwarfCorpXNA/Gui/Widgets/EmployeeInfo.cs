using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeeInfo : Widget
    {
        public bool EnablePosession = false;
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
        private Widget StatCharisma;
        private Widget StatConstitution;
        private Widget StatIntelligence;
        private Widget StatSize;

        private Gui.Widgets.TextProgressBar Hunger;
        private Gui.Widgets.TextProgressBar Energy;
        private Gui.Widgets.TextProgressBar Happiness;
        private Gui.Widgets.TextProgressBar Health;

        private Widget LevelLabel;
        private Widget PayLabel;
        private Widget LevelButton;

        private Widget Thoughts;
        private Widget Bio;
        private Widget TaskLabel;
        private Widget CancelTask;
        private Widget AgeLabel;

        public Action<Widget> OnFireClicked;

        public override void Construct()
        {
            var top = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 96)
            });



            Icon = top.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 96),
            });

        

            NameLabel = top.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 32),
                Font = "font-hires"
            });

            LevelLabel = top.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });
            
            var columns = AddChild(new Gui.Widgets.TwoColumns
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 120)
            });
            
            var left = columns.AddChild(new Gui.Widget());
            var right = columns.AddChild(new Gui.Widget());

            #region Stats
            var statParent = left.AddChild(new Gui.Widgets.TwoColumns
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 60)
            });

            var statsLeft = statParent.AddChild(new Widget());
            var statsRight = statParent.AddChild(new Widget());

            StatDexterity = statsLeft.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 16),
                Tooltip = "Dexterity (affects dwarf speed)"
            });

            StatStrength = statsLeft.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 16),
                Tooltip = "Strength (affects dwarf attack power)"
            });

            StatWisdom = statsLeft.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 16),
                Tooltip = "Wisdom (affects temprement and spell resistance)"
            });

            StatCharisma = statsLeft.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 16),
                Tooltip = "Charisma (affects ability to make friends)"
            });

            StatConstitution = statsRight.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 16),
                Tooltip = "Constitution (affects dwarf health and damage resistance)"
            });

            StatIntelligence = statsRight.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 16),
                Tooltip = "Intelligence (affects crafting/farming)"
            });

            StatSize = statsRight.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 16),
                Tooltip = "Size"
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
                MinimumSize = new Point(0, 24)
            });

            AgeLabel = AddChild(new Widget() {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            Bio = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            var task = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(1, 24)
            });
            CancelTask = task.AddChild(new Button
            {
                AutoLayout = AutoLayout.DockRight,
                Text = "Cancel Task"
            });
            TaskLabel = task.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(128, 24),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Right
            });

            Thoughts = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            AddChild(new Button()
            {
                Text = "Fire",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    Root.SafeCall(OnFireClicked, this);
                }
            });

            if (EnablePosession)
            {
                AddChild(new Button()
                {
                    Text = "Follow",
                    Tooltip = "Click to directly control this dwarf and have the camera follow.",
                    AutoLayout = AutoLayout.FloatBottomLeft,
                    OnClick = (sender, args) =>
                    {
                        (sender.Parent as EmployeeInfo).Employee.IsPosessed = true;
                    }
                });
            }

            LevelButton = right.AddChild(new Button()
            {
                Text = "Promote!",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomLeft,
                Tooltip = "Click to promote this dwarf.\nPromoting Dwarves raises their pay and makes them\nmore effective workers.",
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

        private Gui.Widgets.TextProgressBar CreateStatusBar(Widget AddTo, String Label, params String[] PercentageLabels)
        {
            return AddTo.AddChild(new Gui.Widgets.TextProgressBar
            {
                AutoLayout = AutoLayout.DockTop,
                Label = Label,
                SegmentCount = 10,
                PercentageLabels = PercentageLabels,
                Font = "font"
            }) as Gui.Widgets.TextProgressBar;
        }

        protected override Gui.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null)
            {
                var idx = EmployeePanel.GetIconIndex(Employee.Stats.CurrentClass.Name);
                Icon.Background = idx >= 0 ? new TileReference("dwarves", idx) : null;
                Icon.Invalidate();
                
                NameLabel.Text = "\n" + Employee.Stats.FullName;
                StatDexterity.Text = String.Format("Dex: {0}", Employee.Stats.BuffedDex);
                StatStrength.Text = String.Format("Str: {0}", Employee.Stats.BuffedStr);
                StatWisdom.Text = String.Format("Wis: {0}", Employee.Stats.BuffedWis);
                StatConstitution.Text = String.Format("Con: {0}", Employee.Stats.BuffedCon);
                StatIntelligence.Text = String.Format("Int: {0}", Employee.Stats.BuffedInt);
                StatSize.Text = String.Format("Size: {0}", Employee.Stats.BuffedSiz);
                StatCharisma.Text = String.Format("Cha: {0}", Employee.Stats.BuffedChar);
                SetStatusBar(Hunger, Employee.Status.Hunger);
                SetStatusBar(Energy, Employee.Status.Energy);
                SetStatusBar(Happiness, Employee.Status.Happiness);
                SetStatusBar(Health, Employee.Status.Health);

                LevelLabel.Text = String.Format("\n{0}: Level {1} {2} ({3} xp). {4}", Employee.Stats.CurrentLevel.Name,
                    Employee.Stats.LevelIndex,
                    Employee.Stats.CurrentClass.Name,
                    Employee.Stats.XP,
                    Employee.Creature.Gender);

                Bio.Text = Employee.Biography;

                StringBuilder thoughtsBuilder = new StringBuilder();
                thoughtsBuilder.Append("Thoughts:\n");
                foreach (var thought in Employee.Thoughts)
                {
                    thoughtsBuilder.Append(String.Format("{0} ({1})\n", thought.Description, thought.HappinessModifier));
                }
                var diseases = Employee.Creature.Buffs.OfType<Disease>();
                if (diseases.Any())
                {
                    thoughtsBuilder.Append("Diseases: ");
                }
                foreach (var disease in diseases)
                {
                    thoughtsBuilder.Append(disease.Name + "\n");
                }
                Thoughts.Text = thoughtsBuilder.ToString();

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

                if (Employee.CurrentTask != null)
                {
                    TaskLabel.Text = "Current Task: " + Employee.CurrentTask.Name;
                    CancelTask.TextColor = new Vector4(0, 0, 0, 1);
                    CancelTask.Invalidate();
                    CancelTask.OnClick = (sender, args) =>
                    {
                        if (Employee.CurrentTask != null)
                        {
                            Employee.CurrentTask.Cancel();
                            Employee.CurrentTask = null;
                            TaskLabel.Text = "No tasks";
                            TaskLabel.Invalidate();
                            CancelTask.OnClick = null;
                            CancelTask.TextColor = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                            CancelTask.Invalidate();
                        }
                    };
                }
                else
                {
                    TaskLabel.Text = "No tasks";
                    CancelTask.OnClick = null;
                    CancelTask.TextColor = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                    CancelTask.Invalidate();
                }

                AgeLabel.Text = String.Format("Age: {0}", Employee.Stats.Age);

            }

            foreach (var child in Children)
                child.Invalidate();

            return base.Redraw();
        }

        private void SetStatusBar(Gui.Widgets.TextProgressBar Bar, Status Status)
        {
            Bar.Percentage = (float)Status.Percentage / 100.0f;
        }
    }
}
