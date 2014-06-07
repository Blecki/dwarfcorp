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

        public void CreateResourcePanels()
        {
            Layout.ClearChildren();

            int numItems = CurrentResources.Count;

            int wItems = LocalBounds.Width / PanelWidth;
            int hItems = LocalBounds.Height / PanelHeight;

            Layout.Rows = hItems;
            Layout.Cols = wItems;

            int itemIndex = 0;
            for(int i = 0; i < numItems; i++)
            {
                ResourceAmount amount = CurrentResources[i];
              
                if(amount.NumResources == 0)
                {
                    continue;
                }

                int r = itemIndex / wItems;
                int c = itemIndex % wItems;

                ImagePanel panel = new ImagePanel(GUI, Layout, amount.ResourceType.Image)
                {
                    KeepAspectRatio = true,
                    ToolTip = amount.ResourceType.ResourceName + "\n" + amount.ResourceType.Description
                };

                Layout.SetComponentPosition(panel, c, r, 1, 1);

                Label panelLabel = new Label(GUI, panel, amount.NumResources.ToString(CultureInfo.InvariantCulture), GUI.SmallFont)
                {
                    Alignment = Drawer2D.Alignment.Bottom,
                    LocalBounds = new Rectangle(0, 0, PanelWidth, PanelHeight),
                    TextColor = Color.White
                };

                itemIndex++;

            }

        }


        public override void Update(Microsoft.Xna.Framework.GameTime time)
        {
            UpdateTimer.Update(time);
            if(UpdateTimer.HasTriggered)
            {
                List<ResourceAmount> currentResources = Faction.ListResources();
                bool isDifferent = CurrentResources.Count != currentResources.Count || currentResources.Where((t, i) => t.NumResources != CurrentResources[i].NumResources).Any();


                if(isDifferent)
                {
                    CreateResourcePanels();
                    CurrentResources = currentResources;
                }
            }
            base.Update(time);
        }
       
    }
}
