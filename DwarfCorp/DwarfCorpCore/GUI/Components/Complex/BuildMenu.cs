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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a widget for building rooms, craft objects, items and walls.
    /// </summary>
    public class BuildMenu : Window
    {
        /// <summary>
        ///     The type of thing to be built. A build menu can have a combination
        ///     of building types. For example, one menu can encompass crafts, items and walls.
        ///     Another can encompass cooking.
        /// </summary>
        [Flags]
        public enum BuildType
        {
            // Rooms are collections of voxels and objects.
            Room = 1,
            // Walls are filled voxels.
            Wall = 2,
            // Items are things like doors or traps
            Item = 4,
            // Crafts are intermediate resources that can
            // be traded.
            Craft = 8,
            // Foods that can be eaten
            Cook = 16
        }

        /// <summary>
        ///     Create a build menu.
        /// </summary>
        /// <param name="gui">GUI that the menu is attached to.</param>
        /// <param name="parent">GUI component the menu is attached to.</param>
        /// <param name="faction">Faction that is building stuff.</param>
        /// <param name="type">The type of menu to open.</param>
        public BuildMenu(DwarfGUI gui, GUIComponent parent, GameMaster faction, BuildType type) :
            base(gui, parent, WindowButtons.CloseButton)
        {
            var layout = new GridLayout(GUI, this, 1, 1);
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

        /// <summary>
        ///     Player that the build menu is associated with.
        /// </summary>
        public GameMaster Master { get; set; }

        /// <summary>
        ///     Tabs for each of the building types.
        /// </summary>
        public TabSelector Selector { get; set; }

        /// <summary>
        ///     The kind of thing the player is currently trying to build.
        /// </summary>
        public BuildType Build { get; set; }

        /// <summary>
        ///     Tab associated with crafting.
        /// </summary>
        public BuildTab BuildResourceTab { get; set; }

        /// <summary>
        ///     Tab associated with building rooms.
        /// </summary>
        public BuildTab BuildRoomTab { get; set; }

        /// <summary>
        ///     Tab associated with building special objects like doors or traps.
        /// </summary>
        public BuildTab BuildItemTab { get; set; }

        /// <summary>
        ///     Tab associated with building walls.
        /// </summary>
        public BuildTab BuildWallTab { get; set; }

        /// <summary>
        ///     The current room type that the player wants to build.
        /// </summary>
        public RoomData SelectedRoom { get; set; }

        /// <summary>
        ///     The current item type that the player wants to build.
        /// </summary>
        public CraftItem SelectedItem { get; set; }

        /// <summary>
        ///     The current wall type that the player wants to build.
        /// </summary>
        public VoxelType SelectedWall { get; set; }

        /// <summary>
        ///     This is the selected craft that the player wants to make.
        /// </summary>
        public CraftItem SelectedResource { get; set; }

        private void SetupBuildResourceTab()
        {
            // So, cooking and crafting are the same thing, aside from a few
            // aesthetic differences.
            bool hasCook = Build.HasFlag(BuildType.Cook);

            // The name is either "Food" or "Crafts" depending on what we're doing.
            string name = hasCook ? "Food" : "Crafts";

            // Set up the tab.
            BuildResourceTab = new BuildTab
            {
                Tab = Selector.AddTab(name),
                SelectedResourceBoxes = new List<ComboBox>()
            };
            CreateBuildTab(BuildResourceTab, hasCook ? BuildType.Cook : BuildType.Craft);
            BuildResourceTab.BuildButton.OnClicked += BuildResource_OnClicked;

            // Find all the items that can be crafted.
            List<CraftItem> items =
                CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Resource).ToList();

            // Create a layout containing all the items.
            int numItems = items.Count();
            int numColumns = 1;
            var layout = new GridLayout(GUI, BuildResourceTab.Scroller, numItems, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40*numItems),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            // For each item, determine if it can be crafted, and add it to the layout.
            int i = 0;
            foreach (CraftItem itemType in items)
            {
                CraftItem item = itemType;

                // Edible objects can be cooked, but not crafted.
                bool isEdible = ResourceLibrary.Resources.ContainsKey(item.ResourceCreated) &&
                                ResourceLibrary.Resources[item.ResourceCreated].Tags.Contains(
                                    Resource.ResourceTags.Edible);
                if (!hasCook && isEdible)
                    continue;
                if (hasCook && !isEdible)
                    continue;

                var itemLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };

                // Automatically select the first item.
                if (i == 0)
                {
                    ResourceTabOnClicked(item);
                }

                // Allow the user to click on an item.
                itemLayout.OnClicked += () => ResourceTabOnClicked(item);

                // If the user hovers over the item, draw a rectangle around it.
                int i1 = i;
                itemLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(itemLayout, 0, i, 1, 1);

                var icon = new ImagePanel(GUI, itemLayout, item.Image)
                {
                    KeepAspectRatio = true,
                    MinWidth = 32,
                    MinHeight = 32
                };
                itemLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                var description = new Label(GUI, itemLayout, item.Name, GUI.SmallFont)
                {
                    ToolTip = item.Description
                };
                itemLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        /// <summary>
        ///     Called whenever the user clicks on a resource.
        /// </summary>
        private void BuildResource_OnClicked()
        {
            var assignments = new List<Task>();
            SelectedResource.SelectedResources = new List<ResourceAmount>();

            for (int i = 0; i < BuildResourceTab.SelectedResourceBoxes.Count; i++)
            {
                ComboBox box = BuildResourceTab.SelectedResourceBoxes[i];

                // This is kind of an insane choice, but if the user doesn't have enough resources to build
                // the item, we don't bother checking, we just check the text in the combo box.
                if (box.CurrentValue == "<Not enough!>")
                {
                    return;
                }

                // Otherwise, assert that the selected dwarf has to gather the resources the player told him to.
                Quantitiy<Resource.ResourceTags> tags = SelectedResource.RequiredResources[i];
                SelectedResource.SelectedResources.Add(new ResourceAmount(box.CurrentValue, tags.NumResources));
            }

            // Create a list of tasks for the selected dwarf(s) to craft the object using the selected resources.
            assignments.Add(new CraftResourceTask(SelectedResource));
            if (assignments.Count > 0)
            {
                TaskManager.AssignTasks(assignments,
                    Faction.FilterMinionsWithCapability(Master.SelectedMinions, GameMaster.ToolMode.Craft));
            }
        }


        public override void Update(DwarfTime time)
        {
            base.Update(time);
        }


        /// <summary>
        ///     Called whenever the player decides to build a room.
        /// </summary>
        /// <param name="room">The room that was selected.</param>
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

            foreach (var pair in room.RequiredResources)
            {
                requirementsText += pair.Key + ": " + pair.Value.NumResources + "\n";
            }


            if (room.RequiredResources.Count == 0)
            {
                requirementsText += "Nothing";
            }

            BuildRoomTab.InfoRequirements.Text = requirementsText;
        }

        /// <summary>
        ///     Called whenever the player has decided to build a wall.
        /// </summary>
        /// <param name="wall">The wall type to build.</param>
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

        /// <summary>
        ///     Create a tab for building rooms.
        /// </summary>
        public void SetupBuildRoomTab()
        {
            // Create the tab.
            BuildRoomTab = new BuildTab
            {
                Tab = Selector.AddTab("Rooms")
            };
            CreateBuildTab(BuildRoomTab, BuildType.Room);
            BuildRoomTab.BuildButton.OnClicked += BuildRoomButton_OnClicked;

            // Get a list of all available rooms.
            List<string> roomTypes = RoomLibrary.GetRoomTypes().ToList();

            // Create a layout containing all the rooms.
            int numRooms = roomTypes.Count();
            int numColumns = 1;
            var layout = new GridLayout(GUI, BuildRoomTab.Scroller, numRooms, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40*numRooms),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            // For each room, add an icon to the list of buildable rooms.
            int i = 0;
            foreach (string roomType in roomTypes)
            {
                RoomData room = RoomLibrary.GetData(roomType);

                var roomLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };

                // Automatically select the first room.
                if (i == 0)
                {
                    RoomTabOnClicked(room);
                }

                roomLayout.OnClicked += () => RoomTabOnClicked(room);
                int i1 = i;
                roomLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(roomLayout, 0, i, 1, 1);

                var icon = new ImagePanel(GUI, roomLayout, room.Icon)
                {
                    KeepAspectRatio = true,
                    MinWidth = 32,
                    MinHeight = 32
                };
                roomLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                var description = new Label(GUI, roomLayout, room.Name, GUI.SmallFont)
                {
                    ToolTip = room.Description
                };
                roomLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        /// <summary>
        ///     Create a tab for building items.
        /// </summary>
        private void SetupBuildItemTab()
        {
            // Create the tab.
            BuildItemTab = new BuildTab
            {
                Tab = Selector.AddTab("Objects"),
                SelectedResourceBoxes = new List<ComboBox>()
            };
            CreateBuildTab(BuildItemTab, BuildType.Item);
            BuildItemTab.BuildButton.OnClicked += BuildItemButton_OnClicked;
            List<CraftItem> items =
                CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Object).ToList();

            // Create a layout containing all the objects that can be built.
            int numItems = items.Count();
            int numColumns = 1;
            var layout = new GridLayout(GUI, BuildItemTab.Scroller, numItems, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40*numItems),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };

            // For every item, add an icon to the list of things that can be built.
            int i = 0;
            foreach (CraftItem itemType in items)
            {
                CraftItem item = itemType;
                var itemLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };

                // Automatically select the first item.
                if (i == 0)
                    ItemTabOnClicked(item);
                itemLayout.OnClicked += () => ItemTabOnClicked(item);
                int i1 = i;
                itemLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(itemLayout, 0, i, 1, 1);

                var icon = new ImagePanel(GUI, itemLayout, item.Image)
                {
                    KeepAspectRatio = true,
                    MinWidth = 32,
                    MinHeight = 32
                };
                itemLayout.SetComponentPosition(icon, 0, 0, 1, 1);

                var description = new Label(GUI, itemLayout, item.Name, GUI.SmallFont)
                {
                    ToolTip = item.Description
                };
                itemLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        /// <summary>
        ///     Called whenever the player has decided to build an item.
        /// </summary>
        private void BuildItemButton_OnClicked()
        {
            // Get the resources the player has selected.
            SelectedItem.SelectedResources = new List<ResourceAmount>();

            for (int i = 0; i < BuildItemTab.SelectedResourceBoxes.Count; i++)
            {
                ComboBox box = BuildItemTab.SelectedResourceBoxes[i];

                // This is kind of insane. We check against the value in the combo box to decide
                // whether or not the player has enough resources for the item, since it was already
                // computed.
                if (box.CurrentValue == "<Not enough!>")
                {
                    return;
                }

                Quantitiy<Resource.ResourceTags> tags = SelectedItem.RequiredResources[i];
                SelectedItem.SelectedResources.Add(new ResourceAmount(box.CurrentValue, tags.NumResources));
            }

            // Set up the item builder to use the appropriate item.
            IsVisible = false;
            Master.Faction.RoomBuilder.CurrentRoomData = null;
            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            Master.Faction.WallBuilder.CurrentVoxelType = null;
            Master.Faction.CraftBuilder.IsEnabled = true;
            Master.Faction.CraftBuilder.CurrentCraftType = SelectedItem;
            Master.CurrentToolMode = GameMaster.ToolMode.Build;
            GUI.ToolTipManager.Popup("Click and drag to build " + SelectedItem.Name);
        }

        /// <summary>
        ///     Called whenever the player decides to craft an intermediary resource.
        /// </summary>
        /// <param name="item">The item to craft.</param>
        private void ResourceTabOnClicked(CraftItem item)
        {
            // This tells us whether or not the object should be cooked, or crafted like a normal item.
            bool hasCook = Build.HasFlag(BuildType.Cook);


            SelectedResource = item;

            // Set up the tab's descriptive text to match the item being crafted.
            BuildResourceTab.InfoTitle.Text = item.Name;
            BuildResourceTab.InfoImage.Image = item.Image;
            BuildResourceTab.InfoDescription.Text = item.Description;
            BuildResourceTab.SelectedResourceBoxes = new List<ComboBox>();
            BuildResourceTab.BuildButton.IsVisible = true;
            if (BuildResourceTab.SelectedResourcesLayout != null)
                BuildResourceTab.SelectedResourcesLayout.ClearChildren();
            BuildResourceTab.SelectedResourceBoxes.Clear();
            BuildResourceTab.SelectedResourcesLayout = new FormLayout(GUI, BuildResourceTab.InfoRequirements)
            {
                EdgePadding = 0,
                LabelFont = GUI.SmallFont
            };

            // This is the nearest item that the player knows about which can be used to craft the object.
            // (for example, an anvil or stone)
            Body nearest = PlayState.PlayerFaction.FindNearestItemWithTags(item.CraftLocation, Vector3.Zero, false);


            // Tell the user that we require this item to be built first.
            string requirementsText = "Requires: " + item.CraftLocation + ",\n";

            // If the player doesn't already have this item, let them know!
            if (nearest == null)
            {
                if (!hasCook)
                    requirementsText = "Needs " + item.CraftLocation + " to build!";
                else
                {
                    requirementsText = "Needs " + item.CraftLocation + " to cook!";
                }
            }
                // Otherwise, in addition to the required item, the player must also have the required resources to craft the item.
            else
            {
                // For each of the resource requirements...
                foreach (var resourceAmount in item.RequiredResources)
                {
                    // Tell the user they need this.
                    var box = new ComboBox(GUI, BuildResourceTab.SelectedResourcesLayout)
                    {
                        Font = GUI.SmallFont
                    };

                    // Check to see which resources the player already has. Note that the requirements
                    // to craft an item amount to item "properties" rather than item "types". For example,
                    // Iron, Gold and Silver may all have the "metal" property.
                    List<ResourceAmount> resources = Master.Faction.ListResourcesWithTag(resourceAmount.ResourceType);

                    // If the player already has the resource, let them select it.
                    foreach (ResourceAmount resource in resources)
                    {
                        if (resource.NumResources >= resourceAmount.NumResources)
                            box.AddValue(resource.ResourceType.ResourceName);
                    }

                    // Otherwise, inform them that they do not have enough of that resource.
                    if (resources.Count == 0 || box.Values.Count == 0)
                    {
                        box.AddValue("<Not enough!>");
                    }

                    BuildResourceTab.SelectedResourcesLayout.AddItem(
                        resourceAmount.NumResources + " " + resourceAmount.ResourceType, box);
                    BuildResourceTab.SelectedResourceBoxes.Add(box);
                }
            }

            // Some items may be free. In this case, tell the player that the item has no requirements to build.
            if (item.RequiredResources.Count == 0)
            {
                requirementsText += "Nothing";
            }

            BuildResourceTab.InfoRequirements.Text = requirementsText;

            // If the required thing does not exist, let the player know this is bad by turning the text dark red.
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

        /// <summary>
        ///     Called whenever the user decides to craft a specific item (such as a door or trap)
        /// </summary>
        /// <param name="item"></param>
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

            // Get any required object to craft the item.
            Body nearest = PlayState.PlayerFaction.FindNearestItemWithTags(item.CraftLocation, Vector3.Zero, false);

            // If the object doesn't exist, inform the player
            if (nearest == null)
            {
                requirementsText = "Needs " + item.CraftLocation + " to build!";
            }
                // Otherwise, the item may need additional resources
            else
            {
                foreach (var resourceAmount in item.RequiredResources)
                {
                    var box = new ComboBox(GUI, BuildItemTab.SelectedResourcesLayout)
                    {
                        Font = GUI.SmallFont
                    };

                    // Allow the player to select resources that meet the req
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

                    BuildItemTab.SelectedResourcesLayout.AddItem(
                        resourceAmount.NumResources + " " + resourceAmount.ResourceType, box);
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

        /// <summary>
        ///     Create tab for building walls (filled voxels).
        /// </summary>
        private void SetupBuildWallTab()
        {
            BuildWallTab = new BuildTab
            {
                Tab = Selector.AddTab("Walls")
            };
            CreateBuildTab(BuildWallTab, BuildType.Wall);
            BuildWallTab.BuildButton.OnClicked += WallButton_OnClicked;

            // Get all buildable voxel types and list them as walls.
            List<VoxelType> wallTypes = VoxelLibrary.GetTypes().Where(voxel => voxel.IsBuildable).ToList();

            int numItems = wallTypes.Count();
            int numColumns = 1;
            var layout = new GridLayout(GUI, BuildWallTab.Scroller, numItems, numColumns)
            {
                LocalBounds = new Rectangle(0, 0, 720, 40*numItems),
                EdgePadding = 0,
                WidthSizeMode = SizeMode.Fit,
                HeightSizeMode = SizeMode.Fixed
            };


            int i = 0;
            foreach (VoxelType wallType in wallTypes)
            {
                VoxelType wall = wallType;
                var itemLayout = new GridLayout(GUI, layout, 1, 3)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed,
                    EdgePadding = 0
                };

                itemLayout.OnClicked += () => WallTabOnClicked(wall);
                int i1 = i;
                itemLayout.OnHover += () => HoverItem(layout, i1);

                layout.SetComponentPosition(itemLayout, 0, i, 1, 1);

                var description = new Label(GUI, itemLayout, wall.Name + " Wall", GUI.SmallFont);

                itemLayout.SetComponentPosition(description, 1, 0, 1, 1);
                i++;
            }
            layout.UpdateSizes();
        }

        /// <summary>
        ///     Called whenever the user decides to build a wall.
        /// </summary>
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

        /// <summary>
        ///     Create a tab for building items, crafts or walls.
        /// </summary>
        /// <param name="tab">The tab to set up</param>
        /// <param name="type">The type of building (items, crafts or walls)</param>
        public void CreateBuildTab(BuildTab tab, BuildType type)
        {
            var tabLayout = new GridLayout(GUI, tab.Tab, 1, 3)
            {
                EdgePadding = 0
            };

            var infoLayout = new GridLayout(GUI, tabLayout, 4, 2)
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
                    tab.BuildButton = new Button(GUI, infoLayout, "Craft", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                        GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Build]));
                    break;
                case BuildType.Item:
                case BuildType.Room:
                case BuildType.Wall:
                    tab.BuildButton = new Button(GUI, infoLayout, "Build", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                        GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Build]));
                    break;
                case BuildType.Cook:
                    tab.BuildButton = new Button(GUI, infoLayout, "Cook", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                        GUI.Skin.GetMouseFrame(GUI.Skin.MouseFrames[GUISkin.MousePointer.Cook]));
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

        /// <summary>
        ///     Called whenever the player decides to build a room. Enters build room mode.
        /// </summary>
        private void BuildRoomButton_OnClicked()
        {
            IsVisible = false;
            Master.Faction.RoomBuilder.CurrentRoomData = SelectedRoom;
            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            Master.Faction.WallBuilder.CurrentVoxelType = null;
            Master.Faction.CraftBuilder.IsEnabled = false;
            Master.CurrentToolMode = GameMaster.ToolMode.Build;
            GUI.ToolTipManager.Popup("Click and drag to build " + SelectedRoom.Name);
        }

        /// <summary>
        ///     Whenever the player hovers over an item in the grid, highlight it.
        /// </summary>
        /// <param name="roomLayout">The grid</param>
        /// <param name="i">The item that the player hovered over.</param>
        private void HoverItem(GridLayout roomLayout, int i)
        {
            roomLayout.HighlightRow(i, new Color(255, 100, 100, 200));
        }

        /// <summary>
        ///     A build tab is a selectable build mode that tells the player
        ///     what can be built in a certain category (like walls, or rooms)
        /// </summary>
        public class BuildTab
        {
            /// <summary>
            ///     This is the image displayed for the currently selected object.
            /// </summary>
            public ImagePanel InfoImage { get; set; }

            /// <summary>
            ///     This is the name of the currently selected object.
            /// </summary>
            public Label InfoTitle { get; set; }

            /// <summary>
            ///     Brief description of the object that can be built.
            /// </summary>
            public Label InfoDescription { get; set; }

            /// <summary>
            ///     Requirements for the object to be built.
            /// </summary>
            public Label InfoRequirements { get; set; }

            /// <summary>
            ///     Button labeled "Build" that when clicked gets the player
            ///     into Build Mode.
            /// </summary>
            public Button BuildButton { get; set; }

            /// <summary>
            ///     The underlying Tab associated with this build mode.
            /// </summary>
            public TabSelector.Tab Tab { get; set; }

            /// <summary>
            ///     Scrollable list of things to build.
            /// </summary>
            public ScrollView Scroller { get; set; }

            /// <summary>
            ///     The player can select different kinds of resources to build objects.
            ///     For example, a metal door could be built of gold, iron or silver.
            /// </summary>
            public FormLayout SelectedResourcesLayout { get; set; }

            /// <summary>
            ///     These are the resources the player has selected to use during building.
            /// </summary>
            public List<ComboBox> SelectedResourceBoxes { get; set; }
        }
    }
}