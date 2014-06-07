using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class MinionPanel : GUIComponent
    {
        private CreatureAI minion;
        public CreatureAI Minion { get { return minion; } set { SetMinion(value); }}
        public ImagePanel Portrait { get; set; }
        public Label ClassLabel { get; set; }
        public Label XpLabel { get; set; }
        public Label PayLabel { get; set; }
        public Dictionary<string, Label> StatLabels { get; set; }
        public Dictionary<string, MiniBar> StatusBars { get; set; } 

        public MinionPanel(DwarfGUI gui, GUIComponent parent, CreatureAI minion) :
            base(gui, parent)
        {
            InitializePanel();
            Minion = minion;
        }

        public void InitializePanel()
        {
            StatLabels = new Dictionary<string, Label>();
            StatusBars = new Dictionary<string, MiniBar>();
            GridLayout layout = new GridLayout(GUI, this, 10, 8);

            CreateStatsLabel("Dexterity", "DEX:", layout);
            CreateStatsLabel("Strength", "STR:", layout);
            CreateStatsLabel("Wisdom", "WIS:", layout);
            CreateStatsLabel("Constitution", "CON:", layout);
            CreateStatsLabel("Intelligence", "INT:", layout);
            CreateStatsLabel("Size", "SIZ:", layout);

            int i = 0;
            int nx = 3;
            int ny = 2;
            foreach(KeyValuePair<string, Label> label in StatLabels)
            {
                layout.SetComponentPosition(label.Value, (i % nx), (((i - i % nx) / nx) % ny), 1, 1);
                i++;
            }


            CreateStatusBar("Hunger", layout);
            CreateStatusBar("Energy", layout);
            CreateStatusBar("Happiness", layout);
            CreateStatusBar("Health", layout);

            i = 0;
            nx = 2;
            ny = 3;
            foreach (KeyValuePair<string, MiniBar> label in StatusBars)
            {
                layout.SetComponentPosition(label.Value, (i % nx) * 2, (((i - i % nx) / nx) % ny) * 2 + 2, 2, 2);
                i++;
            }

            Portrait = new ImagePanel(GUI, layout, new ImageFrame())
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
            StatLabels["Dexterity"].Text = "DEX: " + Minion.Stats.Dexterity;
            StatLabels["Strength"].Text = "STR: " + Minion.Stats.Strength;
            StatLabels["Wisdom"].Text = "WIS: " + Minion.Stats.Wisdom;
            StatLabels["Constitution"].Text = "CON: " + Minion.Stats.Constitution;
            StatLabels["Intelligence"].Text = "INT: " + Minion.Stats.Intelligence;
            StatLabels["Size"].Text = "SIZ: " + Minion.Stats.Size;

            foreach(var status in Minion.Status.Statuses)
            {
                StatusBars[status.Key].Value = (status.Value.CurrentValue - status.Value.MinValue) / (status.Value.MaxValue - status.Value.MinValue);
            }

            Rectangle rect = Minion.Creature.Sprite.CurrentAnimation.GetCurrentFrameRect();
            Portrait.Image = new ImageFrame(Minion.Creature.Sprite.SpriteSheet, rect);

            ClassLabel.Text = "lvl. " + Minion.Stats.LevelIndex + " " + Minion.Stats.CurrentClass.Name + "\n" + Minion.Stats.CurrentLevel.Name;

            int diff = Minion.Stats.CurrentClass.Levels[Minion.Stats.LevelIndex + 1].XP - Minion.Stats.XP;

            string diffText = "";

            if(diff > 0)
            {
                diffText = "(" + diff + " to next lvl)";
            }
            else
            {
                diffText = "(Overqualified)";
            }

            XpLabel.Text = "XP: " + Minion.Stats.XP + "\n" + diffText;
            PayLabel.Text = "Pay: " + Minion.Stats.CurrentLevel.Pay.ToString("C0") + " / day";
        }

        public void SetMinion(CreatureAI myMinion)
        {
            minion = myMinion;
            UpdatePanel();
            
        }
    }
}
