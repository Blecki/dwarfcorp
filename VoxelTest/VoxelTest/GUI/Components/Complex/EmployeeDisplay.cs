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
            GridLayout boxLayout = new GridLayout(GUI, employeeBox, 8, 4);
            ScrollView scrollView = new ScrollView(GUI, boxLayout);
            EmployeeSelector = new ListSelector(GUI, scrollView)
            {
                Label = "",
                DrawPanel = false,
                Mode = ListItem.SelectionMode.Selector,
                LocalBounds = new Rectangle(0, 0, 256, Faction.Minions.Count * 24)
            };

            boxLayout.SetComponentPosition(scrollView, 0, 1, 4, 6);
            panelLayout.SetComponentPosition(employeeBox, 0, 0, 3, 10);

            foreach (CreatureAI creature in Faction.Minions)
            {
                EmployeeSelector.AddItem(creature.Stats.FirstName + " " + creature.Stats.LastName);
            }


            EmployeeSelector.OnItemSelected += EmployeeSelector_OnItemSelected;

            Button hireButton = new Button(GUI, boxLayout, "Hire new", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomIn))
            {
                ToolTip = "Hire new employees"
            };

            boxLayout.SetComponentPosition(hireButton, 0, 7, 2, 1);
            
            hireButton.OnClicked += hireButton_OnClicked;

            CurrentMinionBox = new GroupBox(GUI, panelLayout, "");

            GridLayout minionLayout = new GridLayout(GUI, CurrentMinionBox, 10, 10);
            CurrentMinionPanel = new MinionPanel(GUI, minionLayout, Faction.Minions.FirstOrDefault());
            CurrentMinionPanel.Fire += CurrentMinionPanel_Fire;
            minionLayout.EdgePadding = 0;
            minionLayout.SetComponentPosition(CurrentMinionPanel, 0, 1, 10, 9);

            panelLayout.SetComponentPosition(CurrentMinionBox, 3, 0, 4, 10);

            if (Faction.Minions.Count > 0)
            {
                OnMinionSelected(Faction.Minions[0]);
            }



        }

        void CurrentMinionPanel_Fire(CreatureAI creature)
        {
            SoundManager.PlaySound(ContentPaths.Audio.change);
            creature.GetRootComponent().Delete();
            creature.IsDead = true;

            Faction.Minions.Remove(creature);
            Faction.SelectedMinions.Remove(creature);

            EmployeeSelector.ClearItems();
            EmployeeSelector.ClearChildren();

            foreach (CreatureAI minion in Faction.Minions)
            {
                EmployeeSelector.AddItem(minion.Stats.FirstName + " " + minion.Stats.LastName);
            }

            OnMinionSelected(Faction.Minions.FirstOrDefault());
            

        }


        void hireButton_OnClicked()
        {
            HireDialog dialog = HireDialog.Popup(GUI, Faction);
            dialog.OnHired += dialog_OnHired;
        }

        void dialog_OnHired(Applicant applicant)
        {
            EmployeeSelector.ClearItems();
            foreach (CreatureAI creature in Faction.Minions)
            {
                EmployeeSelector.AddItem(creature.Stats.FirstName + " " + creature.Stats.LastName);
            }

        }



        void EmployeeSelector_OnItemSelected(int index, ListItem item)
        {
            CreatureAI selectedMinion = Faction.Minions[index];
            OnMinionSelected(selectedMinion);
        }

        void OnMinionSelected(CreatureAI minion)
        {
            if (minion != null)
            {
                CurrentMinionBox.Title = "Employee: " + minion.Stats.FirstName + " " + minion.Stats.LastName;
            }
            else
            {
                CurrentMinionBox.Title = "None";
            }
            CurrentMinionPanel.Minion = minion;
        }


    }
}
