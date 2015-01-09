using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorpCore
{
    [JsonObject(IsReference = true)]
    public class MagicMenu : Window
    {
        public GameMaster Master { get; set; }
        public TabSelector Selector { get; set; }
        public Spell CurrentSpell { get; set; }
        public TabSelector.Tab SpellsTab { get; set; }
        public TabSelector.Tab ResearchTab { get; set; }

        public SpellTreeDisplay SpellTree { get; set; }

        public delegate void OnSpellTriggered(Spell spell);

        public event OnSpellTriggered SpellTriggered;
        public MagicTab KnownSpellTab { get; set; }
        public class MagicTab
        {
            public ImagePanel InfoImage { get; set; }
            public Label InfoTitle { get; set; }
            public Label InfoDescription { get; set; }
            public Label InfoRequirements { get; set; }
            public Button CastButton { get; set; }
            public TabSelector.Tab Tab { get; set; }
            public ScrollView Scroller { get; set; }
        }

        public MagicMenu(DwarfGUI gui, GUIComponent parent, GameMaster master, WindowButtons buttons = WindowButtons.CloseButton) 
            : base(gui, parent, buttons)
        {
            Master = master;
            MinWidth = 512;
            MinHeight = 256;
            Selector = new TabSelector(GUI, this, 2)
            {
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fit,
                LocalBounds = new Rectangle(0, 0, MinWidth, MinHeight)
            };
            SpellTriggered = spell => { };
            CreateSpellsTab();
            CreateResearchTab();
            Selector.SetTab("Known Spells");
        }

        public void SpellClicked(Spell spell)
        {
            spell.OnButtonTriggered();
            SpellTriggered.Invoke(spell);
            IsVisible = false;
        }

        public void CreateMagicTab(MagicTab tab)
        {
            GridLayout tabLayout = new GridLayout(GUI, tab.Tab, 1, 3)
            {
                EdgePadding = 0
            };

            GridLayout infoLayout = new GridLayout(GUI, tabLayout, 4, 2);
            tabLayout.SetComponentPosition(infoLayout, 1, 0, 1, 1);
            tab.InfoImage = new ImagePanel(GUI, infoLayout, (Texture2D)null)
            {
                KeepAspectRatio = true
            };
            infoLayout.SetComponentPosition(tab.InfoImage, 1, 0, 1, 1);

            tab.InfoTitle = new Label(GUI, infoLayout, "", GUI.DefaultFont);
            infoLayout.SetComponentPosition(tab.InfoTitle, 0, 0, 1, 1);

            tab.InfoDescription = new Label(GUI, infoLayout, "", GUI.SmallFont)
            {
                WordWrap = true
            };
            infoLayout.SetComponentPosition(tab.InfoDescription, 0, 1, 1, 1);

            tab.InfoRequirements = new Label(GUI, infoLayout, "", GUI.SmallFont);
            infoLayout.SetComponentPosition(tab.InfoRequirements, 0, 2, 2, 1);

            tab.CastButton = new Button(GUI, infoLayout, "Cast", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Magic]));
            tab.CastButton.OnClicked += CastButton_OnClicked;
            infoLayout.SetComponentPosition(tab.CastButton, 0, 3, 1, 1);

            tab.CastButton.IsVisible = false;

            tab.Scroller = new ScrollView(GUI, tabLayout)
            {
                DrawBorder = true
            };
            tabLayout.SetComponentPosition(tab.Scroller, 0, 0, 1, 1);
            tabLayout.UpdateSizes();
        }

        void CastButton_OnClicked()
        {
            SpellClicked(CurrentSpell);
        }

        private void HoverItem(GridLayout roomLayout, int i)
        {
            roomLayout.HighlightRow(i, new Color(255, 100, 100, 200));
        }

        public void SetupSpellTab()
        {
            KnownSpellTab = new MagicTab
            {
                Tab = SpellsTab
            };
            CreateMagicTab(KnownSpellTab);
            //BuildItemTab.BuildButton.OnClicked += BuildItemButton_OnClicked;
            List<Spell> spells = Master.Spells.GetKnownSpells();

            int numItems = spells.Count();
            int numColumns = 1;
            GridLayout layout = new GridLayout(GUI, KnownSpellTab.Scroller, numItems, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40 * numItems),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            int i = 0;
            foreach (Spell spell in spells)
            {
                Spell currSpell = spell;
                GridLayout itemLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };

                itemLayout.OnClicked += () => ItemTabOnClicked(currSpell);
                int i1 = i;
                itemLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(itemLayout, 0, i, 1, 1);

                ImagePanel icon = new ImagePanel(GUI, itemLayout, spell.Image)
                {
                    KeepAspectRatio = true
                };
                itemLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                Label description = new Label(GUI, itemLayout, spell.Name, GUI.SmallFont)
                {
                    ToolTip = spell.Description
                };
                itemLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        private void ItemTabOnClicked(Spell item)
        {
            CurrentSpell = item;

            KnownSpellTab.InfoTitle.Text = item.Name;
            KnownSpellTab.InfoImage.Image = item.Image;
            KnownSpellTab.InfoDescription.Text = item.Description + "\n" + "  * " + item.Hint;

            KnownSpellTab.CastButton.IsVisible = true;
            string additional = "";


            KnownSpellTab.InfoDescription.Text += additional;

            string requirementsText = "Requires " + item.ManaCost + " mana.";


            KnownSpellTab.InfoRequirements.Text = requirementsText;
        }

        public void InitializeSpells()
        {
            SpellsTab.ClearChildren();

            /*
            ScrollView scroller = new ScrollView(GUI, SpellsTab)
            {
                HeightSizeMode = SizeMode.Fit,
                WidthSizeMode = SizeMode.Fit
            };

            List<Spell> spells = Master.Spells.GetKnownSpells();

            if (spells.Count == 0) return;
            int numCols = 4;
            int numRows = Math.Max(spells.Count / numCols, 1);
            GridLayout spellLayout = new GridLayout(GUI, scroller, numRows, numCols)
            {
                HeightSizeMode = SizeMode.Fixed,
                WidthSizeMode = SizeMode.Fixed,
                LocalBounds = new Rectangle(0, 0, numCols * 48, numRows * 48)
            };

            int i = 0;
            foreach (Spell spell in spells)
            {
                int row = i / numCols;
                int col = i % numCols;


                Button imageButton = new Button(GUI, spellLayout, spell.Name, GUI.SmallFont,
                    Button.ButtonMode.ImageButton, spell.Image)
                {
                    ToolTip = spell.Description
                };
                Spell toTrigger = spell;
                imageButton.OnClicked += () => SpellClicked(toTrigger);

                spellLayout.SetComponentPosition(imageButton, col, row, 1, 1);
                i++;
            }
            spellLayout.UpdateSizes();
            */
            SetupSpellTab();
        }

        public void CreateSpellsTab()
        {
            SpellsTab = Selector.AddTab("Known Spells");
            SpellsTab.OnSelected += SpellsTab_OnClicked;
            InitializeSpells();
        }

        void SpellsTab_OnClicked()
        {
            InitializeSpells();
        }

        public void CreateResearchTab()
        {
            ResearchTab = Selector.AddTab("Research Spells");
            SpellTree = new SpellTreeDisplay(GUI, ResearchTab, Master.Spells)
            {
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fit
            };

        }


        
    }
}
