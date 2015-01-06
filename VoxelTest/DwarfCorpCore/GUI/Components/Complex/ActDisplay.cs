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
    /// This is a debug display for behavior trees (acts)
    /// </summary>
    public class ActDisplay : GUIComponent
    {
        private Act m_act = null;

        public Act CurrentAct
        {
            get { return m_act; }
            set
            {
                m_act = value;
                InitAct();
            }
        }

        public Color[] DisplayColors { get; set; }
        public int ElementWidth = 50;
        public int ElementHeight = 15;
        private Timer rebuildTimer = new Timer(2.0f, false);

        public struct ActElement
        {
            public Act act;
            public Vector2 position;
        }

        public List<ActElement> Elements { get; set; }

        public ActDisplay(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            DisplayColors = new Color[3];
            DisplayColors[(int) Act.Status.Running] = Color.DarkBlue;
            DisplayColors[(int) Act.Status.Success] = Color.Green;
            DisplayColors[(int) Act.Status.Fail] = Color.Red;

            Elements = new List<ActElement>();
        }


        private void CreateSubtreeRecursive(Act root, ref Vector2 lastPosition, ref Vector2 size)
        {
            if(root == null)
            {
                return;
            }
            else
            {
                ActElement element = new ActElement();
                element.act = root;
                element.position = lastPosition;

                Elements.Add(element);

                lastPosition.Y += ElementHeight;
                lastPosition.X += ElementWidth;

                size += Datastructures.SafeMeasure(GUI.DefaultFont, element.act.Name);
                size.X += ElementWidth;
                size.Y += ElementHeight;

                if(root.Children != null && root.Enumerator.Current != Act.Status.Success)
                {
                    foreach(Act child in root.Children)
                    {
                        CreateSubtreeRecursive(child, ref lastPosition, ref size);
                    }
                }

                lastPosition.X -= ElementWidth;
            }
        }

        private void InitAct()
        {
            Vector2 lastPosition = new Vector2(5, 5);
            Vector2 size = new Vector2(0, 0);

            if(CurrentAct != null && CurrentAct.IsInitialized)
            {
                ActElement element = new ActElement();
                element.act = CurrentAct;
                element.position = lastPosition;
                Elements.Clear();
                Elements.Add(element);

                lastPosition += new Vector2(ElementWidth, ElementHeight);
                size.X += ElementWidth;
                size.Y += ElementHeight;

                if(CurrentAct.Children != null)
                {
                    foreach(Act child in CurrentAct.Children)
                    {
                        CreateSubtreeRecursive(child, ref lastPosition, ref size);
                    }
                }
            }
            else if(CurrentAct != null)
            {
                CurrentAct = null;
            }


            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, (int) size.X, (int) size.Y);
        }

        public override void Update(DwarfTime time)
        {
            rebuildTimer.Update(time);

            if(rebuildTimer.HasTriggered)
            {
                InitAct();
            }

            base.Update(time);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            foreach(ActElement element in Elements)
            {
                string toDraw = element.act.Name;
                Color color = DisplayColors[(int) (element.act.Enumerator.Current)];

                if(element.act.Children.Count == 0 && element.act.Enumerator.Current == Act.Status.Running)
                {
                    toDraw = ">" + toDraw;
                    color = Color.Blue;
                }

                batch.DrawString(GUI.SmallFont, toDraw, element.position + new Vector2(GlobalBounds.X, GlobalBounds.Y), color);
            }

            base.Render(time, batch);
        }
    }

}