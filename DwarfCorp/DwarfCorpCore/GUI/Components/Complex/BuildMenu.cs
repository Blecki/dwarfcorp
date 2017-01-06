// BuildMenu.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class BuildMenu : Window
    {
        public GameMaster Master { get; set; }
        public TabSelector Selector { get; set; }
        public class BuildTab
        {
            public ImagePanel InfoImage { get; set; }
            public Label InfoTitle { get; set; }
            public Label InfoDescription { get; set; }
            public Label InfoRequirements { get; set; }
            public Button BuildButton { get; set; }
            public TabSelector.Tab Tab { get; set; }
            public ScrollView Scroller { get; set; }
            public FormLayout SelectedResourcesLayout { get; set; }
            public List<ComboBox> SelectedResourceBoxes { get; set; } 
        }

        [Flags]
        public enum BuildType
        {
            Room = 1,
            Wall = 2,
            Item = 4,
            Craft = 8,
            Cook = 16
        }

        public BuildType Build { get; set; }

        public BuildTab BuildResourceTab { get; set; }
        public BuildTab BuildRoomTab { get; set; }
        public BuildTab BuildItemTab { get; set; }
        public BuildTab BuildWallTab { get; set; }
        public RoomData SelectedRoom { get; set; }
        public CraftItem SelectedItem { get; set; }
        public VoxelType SelectedWall { get; set; }


        public BuildMenu(DwarfGUI gui, GUIComponent parent, GameMaster faction, BuildType type) :
            base(gui, parent, WindowButtons.CloseButton)
        {
            GridLayout layout = new GridLayout(GUI, this, 1, 1);
            Master = faction;
            Selector = new TabSelector(GUI, layout, 4);
            layout.SetComponentPosition(Selector, 0, 0, 1, 1);
            Build = type;

            if (type.HasFlag(BuildType.Room))
                SetupBuildRoomTab();
            
            if (type.HasFlag(BuildType.Item))
                SetupBuildItemTab();
            
            if (type.HasFlag(BuildType.Craft) || type.HasFlag(BuildType.Cook))
                SetupBuildResourceTab();
            
            if (type.HasFlag(BuildType.Wall))
                SetupBuildWallTab();
            

            if (Selector.Tabs.Count > 0)
                Selector.SetTab(Selector.Tabs.First().Key);

            MinWidth = 512;
            MinHeight = 256;
        }

        private void SetupBuildResourceTab()
        {
            bool hasCook = Build.HasFlag(BuildType.Cook);
            string name = Build.HasFlag(BuildType.Cook) ? "Food" : "Crafts";
            BuildResourceTab = new BuildTab
            {
                Tab = Selector.AddTab(name),
                SelectedResourceBoxes = new List<ComboBox>()
            };
            CreateBuildTab(BuildResourceTab, hasCook ? BuildType.Cook : BuildType.Craft);
            BuildResourceTab.BuildButton.OnClicked += BuildResource_OnClicked;
            List<CraftItem> items = CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Resource).ToList();

            int numItems = items.Count();
            int numColumns = 1;
            GridLayout layout = new GridLayout(GUI, BuildResourceTab.Scroller, numItems, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40 * numItems),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            int i = 0;
            foreach (CraftItem itemType in items)
            {
                CraftItem item = itemType;
                bool isEdible = ResourceLibrary.Resources.ContainsKey(item.ResourceCreated) &&
                    ResourceLibrary.Resources[item.ResourceCreated].Tags.Contains(Resource.ResourceTags.Edible);
                if (!hasCook && isEdible)
                    continue;
                else if (hasCook && !isEdible)
                    continue;

                GridLayout itemLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };
                if (i == 0)
                {
                    ResourceTabOnClicked(item);
                }

                itemLayout.OnClicked += () => ResourceTabOnClicked(item);
                int i1 = i;
                itemLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(itemLayout, 0, i, 1, 1);

                ImagePanel icon = new ImagePanel(GUI, itemLayout, item.Image)
                {
                    KeepAspectRatio = true,
                    MinWidth = 32,
                    MinHeight = 32
                };
                itemLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                Label description = new Label(GUI, itemLayout, item.Name, GUI.SmallFont)
                {
                    ToolTip = item.Description
                };
                itemLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();

        }

        private void BuildResource_OnClicked()
        {
            List<Task> assignments = new List<Task>();
            SelectedResource.SelectedResources = new List<ResourceAmount>();

            for (int i = 0; i < BuildResourceTab.SelectedResourceBoxes.Count; i++)
            {
                ComboBox box = BuildResourceTab.SelectedResourceBoxes[i];

                if (box.CurrentValue == "<Not enough!>")
                {
                    return;
                }

                Quantitiy<Resource.ResourceTags> tags = SelectedResource.RequiredResources[i];
                SelectedResource.SelectedResources.Add(new ResourceAmount(box.CurrentValue, tags.NumResources));
            }

            assignments.Add(new CraftResourceTask(SelectedResource));
            if (assignments.Count > 0)
            {
                TaskManager.AssignTasks(assignments, Faction.FilterMinionsWithCapability(Master.SelectedMinions, GameMaster.ToolMode.Craft));
            }
        }


        public override void Update(DwarfTime time)
        {
            base.Update(time);
        }

        public void RoomTabOnClicked(RoomData room)
        {
            SelectedRoom = room;

            BuildRoomTab.InfoTitle.Text = room.Name;
            BuildRoomTab.InfoImage.Image = room.Icon;
            BuildRoomTab.InfoDescription.Text = room.Description;

            BuildRoomTab.BuildButton.IsVisible = true;
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

            BuildRoomTab.InfoDescription.Text += additional;

            string requirementsText = "Requires (per 4 tiles):\n";

            foreach (KeyValuePair<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> pair in room.RequiredResources)
            {
                requirementsText += pair.Key + ": " + pair.Value.NumResources + "\n";
            }


            if (room.RequiredResources.Count == 0)
            {
                requirementsText += "Nothing";
            }

            BuildRoomTab.InfoRequirements.Text = requirementsText;
        }

        private void WallTabOnClicked(VoxelType wall)
        {
            SelectedWall = wall;

            BuildWallTab.InfoTitle.Text = wall.Name + " Wall";
            BuildWallTab.InfoDescription.Text = "";

            BuildWallTab.BuildButton.IsVisible = true;
            string additional = "";

            additional += "* Wall strength: " + wall.StartingHealth;

            BuildWallTab.InfoDescription.Text += additional;

            string requirementsText = "Requires : " + ResourceLibrary.Resources[wall.ResourceToRelease].ResourceName;
            BuildWallTab.InfoRequirements.Text = requirementsText;
        }
        public void SetupBuildRoomTab()
        {
            BuildRoomTab = new BuildTab()
            {
                Tab = Selector.AddTab("Rooms")
            };
        
            CreateBuildTab(BuildRoomTab, BuildType.Room);
            BuildRoomTab.BuildButton.OnClicked += BuildRoomButton_OnClicked;
            List<string> roomTypes = RoomLibrary.GetRoomTypes().ToList();

            int numRooms = roomTypes.Count();
            int numColumns = 1;
            GridLayout layout = new GridLayout(GUI, BuildRoomTab.Scroller, numRooms, numColumns)
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

                if (i == 0)
                {
                    RoomTabOnClicked(room);
                }

                roomLayout.OnClicked += () => RoomTabOnClicked(room);
                int i1 = i;
                roomLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(roomLayout, 0, i, 1, 1);

                ImagePanel icon = new ImagePanel(GUI, roomLayout, room.Icon)
                {
                    KeepAspectRatio = true,
                    MinWidth = 32,
                    MinHeight = 32
                };
                roomLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                Label description = new Label(GUI, roomLayout, room.Name, GUI.SmallFont)
                {
                    ToolTip = room.Description
                };
                roomLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        private void SetupBuildItemTab()
        {
            BuildItemTab = new BuildTab
            {
                Tab = Selector.AddTab("Objects")
            };
            BuildItemTab.SelectedResourceBoxes = new List<ComboBox>();
            CreateBuildTab(BuildItemTab, BuildType.Item);
            BuildItemTab.BuildButton.OnClicked += BuildItemButton_OnClicked;
            List<CraftItem> items = CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Object).ToList();

            int numItems = items.Count();
            int numColumns = 1;
            GridLayout layout = new GridLayout(GUI, BuildItemTab.Scroller, numItems, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40 * numItems),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            int i = 0;
            foreach (CraftItem itemType in items)
            {
                CraftItem item = itemType;
                GridLayout itemLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };
                if (i == 0)
                    ItemTabOnClicked(item);
                itemLayout.OnClicked += () => ItemTabOnClicked(item);
                int i1 = i;
                itemLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(itemLayout, 0, i, 1, 1);

                ImagePanel icon = new ImagePanel(GUI, itemLayout, item.Image)
                {
                    KeepAspectRatio = true,
                    MinWidth = 32,
                    MinHeight = 32
                };
                itemLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                Label description = new Label(GUI, itemLayout, item.Name, GUI.SmallFont)
                {
                    ToolTip = item.Description
                };
                itemLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        private void BuildItemButton_OnClicked()
        {
            SelectedItem.SelectedResources = new List<ResourceAmount>();

            for (int i = 0; i < BuildItemTab.SelectedResourceBoxes.Count; i++)
            {
                ComboBox box = BuildItemTab.SelectedResourceBoxes[i];

                if (box.CurrentValue == "<Not enough!>")
                {
                    return;
                }

                Quantitiy<Resource.ResourceTags> tags = SelectedItem.RequiredResources[i];
                SelectedItem.SelectedResources.Add(new ResourceAmount(box.CurrentValue, tags.NumResources));
            }
            IsVisible = false;
            Master.Faction.RoomBuilder.CurrentRoomData = null;
            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            Master.Faction.WallBuilder.CurrentVoxelType = null;
            Master.Faction.CraftBuilder.IsEnabled = true;
            Master.Faction.CraftBuilder.CurrentCraftType = SelectedItem;
            Master.CurrentToolMode = GameMaster.ToolMode.Build;
            GUI.ToolTipManager.Popup("Click and drag to build " + SelectedItem.Name);
        }

        public CraftItem SelectedResource { get; set; }


        private void ResourceTabOnClicked(CraftItem item)
        {
            bool hasCook = Build.HasFlag(BuildType.Cook);

            SelectedResource = item;

            BuildResourceTab.InfoTitle.Text = item.Name;
            BuildResourceTab.InfoImage.Image = item.Image;
            BuildResourceTab.InfoDescription.Text = item.Description;
            BuildResourceTab.SelectedResourceBoxes = new List<ComboBox>();
            BuildResourceTab.BuildButton.IsVisible = true;
            string additional = "";


            BuildResourceTab.InfoDescription.Text += additional;
            if (BuildResourceTab.SelectedResourcesLayout != null)
                BuildResourceTab.SelectedResourcesLayout.ClearChildren();
            BuildResourceTab.SelectedResourceBoxes.Clear();
            BuildResourceTab.SelectedResourcesLayout = new FormLayout(GUI, BuildResourceTab.InfoRequirements)
            {
                EdgePadding = 0,
                LabelFont = GUI.SmallFont
            };

            Body nearest = PlayState.PlayerFaction.FindNearestItemWithTags(item.CraftLocation, Vector3.Zero, false);


            string requirementsText = "Requires: " + item.CraftLocation + ",\n";

            if (nearest == null)
            {
                if (!hasCook)
                    requirementsText = "Needs " + item.CraftLocation + " to build!";
                else
                {
                    requirementsText = "Needs " + item.CraftLocation + " to cook!";
                }
            }
            else
            {
                foreach (Quantitiy<Resource.ResourceTags> resourceAmount in item.RequiredResources)
                {
                    ComboBox box = new ComboBox(GUI, BuildResourceTab.SelectedResourcesLayout)
                    {
                        Font = GUI.SmallFont
                    };

                    List<ResourceAmount> resources = Master.Faction.ListResourcesWithTag(resourceAmount.ResourceType);

                    foreach (ResourceAmount resource in resources)
                    {
                        if (resource.NumResources >= resourceAmount.NumResources)
                            box.AddValue(resource.ResourceType.ResourceName);
                    }

                    if (resources.Count == 0 || box.Values.Count == 0)
                    {
                        box.AddValue("<Not enough!>");
                    }

                    BuildResourceTab.SelectedResourcesLayout.AddItem(
                        resourceAmount.NumResources + " " + resourceAmount.ResourceType.ToString(), box);
                    BuildResourceTab.SelectedResourceBoxes.Add(box);
                }
            }


            if (item.RequiredResources.Count == 0)
            {
                requirementsText += "Nothing";
            }

            BuildResourceTab.InfoRequirements.Text = requirementsText;

            if (nearest == null)
            {
                BuildResourceTab.InfoRequirements.TextColor = Color.DarkRed;
                BuildResourceTab.BuildButton.IsVisible = false;
            }
            else
            {
                BuildResourceTab.InfoRequirements.TextColor = Color.Black;
                BuildResourceTab.BuildButton.IsVisible = true;
            }

        }


        private void ItemTabOnClicked(CraftItem item)
        {
            SelectedItem = item;

            BuildItemTab.InfoTitle.Text = item.Name;
            BuildItemTab.InfoImage.Image = item.Image;
            BuildItemTab.InfoDescription.Text = item.Description;

            BuildItemTab.BuildButton.IsVisible = true;
            string additional = "";

            
            BuildItemTab.InfoDescription.Text += additional;
            if (BuildItemTab.SelectedResourcesLayout != null)
                BuildItemTab.SelectedResourcesLayout.ClearChildren();
            BuildItemTab.SelectedResourceBoxes.Clear();
            BuildItemTab.SelectedResourcesLayout = new FormLayout(GUI, BuildItemTab.InfoRequirements)
            {
                EdgePadding = 0,
                LabelFont = GUI.SmallFont
            };
            string requirementsText = "Requires:\n";

            Body nearest = PlayState.PlayerFaction.FindNearestItemWithTags(item.CraftLocation, Vector3.Zero, false);

            if (nearest == null)
            {
                requirementsText = "Needs " + item.CraftLocation + " to build!";
            }
            else
            {
                foreach (Quantitiy<Resource.ResourceTags> resourceAmount in item.RequiredResources)
                {
                    //requirementsText += resourceAmount.ResourceType.ToString() + ": " + resourceAmount.NumResources + "\n";
                    ComboBox box = new ComboBox(GUI, BuildItemTab.SelectedResourcesLayout)
                    {
                        Font = GUI.SmallFont
                    };

                    List<ResourceAmount> resources = Master.Faction.ListResourcesWithTag(resourceAmount.ResourceType);

                    foreach (ResourceAmount resource in resources)
                    {
                        if (resource.NumResources >= resourceAmount.NumResources)
                            box.AddValue(resource.ResourceType.ResourceName);
                    }

                    if (resources.Count == 0 || box.Values.Count == 0)
                    {
                        box.AddValue("<Not enough!>");
                    }

                    BuildItemTab.SelectedResourcesLayout.AddItem(resourceAmount.NumResources + " " + resourceAmount.ResourceType.ToString(), box);
                    BuildItemTab.SelectedResourceBoxes.Add(box);
                }
   
            }

            if (item.RequiredResources.Count == 0)
            {
                requirementsText += "Nothing";
            }

            BuildItemTab.InfoRequirements.Text = requirementsText;

            if (nearest == null)
            {
                BuildItemTab.InfoRequirements.TextColor = Color.DarkRed;
                BuildItemTab.BuildButton.IsVisible = false;
            }
            else
            {
                BuildItemTab.InfoRequirements.TextColor = Color.Black;
                BuildItemTab.BuildButton.IsVisible = true;
            }
        }

        private void SetupBuildWallTab()
        {
            BuildWallTab = new BuildTab
            {
                Tab = Selector.AddTab("Walls")
            };
            CreateBuildTab(BuildWallTab, BuildType.Wall);
            BuildWallTab.BuildButton.OnClicked += WallButton_OnClicked;
            List<VoxelType> wallTypes = VoxelLibrary.GetTypes().Where(voxel => voxel.IsBuildable).ToList();

            int numItems = wallTypes.Count();
            int numColumns = 1;
            GridLayout layout = new GridLayout(GUI, BuildWallTab.Scroller, numItems, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40 * numItems),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            int i = 0;
            foreach (VoxelType wallType in wallTypes)
            {
                VoxelType wall = wallType;
                GridLayout itemLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };

                itemLayout.OnClicked += () => WallTabOnClicked(wall);
                int i1 = i;
                itemLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(itemLayout, 0, i, 1, 1);

                Label description = new Label(GUI, itemLayout, wall.Name + " Wall", GUI.SmallFont);

                itemLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        private void WallButton_OnClicked()
        {
            IsVisible = false;
            Master.Faction.RoomBuilder.CurrentRoomData = null;
            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            Master.Faction.WallBuilder.CurrentVoxelType = SelectedWall;
            Master.Faction.CraftBuilder.IsEnabled = false;
            Master.CurrentToolMode = GameMaster.ToolMode.Build;
            GUI.ToolTipManager.Popup("Click and drag to build " + SelectedWall.Name + " wall.");
        }


        public void CreateBuildTab(BuildTab tab, BuildType type)
        {
            GridLayout tabLayout = new GridLayout(GUI, tab.Tab, 1, 3)
            {
                EdgePadding = 0
            };

            GridLayout infoLayout = new GridLayout(GUI, tabLayout, 4, 2)
            {
                WidthSizeMode = SizeMode.Fixed,
                HeightSizeMode = SizeMode.Fixed
            };
            tabLayout.SetComponentPosition(infoLayout, 1, 0, 2, 1);
            tab.InfoImage = new ImagePanel(GUI, infoLayout, (Texture2D) null)
            {
                KeepAspectRatio = true,
                MinWidth = 32,
                MinHeight = 32
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

            switch (type)
            {
                case BuildType.Craft:
                    tab.BuildButton = new Button(GUI, infoLayout, "Craft", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Build]));
                    break;
                case BuildType.Item:
                case BuildType.Room:
                case BuildType.Wall:
                    tab.BuildButton = new Button(GUI, infoLayout, "Build", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Build]));
                    break;
                case BuildType.Cook:
                    tab.BuildButton = new Button(GUI, infoLayout, "Cook", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Cook]));
                    break;
            }
            infoLayout.SetComponentPosition(tab.BuildButton, 0, 3, 1, 1);

            tab.BuildButton.IsVisible = false;

            tab.Scroller = new ScrollView(GUI, tabLayout)
            {
                DrawBorder = true
            };
            tabLayout.SetComponentPosition(tab.Scroller, 0, 0, 1, 1);
            tabLayout.UpdateSizes();
        }

        void BuildRoomButton_OnClicked()
        {
            IsVisible = false;
            Master.Faction.RoomBuilder.CurrentRoomData = SelectedRoom;
            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            Master.Faction.WallBuilder.CurrentVoxelType = null;
            Master.Faction.CraftBuilder.IsEnabled = false;
            Master.CurrentToolMode = GameMaster.ToolMode.Build;
            GUI.ToolTipManager.Popup("Click and drag to build " + SelectedRoom.Name);
        }

        private void HoverItem(GridLayout roomLayout, int i)
        {
            roomLayout.HighlightRow(i, new Color(255, 100, 100, 200));
        }
    }

}
