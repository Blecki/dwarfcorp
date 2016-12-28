// MinionPanel.cs
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

using System.Collections.Generic;

namespace DwarfCorp
{
    public class MinionPanel : GUIComponent
    {
        public delegate void MinionFired(CreatureAI creature);

        private CreatureAI minion;

        public MinionPanel(DwarfGUI gui, GUIComponent parent, CreatureAI minion) :
            base(gui, parent)
        {
            InitializePanel();
            Minion = minion;
        }

        public CreatureAI Minion
        {
            get { return minion; }
            set { SetMinion(value); }
        }

        public AnimatedImagePanel Portrait { get; set; }
        public Label ClassLabel { get; set; }
        public Label XpLabel { get; set; }
        public Label PayLabel { get; set; }
        public Dictionary<string, Label> StatLabels { get; set; }
        public Dictionary<string, MiniBar> StatusBars { get; set; }
        public Button LevelUpButton { get; set; }
        public Button FireButton { get; set; }
        public event MinionFired Fire;

        protected virtual void OnFire(CreatureAI creature)
        {
            MinionFired handler = Fire;
            if (handler != null) handler(creature);
        }

        public void InitializePanel()
        {
            StatLabels = new Dictionary<string, Label>();
            StatusBars = new Dictionary<string, MiniBar>();
            var layout = new GridLayout(GUI, this, 10, 8);

            CreateStatsLabel("Dexterity", "DEX:", layout);
            CreateStatsLabel("Strength", "STR:", layout);
            CreateStatsLabel("Wisdom", "WIS:", layout);
            CreateStatsLabel("Constitution", "CON:", layout);
            CreateStatsLabel("Intelligence", "INT:", layout);
            CreateStatsLabel("Size", "SIZ:", layout);

            int i = 0;
            int nx = 3;
            int ny = 2;
            foreach (var label in StatLabels)
            {
                layout.SetComponentPosition(label.Value, (i%nx), (((i - i%nx)/nx)%ny), 1, 1);
                i++;
            }


            CreateStatusBar("Hunger", layout);
            CreateStatusBar("Energy", layout);
            CreateStatusBar("Happiness", layout);
            CreateStatusBar("Health", layout);

            i = 0;
            nx = 2;
            ny = 3;
            foreach (var label in StatusBars)
            {
                layout.SetComponentPosition(label.Value, (i%nx)*2, (((i - i%nx)/nx)%ny)*2 + 2, 2, 2);
                i++;
            }

            Portrait = new AnimatedImagePanel(GUI, layout, new ImageFrame())
            {
                KeepAspectRatio = true
            };

            layout.SetComponentPosition(Portrait, 5, 0, 4, 4);

            ClassLabel = new Label(GUI, layout, "Level", GUI.DefaultFont)
            {
                WordWrap = true
            };

            layout.SetComponentPosition(ClassLabel, 5, 4, 4, 2);

            XpLabel = new Label(GUI, layout, "XP", GUI.SmallFont)
            {
                WordWrap = true
            };

            layout.SetComponentPosition(XpLabel, 5, 7, 2, 1);


            PayLabel = new Label(GUI, layout, "Pay", GUI.SmallFont);
            layout.SetComponentPosition(PayLabel, 5, 8, 2, 1);

            LevelUpButton = new Button(GUI, layout, "Promote", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowUp));
            layout.SetComponentPosition(LevelUpButton, 5, 9, 2, 1);
            LevelUpButton.OnClicked += LevelUpButton_OnClicked;

            FireButton = new Button(GUI, layout, "Fire", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomOut))
            {
                ToolTip = "Let this employee go."
            };

            layout.SetComponentPosition(FireButton, 0, 9, 2, 1);

            FireButton.OnClicked += FireButton_OnClicked;
        }

        private void FireButton_OnClicked()
        {
            OnFire(Minion);
        }

        private void LevelUpButton_OnClicked()
        {
            Minion.Stats.LevelUp();
            SoundManager.PlaySound(ContentPaths.Audio.change);
            UpdatePanel();
            Minion.AddThought(Thought.ThoughtType.GotPromoted);
        }

        private MiniBar CreateStatusBar(string name, GridLayout layout)
        {
            StatusBars[name] = new MiniBar(GUI, layout, 0, name);
            return StatusBars[name];
        }

        private Label CreateStatsLabel(string name, string shortName, GridLayout layout)
        {
            StatLabels[name] = new Label(GUI, layout, shortName, GUI.SmallFont)
            {
                ToolTip = name + " stat"
            };
            return StatLabels[name];
        }

        public void UpdatePanel()
        {
            if (Minion == null)
            {
                IsVisible = false;
                return;
            }

            IsVisible = true;
            StatLabels["Dexterity"].Text = "DEX: " + Minion.Stats.BuffedDex;
            StatLabels["Strength"].Text = "STR: " + Minion.Stats.BuffedStr;
            StatLabels["Wisdom"].Text = "WIS: " + Minion.Stats.BuffedWis;
            StatLabels["Constitution"].Text = "CON: " + Minion.Stats.BuffedCon;
            StatLabels["Intelligence"].Text = "INT: " + Minion.Stats.BuffedInt;
            StatLabels["Size"].Text = "SIZ: " + Minion.Stats.BuffedSiz;
            StatusBars["Happiness"].ToolTip = GetThoughtString(Minion);

            foreach (var status in Minion.Status.Statuses)
            {
                StatusBars[status.Key].Value = (status.Value.CurrentValue - status.Value.MinValue)/
                                               (status.Value.MaxValue - status.Value.MinValue);
            }

            Portrait.Image.Image = Minion.Creature.Sprite.SpriteSheet.GetTexture();
            Portrait.Animation = Minion.Creature.Sprite.CurrentAnimation;


            ClassLabel.Text = "lvl. " + Minion.Stats.LevelIndex + " " + Minion.Stats.CurrentClass.Name + "\n" +
                              Minion.Stats.CurrentLevel.Name;

            if (Minion.Stats.CurrentClass.Levels.Count > Minion.Stats.LevelIndex + 1)
            {
                EmployeeClass.Level nextLevel = Minion.Stats.CurrentClass.Levels[Minion.Stats.LevelIndex + 1];
                int diff = nextLevel.XP - Minion.Stats.XP;

                string diffText = "";

                if (diff > 0)
                {
                    diffText = "(" + diff + " to next lvl)";
                    LevelUpButton.IsVisible = false;
                }
                else
                {
                    diffText = "(Overqualified)";
                    LevelUpButton.IsVisible = true;
                    LevelUpButton.ToolTip = "Promote to " + nextLevel.Name;
                }


                XpLabel.Text = "XP: " + Minion.Stats.XP + "\n" + diffText;
            }
            else
            {
                XpLabel.Text = "XP: " + Minion.Stats.XP;
            }
            PayLabel.Text = "Pay: " + Minion.Stats.CurrentLevel.Pay.ToString("C0") + " / day" + "\n" + "Wealth: " +
                            Minion.Status.Money.ToString("C");
        }

        public string GetThoughtString(CreatureAI minion)
        {
            if (minion == null) return "";

            string toReturn = "Status: " + minion.Status.Happiness.GetDescription() + "\n";

            foreach (Thought thought in minion.Thoughts)
            {
                string sign = thought.HappinessModifier > 0 ? "+" : "-";
                toReturn += thought.Description + " (" + sign + (int) thought.HappinessModifier + ")" + "\n";
            }
            return toReturn;
        }

        public void SetMinion(CreatureAI myMinion)
        {
            minion = myMinion;
            UpdatePanel();
        }
    }
}