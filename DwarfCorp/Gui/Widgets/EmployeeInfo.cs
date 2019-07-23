#define ENABLE_CHAT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        private Widget InteriorPanel;

        private DwarfCorp.Gui.Widgets.EmployeePortrait Icon;
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
        private Gui.Widgets.TextProgressBar Boredom;

        private Widget TitleEditor;
        private Widget LevelLabel;
        private Widget PayLabel;
        private Widget LevelButton;

        private Widget Thoughts;
        private Widget Bio;
        private Widget TaskLabel;
        private Widget CancelTask;
        private Widget AgeLabel;

        public Action<Widget> OnFireClicked;

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
            Text = "You have no employees.";
            Font = "font16";

            InteriorPanel = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Hidden = true,
                Background = new TileReference("basic", 0),
                Font = "font8",
            });

            var top = InteriorPanel.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 96)
            });

            Icon = top.AddChild(new EmployeePortrait
            {
                AutoLayout = AutoLayout.DockLeftCentered,
                MinimumSize = new Point(48, 40),
                MaximumSize = new Point(48, 40)
            }) as EmployeePortrait;        

            NameLabel = top.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 48),
                Font = "font16"
            });

            var levelHolder = top.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(256, 24)
            });

            TitleEditor = levelHolder.AddChild(new Gui.Widgets.EditableTextField()
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(128, 24),
                OnTextChange = (sender) =>
                {
                    Employee.Stats.Title = sender.Text;
                },
                Tooltip = "Employee title. You can customize this."
            });

            LevelLabel = levelHolder.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(128, 24)
            });

            var columns = InteriorPanel.AddChild(new Gui.Widgets.Columns
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
                    var employeeInfo = sender.FindParentOfType<EmployeeInfo>();
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

            PayLabel = InteriorPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });
            
            AgeLabel = InteriorPanel.AddChild(new Widget() {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            Bio = InteriorPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            var task = InteriorPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(1, 24)
            });

            var inventory = task.AddChild(new Button
            {
                AutoLayout = AutoLayout.DockRight,
                Text = "Backpack...",
                OnClick = (sender, args) =>
                {
                    var employeeInfo = sender.Parent.Parent.Parent as EmployeeInfo;
                    if (employeeInfo != null && employeeInfo.Employee != null)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.Append("Backpack contains:\n");
                        Dictionary<string, ResourceAmount> aggregateResources = employeeInfo.Employee.Creature.Inventory.Aggregate();
                        foreach (var resource in aggregateResources)
                        {
                            stringBuilder.Append(String.Format("{0}x {1}\n", resource.Value.Count, resource.Key));
                        }
                        if (aggregateResources.Count == 0)
                        {
                            stringBuilder.Append("Nothing.");
                        }

                        Confirm popup = new Confirm()
                        {
                            CancelText = "",
                            Text = stringBuilder.ToString()
                        };


                        sender.Root.ShowMinorPopup(popup);

                        if (aggregateResources.Count > 0)
                        {
                            popup.AddChild(new Button()
                            {
                                Text = "Empty",
                                Tooltip = "Click to order this dwarf to empty their backpack.",
                                AutoLayout = AutoLayout.FloatBottomLeft,
                                OnClick = (currSender, currArgs) =>
                                {
                                    if (employeeInfo != null && employeeInfo.Employee != null
                                         && employeeInfo.Employee.Creature != null)
                                        employeeInfo.Employee.Creature.AssignRestockAllTasks(TaskPriority.Urgent);
                                }
                            });
                            popup.Layout();

                        }
                       
                    }
                }
            });

            CancelTask = task.AddChild(new Button
            {
                AutoLayout = AutoLayout.DockRight,
                Text = "Cancel Task",
                ChangeColorOnHover = true
            });

            TaskLabel = task.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(256, 24),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Right
            });

            Thoughts = InteriorPanel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 24)
            });

            var bottomBar = InteriorPanel.AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });

            bottomBar.AddChild(new Button()
            {
                Text = "Fire",
                Border = "border-button",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    if (Employee == null)
                        return;

                    Root.ShowModalPopup(Root.ConstructWidget(new Confirm
                    {
                        OkayText = Library.GetString("fire-dwarf"),
                        CancelText = Library.GetString("keep-dwarf"),
                        Text = String.Format("Really fire {0}? They will collect {1} in severance pay.", Employee.Stats.FullName, Employee.Stats.CurrentLevel.Pay * 4),
                        Padding = new Margin(32, 10, 10, 10),
                        MinimumSize = new Point(512, 128),
                        OnClose = (confirm) =>
                        {
                            if ((confirm as Gui.Widgets.Confirm).DialogResult == DwarfCorp.Gui.Widgets.Confirm.Result.OKAY)
                            {
                                SoundManager.PlaySound(ContentPaths.Audio.change, 0.25f);

                                if (Employee.IsDead)
                                    return;

                                if (Employee.GetRoot().GetComponent<Inventory>().HasValue(out var inv))
                                    inv.Die();

                                Employee.World.MakeAnnouncement(Library.GetString("was-fired", Employee.Stats.FullName));
                                Employee.GetRoot().Delete();

                                Employee.World.FireEmployee(Employee);
                            }
                        }
                    }));

                    Root.SafeCall(OnFireClicked, this);
                }
            });

            bottomBar.AddChild(new Button()
            {
                Text = "Tasks...",
                Tooltip = "Open allowed tasks filter.",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    var screen = sender.Root.RenderData.VirtualScreen;
                    sender.Root.ShowModalPopup(new AllowedTaskFilter
                    {
                        Employee = Employee,
                        Tag = "selected-employee-allowable-tasks",
                        AutoLayout = AutoLayout.DockFill,
                        MinimumSize = new Point(256, 256),
                        Border = "border-fancy",
                        Rect = new Rectangle(screen.Center.X - 128, screen.Center.Y - 128, 256, 256)
                    });
                }
            });

