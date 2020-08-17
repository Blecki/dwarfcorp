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

    public class TaskPanel : Widget
    {
        public Action<bool> SetHideSprite = null;
        public Func<CreatureAI> FetchEmployee = null;
        public CreatureAI Employee
        {
            get { return FetchEmployee?.Invoke(); }
        }

        private Widget Bio;
        private Widget TaskLabel;
        private Widget CancelTask;

        private Widget PayLabel;
        private Widget AgeLabel;

        private CheckBox ManagerBox;

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

            AgeLabel = AddChild(new Widget()
            {
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

            var managerBar = AddChild(new Widget
            {
                MinimumSize = new Point(0, 24),
                AutoLayout = AutoLayout.DockBottom
            });

            ManagerBox = managerBar.AddChild(new CheckBox
            {
                AutoLayout = AutoLayout.DockLeft,
                OnCheckStateChange = (sender) =>
                {
                    if (Employee == null)
                        return;

                    if (!Employee.Stats.IsManager)
                        Root.ShowModalPopup(Root.ConstructWidget(new Confirm
                        {
                            OkayText = "Promote this Dwarf",
                            CancelText = "Nevermind",
                            Text = "Are you sure you want to promote this dwarf? Managers are unable to do most tasks.",
                            Padding = new Margin(32, 10, 10, 10),
                            MinimumSize = new Point(512, 128),
                            OnClose = (confirm) =>
                            {
                                if ((confirm as Gui.Widgets.Confirm).DialogResult == DwarfCorp.Gui.Widgets.Confirm.Result.OKAY)
                                {
                                    SoundManager.PlaySound(ContentPaths.Audio.change, 0.25f);

                                    if (Employee.IsDead)
                                        return;

                                    Employee.Stats.IsManager = true;
                                    Employee.World.MakeAnnouncement(String.Format("{0} was promoted to Manager", Employee.Stats.FullName));
                                    Employee.Creature.AddThought("I got promoted!", new TimeSpan(4, 0, 0), 100);

                                }
                            }
                        }));
                    else
                    {
                        // Employee is a manager - we are demoting them.

                        // Check manager capacity... can't demote if we don't have enough managers.

                        Root.ShowModalPopup(Root.ConstructWidget(new Confirm
                        {
                            OkayText = "Demote this Dwarf",
                            CancelText = "Nevermind",
                            Text = "Are you sure you want to demote this dwarf? They will not be happy about it.",
                            Padding = new Margin(32, 10, 10, 10),
                            MinimumSize = new Point(512, 128),
                            OnClose = (confirm) =>
                            {
                                if ((confirm as Gui.Widgets.Confirm).DialogResult == DwarfCorp.Gui.Widgets.Confirm.Result.OKAY)
                                {
                                    SoundManager.PlaySound(ContentPaths.Audio.change, 0.25f);

                                    if (Employee.IsDead)
                                        return;

                                    Employee.Stats.IsManager = false;
                                    Employee.World.MakeAnnouncement(String.Format("{0} was demoted.", Employee.Stats.FullName));
                                    Employee.Creature.AddThought("This is bullshit!", new TimeSpan(4, 0, 0), -200);
                                }
                            }
                        }));
                    }
                }
            }) as CheckBox;

            managerBar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Text = "Make this employee a manager."
            });

            AddChild(new AllowedTaskFilter
            {
                FetchEmployee = () => Employee,
                AutoLayout = AutoLayout.DockFill,
                Tag = "selected-employee-allowable-tasks"
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

                PayLabel.Text = String.Format("Pay: {0}/day -- Wealth: {1}", Employee.Stats.CurrentLevel.Pay, Employee.Stats.Money);
                AgeLabel.Text = String.Format("Age: {0}", Employee.Stats.Age);
                Bio.Text = Employee.Biography;
                ManagerBox.SilentSetCheckState(Employee.Stats.IsManager);

                if (Employee.CurrentTask.HasValue(out var currentTask))
                {
                    TaskLabel.Text = "Current Task: " + currentTask.Name;
                    CancelTask.TextColor = new Vector4(0, 0, 0, 1);
                    CancelTask.Invalidate();
                    CancelTask.OnClick = (sender, args) =>
                    {
                        Employee.CancelCurrentTask();
                        TaskLabel.Text = "No tasks";
                        TaskLabel.Invalidate();
                        CancelTask.OnClick = null;
                        CancelTask.TextColor = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                        CancelTask.Invalidate();
                    };
                }
                else
                {
                    TaskLabel.Text = "No tasks";
                    CancelTask.OnClick = null;
                    CancelTask.TextColor = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                    CancelTask.Invalidate();
                }
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
