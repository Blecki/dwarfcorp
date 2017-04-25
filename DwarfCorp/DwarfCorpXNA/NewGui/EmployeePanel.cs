using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class EmployeePanel : TwoColumns
    {
        public Faction Faction;
        private Gum.Widgets.ListView EmployeeList;

        public override void Construct()
        {
            var left = AddChild(new Widget());
            var right = AddChild(new EmployeeInfo
            {
                OnFireClicked = (sender) =>
                {
                    Root.ShowDialog(Root.ConstructWidget(new NewGui.Confirm
                    {
                        OkayText = "Fire this dwarf!",
                        CancelText = "Keep this dwarf.",
                        OnClose = (confirm) =>
                        {
                            if ((confirm as NewGui.Confirm).DialogResult == NewGui.Confirm.Result.OKAY)
                            {

                                SoundManager.PlaySound(ContentPaths.Audio.change);
                                var selectedEmployee = (sender as EmployeeInfo).Employee;
                                selectedEmployee.GetEntityRootComponent().Delete();

                                Faction.Minions.Remove(selectedEmployee);
                                Faction.SelectedMinions.Remove(selectedEmployee);

                                EmployeeList.Items = Faction.Minions.Select(m => m.Stats.FullName).ToList();
                                EmployeeList.SelectedIndex = 0;
                            }
                        }
                    }));
                }
            }) as EmployeeInfo;

            left.AddChild(new Widget
            {
                Text = "Employees",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            var bottomBar = left.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30)
            });

            EmployeeList = left.AddChild(new Gum.Widgets.ListView
            {
                AutoLayout = AutoLayout.DockFill,
                Items = Faction.Minions.Select(m => m.Stats.FullName).ToList(),
                Font = "font-hires",
                OnSelectedIndexChanged = (sender) =>
                {
                    if ((sender as Gum.Widgets.ListView).SelectedIndex >= 0 &&
                        (sender as Gum.Widgets.ListView).SelectedIndex < Faction.Minions.Count)
                    {
                        right.Hidden = false;
                        right.Employee = Faction.Minions[(sender as Gum.Widgets.ListView).SelectedIndex];
                    }
                    else
                        right.Hidden = true;
                }
            }) as Gum.Widgets.ListView;

            EmployeeList.SelectedIndex = 0;

            bottomBar.AddChild(new Widget
            {
                Text = "Hire New Employee",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    // Show hire dialog.
                    var dialog = Root.ConstructWidget(
                        new HireEmployeeDialog(Faction.World.PlayerCompany.Information)
                        {
                            Faction = Faction,
                            OnClose = (_s) =>
                            {
                                EmployeeList.Items = Faction.Minions.Select(m => m.Stats.FullName).ToList();
                                EmployeeList.SelectedIndex = 0;
                            }
                        });
                    Root.ShowDialog(dialog);
                }
            });
        }
    }
}
