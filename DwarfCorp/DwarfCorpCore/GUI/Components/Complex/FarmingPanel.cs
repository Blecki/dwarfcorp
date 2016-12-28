using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    public class FarmingPanel : Window
    {
        public delegate void HarvestDelegate();

        public delegate void PlantDelegate(string plantType, string resource);

        public delegate void TillDelegate();

        public FarmingPanel(DwarfGUI gui, GUIComponent parent, WindowButtons buttons = WindowButtons.CloseButton)
            : base(gui, parent, buttons)
        {
            MinWidth = 512;
            MinHeight = 256;
            Setup();
        }

        public GridLayout Layout { get; set; }
        public Button TillButton { get; set; }
        public Button PlantButton { get; set; }
        public ComboBox PlantSelector { get; set; }
        public Button HarvestButton { get; set; }
        public event TillDelegate OnTill;
        public event PlantDelegate OnPlant;
        public event HarvestDelegate OnHarvest;

        public void Setup()
        {
            Children.Clear();
            Layout = new GridLayout(GUI, this, 3, 3);

            TillButton = new Button(GUI, Layout, "Till Soil", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            TillButton.OnClicked += TillButton_OnClicked;
            Layout.SetComponentPosition(TillButton, 0, 0, 1, 1);

            PlantButton = new Button(GUI, Layout, "Plant", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(PlantButton, 0, 1, 1, 1);
            PlantButton.OnClicked += PlantButton_OnClicked;

            PlantSelector = new ComboBox(GUI, Layout);
            List<ResourceAmount> resources =
                PlayState.Master.Faction.ListResourcesWithTag(Resource.ResourceTags.Plantable);
            foreach (ResourceAmount resource in resources)
            {
                if (resource.NumResources > 0)
                {
                    PlantSelector.AddValue(resource.ResourceType.Type);
                }
            }

            if (resources.Count > 0 && PlantSelector.Values.Count > 0)
            {
                PlantSelector.CurrentIndex = 0;
                PlantSelector.CurrentValue = PlantSelector.Values.ElementAt(0);
            }
            else
            {
                PlantSelector.AddValue("<No plantable items!>");
            }

            Layout.SetComponentPosition(PlantSelector, 1, 1, 2, 1);

            HarvestButton = new Button(GUI, Layout, "Harvest", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(HarvestButton, 0, 2, 1, 1);
            HarvestButton.OnClicked += HarvestButton_OnClicked;
        }

        private void HarvestButton_OnClicked()
        {
            if (OnHarvest != null)
            {
                OnHarvest.Invoke();
                CloseButton.InvokeClick();
            }
        }

        private void PlantButton_OnClicked()
        {
            if (OnPlant != null && !string.IsNullOrEmpty(PlantSelector.CurrentValue) &&
                PlantSelector.CurrentValue != "<No plantable items!>")
            {
                OnPlant.Invoke(PlantSelector.CurrentValue, PlantSelector.CurrentValue);
                CloseButton.InvokeClick();
            }
            else
            {
                GUI.ToolTipManager.Popup("Nothing to plant.");
            }
        }

        private void TillButton_OnClicked()
        {
            if (OnTill != null)
            {
                OnTill.Invoke();
                CloseButton.InvokeClick();
            }
        }
    }
}