#if ENABLE_CHAT
            if (Employee != null && Employee.GetRoot().GetComponent<DwarfThoughts>().HasValue(out var thoughts))
            {
                bottomBar.AddChild(new Button()
                {
                    Text = "Chat...",
                    Tooltip = "Have a talk with your employee.",
                    AutoLayout = AutoLayout.DockRight,
                    OnClick = (sender, args) =>
                    {
                        Employee.Chat();
                    }
                });
            }
#endif


            LevelButton = bottomBar.AddChild(new Button()
            {
                Text = "Promote!",
                Border = "border-button",
                AutoLayout = AutoLayout.DockRight,
                Tooltip = "Click to promote this dwarf.\nPromoting Dwarves raises their pay and makes them\nmore effective workers.",
                OnClick = (sender, args) =>
                {
                    var prevLevel = Employee.Stats.CurrentLevel;
                    Employee.Stats.LevelUp(Employee.Creature);
                    if (Employee.Stats.CurrentLevel.HealingPower > prevLevel.HealingPower)
                    {
                        Employee.World.MakeAnnouncement(String.Format("{0}'s healing power increased to {1}!", Employee.Stats.FullName, Employee.Stats.CurrentLevel.HealingPower));
                    }

                    if (Employee.Stats.CurrentLevel.ExtraWeapons.Count > prevLevel.ExtraWeapons.Count)
                    {
                        Employee.World.MakeAnnouncement(String.Format("{0} learned to cast {1}!", Employee.Stats.FullName, Employee.Stats.CurrentLevel.ExtraWeapons.Last().Name));
                    }
                    SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
                    Invalidate();
                    Employee.Creature.AddThought("I got promoted recently.", new TimeSpan(3, 0, 0, 0), 20.0f);
                }
            });


            var topbuttons = top.AddChild(new Widget()
            {
                AutoLayout = AutoLayout.FloatTopRight,
                MinimumSize = new Point(32, 24)
            });
            topbuttons.AddChild(new Widget()
            {
                Text = "<",
                Font = "font10",
                Tooltip = "Previous employee.",
                AutoLayout = AutoLayout.DockLeft,
                ChangeColorOnHover = true,
                MinimumSize = new Point(16, 24),
                OnClick = (sender, args) =>
                {
                    if (Employee.Faction.Minions.Count == 0)
                        Employee = null;

                    if (Employee == null)
                        return;
                    int idx = Employee.Faction.Minions.IndexOf(Employee);
                    if (idx < 0)
                        idx = 0;
                    else
                        idx--;
                    Employee = Employee.Faction.Minions[Math.Abs(idx) % Employee.Faction.Minions.Count];
                    Employee.World.PersistentData.SelectedMinions = new List<CreatureAI>() { Employee };
                }
            });
            topbuttons.AddChild(new Widget()
            {
                Text = ">",
                Font = "font10",
                Tooltip = "Next employee.",
                AutoLayout = AutoLayout.DockRight,
                ChangeColorOnHover = true,
                MinimumSize = new Point(16, 24),
                OnClick = (sender, args) =>
                {
                    if (Employee.Faction.Minions.Count == 0)
                        Employee = null;

                    if (Employee == null)
                        return;
                    int idx = Employee.Faction.Minions.IndexOf(Employee);
                    if (idx < 0)
                        idx = 0;
                    else
                        idx++;
                    Employee = Employee.Faction.Minions[idx % Employee.Faction.Minions.Count];
                    Employee.World.PersistentData.SelectedMinions = new List<CreatureAI>() { Employee };
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
                Font = "font8"
            }) as Gui.Widgets.TextProgressBar;
        }

        protected override Gui.Mesh Redraw()
        {
            // Set values from CreatureAI
            if (Employee != null && !Employee.IsDead)
            {
                InteriorPanel.Hidden = false;

                if (Employee.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>().HasValue(out var sprite))
                {
                    Icon.Sprite = sprite.GetLayers();
                    Icon.AnimationPlayer = sprite.AnimPlayer;
                }
                else
                {
                    Icon.Sprite = null;
                    Icon.AnimationPlayer = null;
                }

                NameLabel.Text = "\n" + Employee.Stats.FullName;
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
                TitleEditor.Text = Employee.Stats.Title ?? Employee.Stats.CurrentClass.Name;
                LevelLabel.Text = String.Format("Level {0} {1}\n({2} xp). {3}",
                    Employee.Stats.LevelIndex,
                    Employee.Stats.CurrentClass.Name,
                    Employee.Stats.XP,
                    Employee.Creature.Stats.Gender);

                Bio.Text = Employee.Biography;

                StringBuilder thoughtsBuilder = new StringBuilder();
                thoughtsBuilder.Append("Thoughts:\n");
                if (Employee.Physics.GetComponent<DwarfThoughts>().HasValue(out var thoughts))
                    foreach (var thought in thoughts.Thoughts)
                        thoughtsBuilder.Append(String.Format("{0} ({1})\n", thought.Description, thought.HappinessModifier));
   
                var diseases = Employee.Creature.Stats.Buffs.OfType<Disease>();
                if (diseases.Any())
                {
                    thoughtsBuilder.Append("Conditions: ");
                }

                if (Employee.Stats.IsAsleep)
                {
                    thoughtsBuilder.AppendLine("Unconscious");
                }

                foreach (var disease in diseases)
                {
                    thoughtsBuilder.AppendLine(disease.Name);
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
                        LevelButton.Invalidate();
                    }
                    else
                    {
                        //ExperienceLabel.Text = String.Format("XP: {0}\n({1} to next level)",
                        //    Employee.Stats.XP, "(Overqualified)");
                        LevelButton.Hidden = false;
                        LevelButton.Tooltip = "Promote to " + nextLevel.Name;
                        LevelButton.Invalidate();
                    }
                }
                else
                {
                    //ExperienceLabel.Text = String.Format("XP: {0}", Employee.Stats.XP);
                }

                PayLabel.Text = String.Format("Pay: {0}/day\nWealth: {1}", Employee.Stats.CurrentLevel.Pay,
                    Employee.Stats.Money);

                if (Employee.CurrentTask != null)
                {
                    TaskLabel.Text = "Current Task: " + Employee.CurrentTask.Name;
                    CancelTask.TextColor = new Vector4(0, 0, 0, 1);
                    CancelTask.Invalidate();
                    CancelTask.OnClick = (sender, args) =>
                    {
                        if (Employee.CurrentTask != null)
                        {
                            Employee.CancelCurrentTask();
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
            else
                InteriorPanel.Hidden = true;

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
