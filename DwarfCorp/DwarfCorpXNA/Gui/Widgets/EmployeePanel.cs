using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeePanel : Columns
    {
        public Faction Faction;
        private Gui.Widgets.WidgetListView EmployeeList;
        private static Dictionary<String, int> IconIndex;

        private void RebuildEmployeeList()
        {
            EmployeeList.ClearItems();

            foreach (var employee in Faction.Minions)
            {
                var bar = Root.ConstructWidget(new Widget
                {
                    Background = new TileReference("basic", 0)
                });
                var employeeSprite = employee.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();
               

                if (employeeSprite != null)
                    bar.AddChild(new EmployeePortrait
                    {
                        AutoLayout = AutoLayout.DockLeft,
                        MinimumSize = new Point(48, 40),
                        MaximumSize = new Point(48, 40),
                        Sprite = employeeSprite.GetLayers(),
                        AnimationPlayer = employeeSprite.AnimPlayer
                    });

                var title = employee.Stats.Title ?? employee.Stats.CurrentLevel.Name;
                bar.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockFill,
                    TextVerticalAlign = VerticalAlign.Center,
                    MinimumSize = new Point(128, 64),
                    Text = (employee.Stats.IsOverQualified ? employee.Stats.FullName + "*" : employee.Stats.FullName) + " (" + title + ")"
                });

                EmployeeList.AddItem(bar);
            }

            EmployeeList.AddItem(new Widget
            {
                Text = "+ Hire New Employee",
                MinimumSize = new Point(128, 64),
                OnClick = (sender, args) =>
                {
                    // Show hire dialog.
                    var dialog = Root.ConstructWidget(
                        new HireEmployeeDialog(Faction.World.PlayerCompany.Information)
                        {
                            Faction = Faction,
                            OnClose = (_s) =>
                            {
                                RebuildEmployeeList();
                            }
                        });
                    Root.ShowModalPopup(dialog);
                    Faction.World.Tutorial("hire");
                }
            });

            EmployeeList.SelectedIndex = 0;
        }

        public static int GetIconIndex(String Class)
        {
            if (IconIndex == null)
            {
                IconIndex = new Dictionary<string, int>();
                IconIndex.Add("Craftdwarf", 0);
                IconIndex.Add("Musket", 1);
                IconIndex.Add("Miner", 2);
                IconIndex.Add("AxeDwarf", 3);
                IconIndex.Add("Wizard", 4);
            }

            if (IconIndex.ContainsKey(Class))
                return IconIndex[Class];
            return -1;
        }

        public override void Construct()
        {
            

            var left = AddChild(new Widget());
            var right = AddChild(new EmployeeInfo
            {
                OnFireClicked = (sender) =>
                {
                    Root.ShowModalPopup(Root.ConstructWidget(new Gui.Widgets.Confirm
                    {
                        OkayText = "Fire this dwarf!",
                        CancelText = "Keep this dwarf.",
                        Padding = new Margin(32, 10, 10, 10),
                        OnClose = (confirm) =>
                        {
                            if ((confirm as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                            {
                                SoundManager.PlaySound(ContentPaths.Audio.change, 0.5f);
                                var employeeInfo = (sender as EmployeeInfo);
                                if (employeeInfo == null)
                                {
                                    Console.Error.WriteLine("Error firing dwarf. This should not have happened!");
                                    return;
                                }
                                var selectedEmployee = employeeInfo.Employee;
                                selectedEmployee.GetRoot().Delete();

                                Faction.Minions.Remove(selectedEmployee);
                                Faction.SelectedMinions.Remove(selectedEmployee);

                                RebuildEmployeeList();
                            }
                        }
                    }));
                }
            }) as EmployeeInfo;

            var bottomBar = left.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30)
            });

            EmployeeList = left.AddChild(new Gui.Widgets.WidgetListView
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font10",
                ItemHeight = 64,
                OnSelectedIndexChanged = (sender) =>
                {
                    if ((sender as Gui.Widgets.WidgetListView).SelectedIndex >= 0 &&
                        (sender as Gui.Widgets.WidgetListView).SelectedIndex < Faction.Minions.Count)
                    {
                        right.Hidden = false;
                        right.Employee = Faction.Minions[(sender as Gui.Widgets.WidgetListView).SelectedIndex];
                    }
                    else
                        right.Hidden = true;
                }
            }) as Gui.Widgets.WidgetListView;

            RebuildEmployeeList();
        }
    }
}
