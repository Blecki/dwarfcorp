// EmployeeDisplay.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class EmployeeDisplay : Panel
    {
        public EmployeeDisplay(DwarfGUI gui, GUIComponent parent, Faction faction) :
            base(gui, parent)
        {
            Faction = faction;
            Initialize();
        }

        public Faction Faction { get; set; }
        public ListSelector EmployeeSelector { get; set; }

        public GroupBox CurrentMinionBox { get; set; }
        public MinionPanel CurrentMinionPanel { get; set; }

        public void Initialize()
        {
            ClearChildren();

            var panelLayout = new GridLayout(GUI, this, 10, 10);
            var employeeBox = new GroupBox(GUI, panelLayout, "Employees");
            var boxLayout = new GridLayout(GUI, employeeBox, 8, 4);
            var scrollView = new ScrollView(GUI, boxLayout);
            EmployeeSelector = new ListSelector(GUI, scrollView)
            {
                Label = "",
                DrawPanel = false,
                Mode = ListItem.SelectionMode.Selector,
                LocalBounds = new Rectangle(0, 0, 256, Faction.Minions.Count*24),
                WidthSizeMode = SizeMode.Fit
            };

            boxLayout.SetComponentPosition(scrollView, 0, 1, 3, 6);
            panelLayout.SetComponentPosition(employeeBox, 0, 0, 3, 10);

            foreach (CreatureAI creature in Faction.Minions)
            {
                EmployeeSelector.AddItem(creature.Stats.FullName);
            }


            EmployeeSelector.OnItemSelected += EmployeeSelector_OnItemSelected;

            var hireButton = new Button(GUI, boxLayout, "Hire new", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomIn))
            {
                ToolTip = "Hire new employees"
            };

            boxLayout.SetComponentPosition(hireButton, 0, 7, 2, 1);

            hireButton.OnClicked += hireButton_OnClicked;

            CurrentMinionBox = new GroupBox(GUI, panelLayout, "");

            var minionLayout = new GridLayout(GUI, CurrentMinionBox, 10, 10);
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

        private void CurrentMinionPanel_Fire(CreatureAI creature)
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
                EmployeeSelector.AddItem(minion.Stats.FullName);
            }

            OnMinionSelected(Faction.Minions.FirstOrDefault());
        }


        private void hireButton_OnClicked()
        {
            HireDialog dialog = HireDialog.Popup(GUI, Faction);
            dialog.OnHired += dialog_OnHired;
        }

        private void dialog_OnHired(Applicant applicant)
        {
            EmployeeSelector.ClearItems();
            foreach (CreatureAI creature in Faction.Minions)
            {
                EmployeeSelector.AddItem(creature.Stats.FullName);
            }
        }


        private void EmployeeSelector_OnItemSelected(int index, ListItem item)
        {
            CreatureAI selectedMinion = Faction.Minions[index];
            OnMinionSelected(selectedMinion);
        }

        private void OnMinionSelected(CreatureAI minion)
        {
            if (minion != null)
            {
                CurrentMinionBox.Title = "Employee: " + minion.Stats.FullName;
            }
            else
            {
                CurrentMinionBox.Title = "None";
            }
            CurrentMinionPanel.Minion = minion;
        }
    }
}