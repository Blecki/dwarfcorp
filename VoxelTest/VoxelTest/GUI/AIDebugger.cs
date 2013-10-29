using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
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
        ActDisplay BTDisplay { get; set; }
        bool m_visible = false;

        public bool Visible { get { return m_visible; } set { m_visible = value; MainPanel.IsVisible = value; } }

        public AIDebugger(SillyGUI gui, GameMaster master)
        {
            MainPanel = new Panel(gui, gui.RootComponent);
            Layout = new GridLayout(gui, MainPanel, 13, 1);
            
            DwarfSelector = new ComboBox(gui, Layout);
            DwarfSelector.OnSelectionModified += new ComboBoxSelector.Modified(DwarfSelector_OnSelectionModified);
            GoalLabel = new Label(gui, Layout, "Goal: null", gui.DefaultFont);
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
            foreach (CreatureAIComponent component in master.Minions)
            {
                DwarfSelector.AddValue("Minion " + i);
                i++;
            }


            Master = master;

            MainPanel.LocalBounds = new Rectangle(20, 100, 500, 600);
        }

        void DwarfSelector_OnSelectionModified(string arg)
        {
            /*
             int dwarfIndex = DwarfSelector.CurrentIndex;
             if (dwarfIndex != -1 && DwarfSelector.CurrentValue != "")
             {
                 CreatureAIComponent creature = Master.Minions[dwarfIndex];
                 BTDisplay.CurrentAct = creature.CurrentAct;
             }
             */
        }

        public void Update(GameTime time)
        {

            if (GameSettings.Default.EnableAIDebugger)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.K))
                {
                    Visible = true;
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.L))
                {
                    Visible = false;
                }
            }
             
            if(!Visible)
            {
                return;
            }

            int dwarfIndex = DwarfSelector.CurrentIndex;
            if (dwarfIndex != -1 && DwarfSelector.CurrentValue != "")
            {
                CreatureAIComponent creature = Master.Minions[dwarfIndex];
              
                Goal g = creature.CurrentGoal;

                if (creature.CurrentAct != BTDisplay.CurrentAct)
                {
                    BTDisplay.CurrentAct = creature.CurrentAct;
                }

                if (g != null && creature != null)
                {
                    GoalLabel.Text = g.Name;
                    List<Action> plan = creature.CurrentActionPlan;

                    if (plan != null && plan.Count > 0)
                    {
                        PlanLabel.Text = "Action: " + plan[creature.CurrentActionIndex].Name;
                    }
                    else
                    {
                        PlanLabel.Text = "Action: null";
                    }

                    if (creature.CurrentPath != null)
                    {
                        AStarPathLabel.Text = "A* Plan: " + creature.CurrentPath.Count;
                    }
                    else
                    {
                        AStarPathLabel.Text = "A* Plan: null";
                    }

                    LastMessages.Text = "";
                    for (int i = creature.MessageBuffer.Count - 1; i > Math.Max(creature.MessageBuffer.Count - 4, 0); i--)
                    {
                        LastMessages.Text += creature.MessageBuffer[i] + "\n";
                    }

                    SimpleDrawing.DrawBox(creature.Physics.GetBoundingBox(), Color.White, 0.05f);
                }


            }

            
        }



    }
}
