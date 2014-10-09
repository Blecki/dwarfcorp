using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class BuildMenu : Window
    {
        public GameMaster Master { get; set; }
        public TabSelector Selector { get; set; }
        public Label InfoTitle { get; set; }
        public Label InfoDescription { get; set; }
        public Label InfoRequirements { get; set; }
        public RoomData SelectedRoom { get; set; }
        public Button BuildRoomButton { get; set; }

        public BuildMenu(DwarfGUI gui, GUIComponent parent, GameMaster faction) :
            base(gui, parent, WindowButtons.CloseButton)
        {
            GridLayout layout = new GridLayout(GUI, this, 1, 1);
            Master = faction;
            Selector = new TabSelector(GUI, layout, 3);
            layout.SetComponentPosition(Selector, 0, 0, 1, 1);
            TabSelector.Tab roomTab = Selector.AddTab("Rooms");
            CreateRoomTab(roomTab);

            TabSelector.Tab craftTab = Selector.AddTab("Items");
            TabSelector.Tab wallTab = Selector.AddTab("Walls");
            Selector.SetTab(roomTab.Name);
            MinWidth = 512;
            MinHeight = 256;
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
        }

        public void RoomTabOnClicked(RoomData room)
        {
            SelectedRoom = room;

            InfoTitle.Text = room.Name;
            InfoDescription.Text = room.Description;

            BuildRoomButton.IsVisible = true;
            string additional = "";

            if (!room.CanBuildAboveGround)
            {
                additional += "\n* Must be built below ground.";
            }

            if (!room.CanBuildBelowGround)
            {
                additional += "\n* Must be built above ground.";
            }

            if (room.MustBeBuiltOnSoil)
            {
                additional += "\n* Must be built on soil.";
            }

            InfoDescription.Text += additional;

            string requirementsText = "Requires (per 4 tiles):\n";

            foreach (KeyValuePair<ResourceLibrary.ResourceType, ResourceAmount> pair in room.RequiredResources)
            {
                requirementsText += pair.Key + ": " + pair.Value.NumResources + "\n";
            }


            if (room.RequiredResources.Count == 0)
            {
                requirementsText += "Nothing";
            }

            InfoRequirements.Text = requirementsText;
        }

        public void CreateRoomTab(TabSelector.Tab tab)
        {
            GridLayout tabLayout = new GridLayout(GUI, tab, 1, 2)
            {
                EdgePadding = 0
            };

            List<string> roomTypes = RoomLibrary.GetRoomTypes().ToList();

            int numRooms = roomTypes.Count();
            int numColumns = 1;

            GridLayout infoLayout = new GridLayout(GUI, tabLayout, 4, 2);
            tabLayout.SetComponentPosition(infoLayout, 1, 0, 1, 1);

            InfoTitle = new Label(GUI, infoLayout, "", GUI.DefaultFont)
            {
                StrokeColor = Color.Transparent
            };
            infoLayout.SetComponentPosition(InfoTitle, 0, 0, 1, 2);

            InfoDescription = new Label(GUI, infoLayout, "", GUI.SmallFont);
            infoLayout.SetComponentPosition(InfoDescription, 0, 1, 2, 1);

            InfoRequirements = new Label(GUI, infoLayout, "", GUI.SmallFont);
            infoLayout.SetComponentPosition(InfoRequirements, 0, 2, 2, 1);

            BuildRoomButton = new Button(GUI, infoLayout, "Build", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Build]));
            infoLayout.SetComponentPosition(BuildRoomButton, 1, 3, 1, 1);

            BuildRoomButton.OnClicked += BuildRoomButton_OnClicked;
            BuildRoomButton.IsVisible = false;

            ScrollView scrollView = new ScrollView(GUI, tabLayout)
            {
                DrawBorder = true
            };
            tabLayout.SetComponentPosition(scrollView, 0, 0, 1, 1);
            tabLayout.UpdateSizes();

            GridLayout layout = new GridLayout(GUI, scrollView, numRooms, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40 * numRooms),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            int i = 0;
            foreach (string roomType in roomTypes)
            {
                RoomData room = RoomLibrary.GetData(roomType);

                GridLayout roomLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed, 
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };
               
                roomLayout.OnClicked += () => RoomTabOnClicked(room);
                int i1 = i;
                roomLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(roomLayout, 0, i, 1, 1);
               
                ImagePanel icon = new ImagePanel(GUI, roomLayout, room.Icon)
                {
                    KeepAspectRatio = true
                };
                roomLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                Label description = new Label(GUI, roomLayout, room.Name, GUI.SmallFont)
                {
                    ToolTip = room.Description
                };
                roomLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            tabLayout.UpdateSizes();
            layout.UpdateSizes();
        }

        void BuildRoomButton_OnClicked()
        {
            IsVisible = false;
            Master.Faction.RoomBuilder.CurrentRoomData = SelectedRoom;
            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            Master.Faction.WallBuilder.CurrentVoxelType = null;
            Master.Faction.CraftBuilder.IsEnabled = false;
            Master.CurrentToolMode = GameMaster.ToolMode.Build;
        }

        private void HoverItem(GridLayout roomLayout, int i)
        {
            roomLayout.HighlightRow(i, new Color(255, 100, 100, 200));
        }
    }
}
