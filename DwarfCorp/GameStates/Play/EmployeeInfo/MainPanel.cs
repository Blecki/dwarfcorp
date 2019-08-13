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

    public class MainPanel : Widget
    {
        public Action<bool> SetHideSprite = null;
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }


        private Widget PayLabel;
        private Widget LevelButton;

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
            Font = "font8";

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

            var inventory = task.AddChild(new Button
            {
                AutoLayout = AutoLayout.DockRight,
                Text = "Backpack...",
                OnClick = (sender, args) =>
                {
                    if (Employee != null)
                    {
                        var backpackDialog = new EmployeeBackpackDialog
                        {
                            Employee = Employee,
                            OnClose = (_sender) => SetHideSprite?.Invoke(false)
                        };

                        sender.Root.ShowMinorPopup(backpackDialog);
                        SetHideSprite?.Invoke(true);
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

            var bottomBar = AddChild(new Widget
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

            bottomBar.AddChild(new Button()
            {
                Text = "Find",
                Tooltip = "Zoom camera to this employee",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    Employee.World.Renderer.Camera.SetZoomTarget(Employee.Position);
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
                Hidden = false;

                Bio.Text = Employee.Biography;

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
