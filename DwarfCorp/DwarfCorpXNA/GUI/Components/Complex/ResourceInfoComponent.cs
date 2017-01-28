// ResourceInfoComponent.cs
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
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class ResourceInfoComponent : GUIComponent
    {
        public Faction Faction { get; set; }
        public Timer UpdateTimer { get; set; }
        public List<ResourceAmount> CurrentResources { get; set; }
        public GridLayout Layout { get; set; }
        public int PanelWidth { get; set; }
        public int PanelHeight { get; set; }
        public ResourceInfoComponent(DwarfGUI gui, GUIComponent parent, Faction faction) : base(gui, parent)
        {
            Faction = faction;
            UpdateTimer = new Timer(0.5f, false);
            Layout = new GridLayout(gui, this, 1, 1);
            CurrentResources = new List<ResourceAmount>();
            PanelWidth = 40;
            PanelHeight = 40;
        }

        public void CleanUp()
        {
            Faction = null;
        }

        public void CreateResourcePanels()
        {
            Layout.ClearChildren();

            int numItems = CurrentResources.Count;

            int wItems = LocalBounds.Width / PanelWidth - 1;
            int hItems = LocalBounds.Height / PanelHeight;

            Layout.Rows = hItems;
            Layout.Cols = wItems;
            List<ImagePanel> panels = new List<ImagePanel>();
            List<int> counts = new List<int>();
            List<Label> labels = new List<Label>();
            int itemIndex = 0;
            for(int i = 0; i < numItems; i++)
            {
                ResourceAmount amount = CurrentResources[i];
              
                if(amount.NumResources == 0)
                {
                    continue;
                }


                bool exists = false;
                int k = 0;
                foreach (ImagePanel imgPanel in panels)
                {
                    if (imgPanel.Image.Equals(amount.ResourceType.Image))
                    {
                        imgPanel.ToolTip = imgPanel.ToolTip + "\n" +
                                             "* " + amount.NumResources.ToString() + " " + amount.ResourceType.ResourceName + "\n" +
                                           amount.ResourceType.Description + "\n Props: " +
                                           amount.ResourceType.GetTagDescription(", ");
                        exists = true;

                        counts[k] += amount.NumResources;
                        labels[k].Text = counts[k].ToString();
                        break;
                    }
                    k++;
                }

                if (exists) continue;

                int r = itemIndex / wItems;
                int c = itemIndex % wItems;

                ImagePanel panel = new ImagePanel(GUI, Layout, amount.ResourceType.Image)
                {
                    KeepAspectRatio = true,
                    ToolTip = "* " + amount.NumResources.ToString() + " " + amount.ResourceType.ResourceName + "\n" + amount.ResourceType.Description + "\n Props: " + amount.ResourceType.GetTagDescription(", "),
                    Tint = amount.ResourceType.Tint
                };

                Layout.SetComponentPosition(panel, c, r, 1, 1);

                Label panelLabel = new Label(GUI, panel, amount.NumResources.ToString(CultureInfo.InvariantCulture), GUI.SmallFont)
                {
                    Alignment = Drawer2D.Alignment.Bottom,
                    LocalBounds = new Rectangle(0, 0, PanelWidth, PanelHeight),
                    TextColor = Color.White
                };

                panels.Add(panel);
                labels.Add(panelLabel);
                counts.Add(amount.NumResources);


                itemIndex++;

            }

        }


        public override void Update(DwarfTime time)
        {
            UpdateTimer.Update(time);
            if(UpdateTimer.HasTriggered)
            {
                List<ResourceAmount> currentResources = Faction.ListResources().Values.ToList();
                bool isDifferent = CurrentResources.Count != currentResources.Count || currentResources.Where((t, i) => t.NumResources != CurrentResources[i].NumResources).Any();


                if(isDifferent)
                {
                    CurrentResources.Clear();
                    CurrentResources.AddRange(currentResources);
                    CreateResourcePanels();
                }
            }
            base.Update(time);
        }
       
    }
}
