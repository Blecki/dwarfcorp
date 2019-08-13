#define ENABLE_CHAT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Play.EmployeeInfo
{

    public class StatsPanel : Widget
    {
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

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
        private Gui.Widgets.TextProgressBar Boredom;

        private Widget Thoughts;

        private String SetLength(String S, int L)
        {
            if (S.Length > L)
                return S.Substring(0, L);

            if (S.Length < L)
                S += new string(' ', L - S.Length);

            return S;
        }

        private String FormatNumber(float F)
        {
            F = (float)Math.Round(F, 1);
            if (F == 0) return " 0  ";
            var r = "";
            if (F < 0) r += "-";
            if (F > 0) r += "+";

            var front = (int)Math.Floor(Math.Abs(F));
            var back = (int)((Math.Abs(F) - front) * 10.0f);
            if (front > 9)
            {
                front = 9;
                back = 9;
            }

            r += String.Format("{0}.{1}", front, back);
            return r;
        }

        public override void Construct()
        {
            Font = "font8";

            var columns = AddChild(new Gui.Widgets.Columns
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 100),
                ColumnCount = 3
            });
            
            var left = columns.AddChild(new Gui.Widget());
            var right = columns.AddChild(new Gui.Widget());
            var evenMoreRight = columns.AddChild(new Gui.Widget());

            #region Stats
            var statParent = left.AddChild(new Gui.Widgets.Columns
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 60),
                TriggerOnChildClick = true,
                OnClick = (sender, args) =>
                {
                    var employeeInfo = sender.FindParentOfType<OverviewPanel>();
                    if (employeeInfo != null && employeeInfo.Employee != null)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.Append("MODIFIERS: CHA  CON  DEX  INT  SIZ  STR  WIS \n");
                        // Todo: Need to align columns. Current method only works for monospaced fonts.

                        foreach (var modifier in employeeInfo.Employee.Creature.Stats.EnumerateStatAdjustments())
                        {
                            stringBuilder.Append(SetLength(modifier.Name, 9));
                            stringBuilder.Append(": ");
                            stringBuilder.Append(FormatNumber(modifier.Charisma));
                            stringBuilder.Append(" ");
                            stringBuilder.Append(FormatNumber(modifier.Constitution));
                            stringBuilder.Append(" ");
                            stringBuilder.Append(FormatNumber(modifier.Dexterity));
                            stringBuilder.Append(" ");
                            stringBuilder.Append(FormatNumber(modifier.Intelligence));
                            stringBuilder.Append(" ");
                            stringBuilder.Append(FormatNumber(modifier.Size));
                            stringBuilder.Append(" ");
                            stringBuilder.Append(FormatNumber(modifier.Strength));
                            stringBuilder.Append(" ");
                            stringBuilder.Append(FormatNumber(modifier.Wisdom));
                            stringBuilder.Append("\n");
                        }

                            Confirm popup = new Confirm()
                        {
                            CancelText = "",
                            Text = stringBuilder.ToString()
                        };

                        sender.Root.ShowMinorPopup(popup);
                    }
                }
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
            Health = CreateStatusBar(evenMoreRight, "Health", "Near Death", "Critical", "Hurt", "Uncomfortable", "Fine", "Perfect");
            Boredom = CreateStatusBar(evenMoreRight, "Boredom", "Desperate", "Overworked", "Bored", "Meh", "Fine", "Excited");
            #endregion           

            Thoughts = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            var bottomBar = AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
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
                Font = "font8"
            }) as Gui.Widgets.TextProgressBar;
        }

        protected override Gui.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null && !Employee.IsDead)
            {
                Hidden = false;
                StatDexterity.Text = String.Format("Dex: {0}", Employee.Stats.Dexterity);
                StatStrength.Text = String.Format("Str: {0}", Employee.Stats.Strength);
                StatWisdom.Text = String.Format("Wis: {0}", Employee.Stats.Wisdom);
                StatConstitution.Text = String.Format("Con: {0}", Employee.Stats.Constitution);
                StatIntelligence.Text = String.Format("Int: {0}", Employee.Stats.Intelligence);
                StatSize.Text = String.Format("Size: {0}", Employee.Stats.Size);
                StatCharisma.Text = String.Format("Cha: {0}", Employee.Stats.Charisma);
                SetStatusBar(Hunger, Employee.Stats.Hunger);
                SetStatusBar(Energy, Employee.Stats.Energy);
                SetStatusBar(Happiness, Employee.Stats.Happiness);
                SetStatusBar(Health, Employee.Stats.Health);
                SetStatusBar(Boredom, Employee.Stats.Boredom);

                StringBuilder thoughtsBuilder = new StringBuilder();
                thoughtsBuilder.Append("Thoughts:\n");
                if (Employee.Physics.GetComponent<DwarfThoughts>().HasValue(out var thoughts))
                    foreach (var thought in thoughts.Thoughts)
                        thoughtsBuilder.Append(String.Format("{0} ({1})\n", thought.Description, thought.HappinessModifier));

                var diseases = Employee.Creature.Stats.Buffs.OfType<Disease>();
                if (diseases.Any())
                    thoughtsBuilder.Append("Conditions: ");

                if (Employee.Stats.IsAsleep)
                    thoughtsBuilder.AppendLine("Unconscious");

                foreach (var disease in diseases)
                    thoughtsBuilder.AppendLine(disease.Name);

                Thoughts.Text = thoughtsBuilder.ToString();
            }
            else
                Hidden = true;

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
