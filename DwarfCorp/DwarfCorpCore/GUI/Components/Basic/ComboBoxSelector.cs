// ComboBoxSelector.cs
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
    /// This GUI component is the list found inside the ComboBox
    /// </summary>
    public class ComboBoxSelector : GUIComponent
    {
        public class Entry
        {
            public string LocalName { get; set; }
            public string GlobalName { get; set; }
            public List<Entry> Children { get; set; }
            public ComboBoxSelector ChildSelector { get; set; }
        }

        public delegate void Modified(string arg);

        public event Modified OnSelectionModified;

        private List<Entry> Values { get; set; }
        private List<List<Entry>> Columns { get; set; }
        public Entry CurrentValue { get; set; }
        private int PixelsPerValue { get; set; }
        private ComboBox Box { get; set; }
        private Timer ClickTimer { get; set; }
        public bool Drawn { get; set; }
        public SpriteFont Font { get; set; }

        public bool IsDead = false;
        private int ColumnWidth = 0;


        public static List<Entry> CreateEntries(List<string> values)
        {
            Dictionary<string, Entry> entryDict  = new Dictionary<string, Entry>();
            List<Entry> toReturn = new List<Entry>();
            foreach (string s in values)
            {
                List<string> subvalues = s.Split('/').ToList();

                Entry entry = null;


                if (entryDict.ContainsKey(subvalues[0]))
                {
                    entry = entryDict[subvalues[0]];
                }
                else
                {
                    entry = new Entry()
                    {
                        GlobalName = s,
                        LocalName = subvalues[0]
                    };
                    toReturn.Add(entry);
                    entryDict[subvalues[0]] = entry;
                    if (subvalues.Count == 1)
                    {
                        continue;
                    }
                    else
                    {
                        entry.LocalName += " >";
                    }
                }

                string curr = subvalues.FirstOrDefault();
                Entry parentEntry = entry;
                Entry subentry = entry;
                for (int i = 1; i < subvalues.Count; i++)
                {
                    string subvalue = subvalues[i];
                    curr += "/" + subvalue;

                    if (entryDict.ContainsKey(curr))
                    {
                        parentEntry = subentry;
                        subentry = entryDict[curr];
                    }
                    else
                    {
                        parentEntry = subentry;
                        subentry = new Entry()
                        {
                            GlobalName = curr,
                            LocalName = subvalue
                        };

                        if (subvalues.Count > 2)
                        {
                            subentry.LocalName += " >";
                        }

                        if (parentEntry.Children == null)
                        {
                            parentEntry.Children = new List<Entry>();
                        }

                        parentEntry.Children.Add(subentry);
                        entryDict[curr] = subentry;
                    }
                }
            }

            return toReturn;
        }

        public ComboBoxSelector(DwarfGUI gui, ComboBox parent, List<Entry> values, int posX = -1, int posY = -1) :
            base(gui, parent)
        {
			MaxHeight = 500;
            Font = gui.DefaultFont;
            Columns = new List<List<Entry>>();
            Values = values;
            CurrentValue = values.FirstOrDefault();

            int height = 0;
            Columns.Add(new List<Entry>());
            int currentColumn = 0;
            int columnWidth = parent.GlobalBounds.Width - 37;
            ColumnWidth = columnWidth;
            int bestHeight = 0;
            int x = posX > 0 ? posX : 0;
            Box = parent;
            int y = posY > 0 ? posY : parent.GlobalBounds.Height / 2 + GUI.Skin.TileHeight / 2;
            foreach (Entry s in values)
            {
                List<Entry> column = Columns[currentColumn];
                Vector2 measure = Datastructures.SafeMeasure(Font, s.LocalName);
                height += (int) measure.Y;
                column.Add(s);

                if(height > bestHeight)
                {
                    bestHeight = height;
                }

                if(height >= MaxHeight || height + y + Box.GlobalBounds.Y + 32 > GameSettings.Default.ResolutionY)
                {
                    height = 0;
                    Columns.Add(new List<Entry>());
                    currentColumn++;
                    columnWidth += ColumnWidth;
                }
            }

            List<List<Entry>> removals = new List<List<Entry>>();

            foreach (List<Entry> column in Columns)
            {
                if(column.Count == 0)
                {
                    removals.Add(column);
                }
            }

            foreach (List<Entry> column in removals)
            {
                Columns.Remove(column);
                columnWidth -= ColumnWidth;
            }

            bestHeight += 15;

            LocalBounds = new Rectangle(x,y,columnWidth, bestHeight);

            ClickTimer = new Timer(0.1f, true, Timer.TimerMode.Real);
            InputManager.MouseClickedCallback += InputManager_MouseClickedCallback;
            Drawn = true;
        }

        private Vector2 MeasureColumn(IEnumerable<Entry> column)
        {
            Vector2 toReturn = Vector2.Zero;
            foreach (Entry s in column)
            {
                toReturn.Y += Datastructures.SafeMeasure(Font, s.LocalName).Y;
                toReturn.X = (float)Math.Max(toReturn.X, Datastructures.SafeMeasure(Font, s.LocalName).X);
            }
            return toReturn;
        }


        private void InputManager_MouseClickedCallback(InputManager.MouseButton button)
        {
            if(ClickTimer.HasTriggered && !IsDead)
            {
                if (CurrentValue != null && (CurrentValue.Children == null || CurrentValue.Children.Count == 0))
                {
                    OnSelectionModified.Invoke(CurrentValue.GlobalName);
                    Parent.RemoveChild(this);
                    IsDead = true;
                    InputManager.MouseClickedCallback -= InputManager_MouseClickedCallback;
                }
                else if (CurrentValue != null)
                {
                    if (CurrentValue.ChildSelector == null || CurrentValue.ChildSelector.IsDead)
                    {
                        MouseState mouse = Mouse.GetState();
                        CurrentValue.ChildSelector = new ComboBoxSelector(GUI, Box, CurrentValue.Children, mouse.X - Box.GlobalBounds.X, mouse.Y - Box.GlobalBounds.Y);
                        GUI.FocusComponent = CurrentValue.ChildSelector;
                        Entry value = CurrentValue;
                        CurrentValue.ChildSelector.OnSelectionModified += (string arg) => ChildSelector_OnSelectionModified(arg, value);
                    }
                }
            }
        }

        void ChildSelector_OnSelectionModified(string arg, Entry entry)
        {
            CurrentValue = entry;
            OnSelectionModified.Invoke(arg);
            Parent.RemoveChild(this);
            IsDead = true;
            InputManager.MouseClickedCallback -= InputManager_MouseClickedCallback;
            GUI.FocusComponent = null;
        }


        public override void Update(DwarfTime time)
        {
            ClickTimer.Update(time);
            if(IsMouseOver && !IsDead)
            {
                //GUI.FocusComponent = Parent;
                MouseState mouse = Mouse.GetState();

                float normalizedX = Math.Min(Math.Max(((float) mouse.X - (float) GlobalBounds.X) / GlobalBounds.Width, 0), 1.0f);
                int nearestColumn = (int) (normalizedX * Columns.Count);

                if(nearestColumn >= 0 && nearestColumn < Columns.Count)
                {
                    Vector2 colMeasure = MeasureColumn(Columns[nearestColumn]);
                    float normalizedY = Math.Min(Math.Max(((float) mouse.Y - (float) GlobalBounds.Y) / colMeasure.Y, 0), 1.0f);
                    int nearestRow = (int) (normalizedY * Columns[nearestColumn].Count);

                    if(nearestRow >= 0 && nearestRow < Columns[nearestColumn].Count)
                    {
                        CurrentValue = Columns[nearestColumn][nearestRow];
                        Box.CurrentValue = CurrentValue.GlobalName;
                    }
                }
            }
            else
            {
                //GUI.FocusComponent = null;
            }


            base.Update(time);
        }


        public override void Render(DwarfTime time, Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
        {
            if(Drawn)
            {
                if(!GUI.DrawAfter.Contains(this))
                {
                    GUI.DrawAfter.Add(this);
                    Drawn = false;
                }
            }
            else
            {
                GUI.Skin.RenderButton(GlobalBounds, batch);


                int x = 0;
                foreach(List<Entry> column in Columns)
                {
                    if(column.Count == 0)
                    {
                        continue;
                    }

                    float columnMeasure = MeasureColumn(column).Y;
                    PixelsPerValue = (int) columnMeasure / column.Count;
                    int h = 0;

                    foreach(Entry s in column)
                    {
                        Vector2 measure = Datastructures.SafeMeasure(Font, s.LocalName);

                        Color c = Color.Black;

                        if(s == CurrentValue)
                        {
                            c = Color.DarkRed;
                        }

                        Drawer2D.DrawAlignedText(batch, s.LocalName, Font, c, Drawer2D.Alignment.Left, new Rectangle(GlobalBounds.X + 10 + x, GlobalBounds.Y + h + 5, GlobalBounds.Width, (int)measure.Y + 5));

                        h += PixelsPerValue;
                    }

                    x += ColumnWidth;
                }
                Drawn = true;
                base.Render(time, batch);
            }
        }
    }

}