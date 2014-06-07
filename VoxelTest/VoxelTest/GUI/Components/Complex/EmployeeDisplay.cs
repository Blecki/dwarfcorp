using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class EmployeeDisplay : Panel
    {

        public Faction Faction { get; set; }
        public ListSelector EmployeeSelector { get; set; }

        public GroupBox CurrentMinionBox { get; set; }
        public MinionPanel CurrentMinionPanel { get; set; }

        public EmployeeDisplay(DwarfGUI gui, GUIComponent parent, Faction faction) :
            base(gui, parent)
        {
            Faction = faction;
            Initialize();
        }

        public void Initialize()
        {
            ClearChildren();
            GridLayout panelLayout = new GridLayout(GUI, this, 10, 10);

            GroupBox employeeBox = new GroupBox(GUI, panelLayout, "Employees");
            GridLayout boxLayout = new GridLayout(GUI, employeeBox, 1, 1);
            EmployeeSelector = new ListSelector(GUI, boxLayout)
            {
                Label = "",
                DrawPanel = false,
                Mode = ListItem.SelectionMode.Selector
            };
            boxLayout.SetComponentPosition(EmployeeSelector, 0, 0, 1, 1);
            panelLayout.SetComponentPosition(employeeBox, 0, 0, 3, 10);

            foreach (CreatureAI creature in Faction.Minions)
            {
                EmployeeSelector.AddItem(creature.Stats.FirstName + " " + creature.Stats.LastName);
            }


            EmployeeSelector.OnItemSelected += EmployeeSelector_OnItemSelected;

            CurrentMinionBox = new GroupBox(GUI, panelLayout, "");

            GridLayout minionLayout = new GridLayout(GUI, CurrentMinionBox, 10, 10);
            CurrentMinionPanel = new MinionPanel(GUI, minionLayout, Faction.Minions[0]);
            minionLayout.EdgePadding = 0;
            minionLayout.SetComponentPosition(CurrentMinionPanel, 0, 1, 10, 9);

            panelLayout.SetComponentPosition(CurrentMinionBox, 3, 0, 4, 10);

            OnMinionSelected(Faction.Minions[0]);
            
        }



        void EmployeeSelector_OnItemSelected(int index, ListItem item)
        {
            CreatureAI selectedMinion = Faction.Minions[index];
            OnMinionSelected(selectedMinion);
        }

        void OnMinionSelected(CreatureAI minion)
        {
            CurrentMinionBox.Title = "Employee: " + minion.Stats.FirstName + " " + minion.Stats.LastName;
            CurrentMinionPanel.Minion = minion;
        }


    }
}
