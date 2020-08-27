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
    public class OverviewPanel : Widget
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

        private Widget TitleEditor;
        private Widget LevelLabel;

        private bool HideSprite = false;

        public Action<Widget> OnFireClicked;

        private Widget MainPanel;
        private Widget StatsPanel;
        private Widget EquipmentPanel;
        private Widget PackPanel;
        private Widget DebugPanel;

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

            var bottomBar = InteriorPanel.AddChild(new Widget
            {
                Transparent = true,
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32),
                Font = "font8"
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
                        
            var tabs = InteriorPanel.AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                ButtonFont = "font8"
            }) as TabPanel;

            MainPanel = tabs.AddTab("Main", new TaskPanel
            {
                SetHideSprite = (v) => HideSprite = v,
                FetchEmployee = () => Employee,
            });

            StatsPanel = tabs.AddTab("Stats", new StatsPanel
            {
                FetchEmployee = () => Employee
            });

            EquipmentPanel = tabs.AddTab("Equip", new EquipmentPanel
            {
                FetchEmployee = () => Employee
            });

            PackPanel = tabs.AddTab("Pack", new PackPanel
            {
                FetchEmployee = () => Employee
            });

#if DEBUG
            DebugPanel = tabs.AddTab("Debug", new DebugPanel
            {
                FetchEmployee = () => Employee
            });
#endif

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

                    if (idx == -1) idx = Employee.Faction.Minions.Count - 1;
                    Employee = Employee.Faction.Minions[idx];
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

                    if (idx >= Employee.Faction.Minions.Count) idx = 0;
                    Employee = Employee.Faction.Minions[idx];
                    Employee.World.PersistentData.SelectedMinions = new List<CreatureAI>() { Employee };
                }
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
                        Text = String.Format("Really fire {0}? They will collect {1} in severance pay.", Employee.Stats.FullName, Employee.Stats.DailyPay * GameSettings.Current.DwarfSigningBonusFactor),
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
                    Text = "Chat...",
                    Tooltip = "Have a talk with your employee.",
                    AutoLayout = AutoLayout.DockRight,
                    OnClick = (sender, args) =>
                    {
                        if (Employee != null)
                            Employee.Chat();
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

            bottomBar.AddChild(new Button()
            {
                Text = "Empty Pack",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender1, args1) =>
                {
                    if (Employee.Creature != null)
                        Employee.Creature.AssignRestockAllTasks(TaskPriority.Urgent, true);
                },
                MinimumSize = new Point(128, 32)
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

                if (Employee.GetRoot().GetComponent<DwarfSprites.LayeredCharacterSprite>().HasValue(out var sprite))
                {
                    Icon.Sprite = sprite.GetLayers();
                    Icon.AnimationPlayer = sprite.AnimPlayer;
                }
                else
                {
                    Icon.Sprite = null;
                    Icon.AnimationPlayer = null;
                }

                Icon.Hidden = HideSprite;

                NameLabel.Text = "\n" + Employee.Stats.FullName;
                TitleEditor.Text = Employee.Stats.Title ?? (Employee.Stats.CurrentClass.HasValue(out var c) ? c.Name : "cretin");
                LevelLabel.Text = String.Format("Level {0} {1}\n({2} xp)",
                    Employee.Stats.GetCurrentLevel(),
                    (Employee.Stats.CurrentClass.HasValue(out var _c) ? _c.Name : "cretin"),
                    Employee.Stats.XP);

            }
            else
                InteriorPanel.Hidden = true;

            foreach (var child in Children)
                child.Invalidate();
            MainPanel.Invalidate();
            StatsPanel.Invalidate();
            EquipmentPanel.Invalidate();
            PackPanel.Invalidate();
            DebugPanel?.Invalidate();

            return base.Redraw();
        }

        private void SetStatusBar(Gui.Widgets.TextProgressBar Bar, Status Status)
        {
            Bar.Percentage = (float)Status.Percentage / 100.0f;
        }
    }
}
