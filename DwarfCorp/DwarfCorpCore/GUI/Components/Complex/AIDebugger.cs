// AIDebugger.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// This is a debug display for Dwarf AI
    /// </summary>
    public class AIDebugger
    {
        public Panel MainPanel { get; set; }
        public ComboBox DwarfSelector { get; set; }
        public GridLayout Layout { get; set; }
        public Label GoalLabel { get; set; }
        public Label PlanLabel { get; set; }
        public Label AStarPathLabel { get; set; }
        public GameMaster Master { get; set; }
        public Label LastMessages { get; set; }
        private ActDisplay BTDisplay { get; set; }
        private bool m_visible = false;

        public bool Visible
        {
            get { return m_visible; }
            set
            {
                m_visible = value;
                MainPanel.IsVisible = value;
            }
        }

        public AIDebugger(DwarfGUI gui, GameMaster master)
        {
            MainPanel = new Panel(gui, gui.RootComponent);
            Layout = new GridLayout(gui, MainPanel, 13, 1);

            DwarfSelector = new ComboBox(gui, Layout);
            DwarfSelector.OnSelectionModified += DwarfSelector_OnSelectionModified;
            GoalLabel = new Label(gui, Layout, "Script: null", gui.DefaultFont);
            PlanLabel = new Label(gui, Layout, "Plan: null", gui.DefaultFont);
            AStarPathLabel = new Label(gui, Layout, "Astar Path: null", gui.DefaultFont);
            LastMessages = new Label(gui, Layout, "Messages: ", gui.DefaultFont);
            ScrollView btDisplayHolder = new ScrollView(gui, Layout);
            BTDisplay = new ActDisplay(gui, btDisplayHolder);

            Layout.SetComponentPosition(DwarfSelector, 0, 0, 1, 1);
            Layout.SetComponentPosition(GoalLabel, 0, 1, 1, 1);
            Layout.SetComponentPosition(PlanLabel, 0, 2, 1, 1);
            Layout.SetComponentPosition(AStarPathLabel, 0, 3, 1, 1);
            Layout.SetComponentPosition(LastMessages, 0, 4, 1, 2);
            Layout.SetComponentPosition(btDisplayHolder, 0, 6, 1, 6);
            Visible = false;

            int i = 0;
            foreach(CreatureAI component in master.Faction.Minions)
            {
                DwarfSelector.AddValue("Minion " + i);
                i++;
            }


            Master = master;

            MainPanel.LocalBounds = new Rectangle(100, 120, 500, 600);
        }

        private void DwarfSelector_OnSelectionModified(string arg)
        {
            
             int dwarfIndex = DwarfSelector.CurrentIndex;
             if (dwarfIndex != -1 && DwarfSelector.CurrentValue != "")
             {
                 CreatureAI creature = Master.Faction.Minions[dwarfIndex];
                 BTDisplay.CurrentAct = creature.CurrentAct;
             }
             
        }

        public void Update(DwarfTime time)
        {
            if(GameSettings.Default.EnableAIDebugger)
            {
                DwarfSelector.ClearValues();
                int i = 0;
                foreach (CreatureAI component in Master.Faction.Minions)
                {
                    DwarfSelector.AddValue("Minion " + i);
                    i++;
                }

                if(Keyboard.GetState().IsKeyDown(Keys.K))
                {
                    Visible = true;
                }
                else if(Keyboard.GetState().IsKeyDown(Keys.L))
                {
                    Visible = false;
                }
            }

            if(!Visible)
            {
                return;
            }

            int dwarfIndex = DwarfSelector.CurrentIndex;
            if(dwarfIndex != -1 && DwarfSelector.CurrentValue != "")
            {
                CreatureAI creature = Master.Faction.Minions[dwarfIndex];
                
                if (creature == null)
                {
                    return;
                }

                if(creature.CurrentAct != BTDisplay.CurrentAct)
                {
                    BTDisplay.CurrentAct = creature.CurrentAct;
                }


               
                if(creature.CurrentPath != null)
                {
                    AStarPathLabel.Text = "A* Plan: " + creature.CurrentPath.Count;
                }
                else
                {
                    AStarPathLabel.Text = "A* Plan: null";
                }

                LastMessages.Text = "";
                for(int i = creature.MessageBuffer.Count - 1; i > Math.Max(creature.MessageBuffer.Count - 4, 0); i--)
                {
                    LastMessages.Text += creature.MessageBuffer[i] + "\n";
                }

                Drawer3D.DrawBox(creature.Physics.GetBoundingBox(), Color.White, 0.05f);
            }
        }
    }

}