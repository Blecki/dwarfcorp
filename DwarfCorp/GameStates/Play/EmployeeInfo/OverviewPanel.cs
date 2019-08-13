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

            var tabs = InteriorPanel.AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill
            }) as TabPanel;

            MainPanel = tabs.AddTab("Main", new MainPanel
            {
                SetHideSprite = (v) => HideSprite = v,
                FetchEmployee = () => Employee,
                OnFireClicked = (sender) => this.OnFireClicked?.Invoke(sender)
            });

            StatsPanel = tabs.AddTab("Stats", new StatsPanel
            {
                FetchEmployee = () => Employee
            });

            EquipmentPanel = tabs.AddTab("Equip", new EquipmentPanel
            {
                FetchEmployee = () => Employee
            });

            PackPanel = tabs.AddTab("Pack", new EquipmentPanel
            {
                FetchEmployee = () => Employee
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

                Icon.Hidden = HideSprite;

                NameLabel.Text = "\n" + Employee.Stats.FullName;
                TitleEditor.Text = Employee.Stats.Title ?? Employee.Stats.CurrentClass.Name;
                LevelLabel.Text = String.Format("Level {0} {1}\n({2} xp). {3}",
                    Employee.Stats.LevelIndex,
                    Employee.Stats.CurrentClass.Name,
                    Employee.Stats.XP,
                    Employee.Creature.Stats.Gender);
            }
            else
                InteriorPanel.Hidden = true;

            foreach (var child in Children)
                child.Invalidate();
            MainPanel.Invalidate();
            StatsPanel.Invalidate();
            EquipmentPanel.Invalidate();
            PackPanel.Invalidate();

            return base.Redraw();
        }

        private void SetStatusBar(Gui.Widgets.TextProgressBar Bar, Status Status)
        {
            Bar.Percentage = (float)Status.Percentage / 100.0f;
        }
    }
}
