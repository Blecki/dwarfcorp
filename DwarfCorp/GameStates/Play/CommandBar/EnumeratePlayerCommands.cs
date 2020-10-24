using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.Play
{
    public class PlayerCommandEnumerator
    {
        public static IEnumerable<PossiblePlayerCommand> EnumeratePlayerCommands(WorldManager World)
        {
            yield return new PossiblePlayerCommand
            {
                ID = new List<String> { "Select" },
                DisplayName = "Select",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/icons",
                    FrameSize = new Point(32, 32),
                    Frame = new Point(5, 0)
                },
                OnClick = (sender, args) => World.UserInterface.ChangeTool("SelectUnits", null),
                Tooltip = "Select dwarves"
            };

            yield return new PossiblePlayerCommand
            {
                ID = new List<String> { "Destroy Zone" },
                DisplayName = "Destroy",
                Tooltip = "Deconstruct Zone",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/round-buttons",
                    FrameSize = new Point(16, 16),
                    Frame = new Point(1, 1)
                },
                OnClick = (sender, args) =>
                {
                    World.UserInterface.ShowToolPopup("Left click zones to destroy them.");
                    World.UserInterface.ChangeTool("DestroyZone", null);
                }
            };

            foreach (var data in Library.EnumerateZoneTypes())
                yield return new PossiblePlayerCommand
                {
                    ID = new List<String> { "Build Zone", data.DisplayName },
                    DisplayName = "Build " + data.DisplayName,
                    OldStyleIcon = data.NewIcon,
                    OnClick = (sender, args) => World.UserInterface.ChangeTool("BuildZone", data),
                    Tooltip = "",
                    HoverWidget = new BuildRoomInfo
                    {
                        Data = data,
                        Rect = new Rectangle(0, 0, 256, 164),
                        World = World
                    }
                };

            yield return new PossiblePlayerCommand
            {
                ID = new List<String> { "Move Object" },
                DisplayName = "Move Object",
                Tooltip = "Move objects",
                OldStyleIcon = new TileReference("mouse", 9),
                OnClick = (sender, args) =>
                {
                    World.UserInterface.ShowToolPopup("Left click objects to move them.\nRight click to destroy them.");
                    World.UserInterface.ChangeTool("MoveObjects");
                }
            };

            yield return new PossiblePlayerCommand
            {
                ID = new List<String> { "Destroy Object" },
                DisplayName = "Destroy Object",
                Tooltip = "Deconstruct objects",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/round-buttons",
                    FrameSize = new Point(16, 16),
                    Frame = new Point(1, 1)
                },
                OnClick = (sender, args) =>
                {
                    World.UserInterface.ShowToolPopup("Left click objects to destroy them.");
                    World.UserInterface.ChangeTool("DeconstructObject");
                }
            };

            foreach (var data in Library.EnumerateVoxelTypes().Where(v => v.IsBuildable))
                yield return new PossiblePlayerCommand
                {
                    ID = new List<string> { "Build Wall", data.Name },
                    DisplayName = "Build " + data.Name + " Wall",
                    OldStyleIcon = new Gui.TileReference("voxels", data.ID),
                    OperationIcon = new ResourceType.GuiGraphic
                    {
                        AssetPath = "newgui//icons",
                        FrameSize = new Point(32, 32),
                        Frame = new Point(0, 3)
                    },
                    HoverWidget = new BuildWallInfo
                    {
                        Data = data,
                        Rect = new Rectangle(0, 0, 256, 128),
                        World = World
                    },
                    OnClick = (_sender, args) =>
                    {
                        World.UserInterface.ChangeTool("BuildWall", new BuildWallTool.BuildWallToolArguments
                        {
                            VoxelType = (byte)data.ID,
                            Floor = false
                        });
                    },
                    IsAvailable = () => World.CanBuildVoxel(data)
                };

            foreach (var data in Library.EnumerateVoxelTypes().Where(v => v.IsBuildable))
                yield return new PossiblePlayerCommand
                {
                    ID = new List<string> { "Build Floor", data.Name },
                    DisplayName = "Build " + data.Name + " Floor",
                    OldStyleIcon = new Gui.TileReference("voxels", data.ID),
                    OperationIcon = new ResourceType.GuiGraphic
                    {
                        AssetPath = "newgui//icons",
                        FrameSize = new Point(32,32),
                        Frame = new Point(1,3)
                    },
                    HoverWidget = new BuildWallInfo
                    {
                        Data = data,
                        Rect = new Rectangle(0, 0, 256, 128),
                        World = World
                    },
                    OnClick = (_sender, args) =>
                    {
                        World.UserInterface.ChangeTool("BuildWall", new BuildWallTool.BuildWallToolArguments
                        {
                            VoxelType = (byte)data.ID,
                            Floor = true
                        });
                    },
                    IsAvailable = () => World.CanBuildVoxel(data)
                };

            // TODO: Translation
            Func<string, string> objectNameToLabel = (string name) =>
            {
                var replacement = name.Replace("Potion", "").Replace("of", "");
                return replacement;
            };

            foreach (var data in Library.EnumerateResourceTypes().Where(r => r.Craft_Craftable))
                yield return new PossiblePlayerCommand
                {
                    ID = new List<string> { "Craft", data.Category, data.DisplayName },
                    Icon = data.Gui_Graphic,
                    Tooltip = data.Craft_Verb.PastTense + " " + objectNameToLabel(data.DisplayName),
                    DisplayName = data.Craft_Verb.PastTense + " "  + objectNameToLabel(data.DisplayName),
                    OperationIcon = new ResourceType.GuiGraphic
                    {
                        AssetPath = "newgui//icons",
                        FrameSize = new Point(32, 32),
                        Frame = new Point(7, 4)
                    },
                    HoverWidget = new BuildCraftInfo
                    {
                        Data = data as ResourceType,
                        DrawBorder = false,
                        Rect = new Rectangle(0, 0, 450, 200),
                        World = World,
                        OnShown = (sender) => World.Tutorial((data as ResourceType).TypeName),
                        BuildAction = (sender, args) =>
                        {
                            var buildInfo = (sender as Gui.Widgets.BuildCraftInfo);
                            if (buildInfo == null)
                                return;
                            //sender.Hidden = true;

                            // Todo: Break out into task composition function.
                            var numRepeats = buildInfo.GetNumRepeats();
                            if (numRepeats > 1)
                            {
                                var subTasks = new List<Task>();
                                var compositeTask = new CompoundTask(String.Format("Craft {0} {1}", numRepeats, (data as ResourceType).PluralDisplayName), TaskCategory.CraftItem, TaskPriority.Medium);
                                for (var i = 0; i < numRepeats; ++i)
                                    subTasks.Add(new CraftResourceTask((data as ResourceType), i + 1, numRepeats, buildInfo.GetSelectedResources()) { Hidden = true });
                                World.TaskManager.AddTasks(subTasks);
                                compositeTask.AddSubTasks(subTasks);
                                World.TaskManager.AddTask(compositeTask);
                            }
                            else
                                World.TaskManager.AddTask(new CraftResourceTask((data as ResourceType), 1, 1, buildInfo.GetSelectedResources()));

                            World.UserInterface.ShowToolPopup((data as ResourceType).Craft_Verb.PresentTense + " " + numRepeats.ToString() + " " + (numRepeats == 1 ? data.DisplayName : (data as ResourceType).PluralDisplayName));
                        }
                    }
                };

            foreach (var data in Library.EnumerateResourceTypes().Where(r => r.Placement_Placeable))
                yield return new PossiblePlayerCommand
                {
                    ID = new List<string> { "Place", data.Category, data.DisplayName },
                    Icon = data.Gui_Graphic,
                    Tooltip = "Place " + objectNameToLabel(data.DisplayName),
                    DisplayName = "Place " + objectNameToLabel(data.DisplayName),
                    HoverWidget = new PlaceCraftInfo
                    {
                        Data = data as ResourceType,
                        Rect = new Rectangle(0, 0, 256, 164),
                        World = World,
                    },
                    IsAvailable = () => World.ListResources().Any(r => r.Key == data.TypeName),
                    OnClick = (sender, args) => World.UserInterface.ChangeTool("PlaceObject", data)
                };

            yield return new PossiblePlayerCommand
            {
                ID = new List<string> { "Rail", "Paint" },
                DisplayName = "Paint Rail",
                OldStyleIcon = new TileReference("rail", 0),
                Tooltip = "Paint",
                OnClick = (widget, args) => World.UserInterface.ChangeTool("PaintRail", Rail.PaintRailTool.Mode.Normal)
            };

            foreach (var data in Library.EnumerateRailPatterns())
                yield return new PossiblePlayerCommand
                {
                    ID = new List<string> { "Rail", data.Name },
                    Tooltip = "Build Rail " + data.Name,
                    DisplayName = data.Name,
                    OldStyleIcon = new TileReference("rail", data.Icon),
                    OperationIcon = new ResourceType.GuiGraphic
                    {
                        AssetPath = "newgui//icons",
                        FrameSize = new Point(32, 32),
                        Frame = new Point(7, 2)
                    },
                    OnClick = (sender, args) => World.UserInterface.ChangeTool("BuildRail", new Rail.BuildRailTool.Arguments
                    {
                        Pattern = data,
                        Mode = Rail.BuildRailTool.Mode.Normal
                    })
                };


            yield return new PossiblePlayerCommand
            {
                ID = new List<string> { "Dig" },
                DisplayName = "Dig",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/icons",
                    FrameSize = new Point(32, 32),
                    Frame = new Point(0, 0)
                },
                Tooltip = "Dig",
                OnClick = (sender, args) => World.UserInterface.ChangeTool("Dig"),
            };

            yield return new PossiblePlayerCommand
            {
                ID = new List<string> { "Gather" },
                DisplayName = "Gather",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/icons",
                    FrameSize = new Point(32, 32),
                    Frame = new Point(6, 0)
                },
                Tooltip = "Tell dwarves to pick things up.",
                OnClick = (sender, args) => World.UserInterface.ChangeTool("Gather")
            };

            yield return new PossiblePlayerCommand
            {
                ID = new List<string> { "Harvest" },
                DisplayName = "Harvest",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/icons",
                    FrameSize = new Point(32, 32),
                    Frame = new Point(1, 0)
                },
                Tooltip = "Chop trees and harvest plants.",
                OnClick = (sender, args) => World.UserInterface.ChangeTool("Chop")
            };

            yield return new PossiblePlayerCommand
            {
                ID = new List<string> { "Hunt" },
                DisplayName = "Hunt",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/icons",
                    FrameSize = new Point(32, 32),
                    Frame = new Point(3, 0)
                },
                Tooltip = "Attack",
                OnClick = (sender, args) => World.UserInterface.ChangeTool("Attack")
            };

            foreach (var data in Library.EnumerateResourceTypesWithTag("Plantable"))
                yield return new PossiblePlayerCommand
                {
                    ID = new List<string> { "Plant", data.DisplayName },
                    Icon = data.Gui_Graphic,
                    OperationIcon = new ResourceType.GuiGraphic
                    {
                        AssetPath = "newgui//icons",
                        FrameSize = new Point(32, 32),
                        Frame = new Point(5, 1)
                    },
                    Tooltip = "Plant " + data.DisplayName,
                    DisplayName = "Plant " + data.DisplayName,
                    OnClick = (sender, args) => World.UserInterface.ChangeTool("Plant", data.TypeName),
                    HoverWidget = new PlantInfo()
                    {
                        Type = data.TypeName,
                        Rect = new Rectangle(0, 0, 256, 128),
                        TextColor = Color.Black.ToVector4()
                    },
                };

            yield return new PossiblePlayerCommand
            {
                ID = new List<string> { "Wrangle" },
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/icons",
                    FrameSize = new Point(32, 32),
                    Frame = new Point(0, 4)
                },
                DisplayName = "Catch",
                Tooltip = "Catch Animals",
                HoverWidget = new Widget()
                {
                    Border = "border-fancy",
                    Text = "Catch Animals.\n Click and drag to catch animals.\nRequires animal pen.",
                    Rect = new Rectangle(0, 0, 256, 128),
                    TextColor = Color.Black.ToVector4()
                },
                OnClick = (sender, args) => World.UserInterface.ChangeTool("Wrangle")
            };
            
            yield return new PossiblePlayerCommand
            {
                ID = new List<string> { "Cancel" },
                DisplayName = "Cancel",
                Tooltip = "Cancel voxel tasks such as mining, guarding, and planting.",
                Icon = new ResourceType.GuiGraphic
                {
                    AssetPath = "newgui/round-buttons",
                    FrameSize = new Point(16, 16),
                    Frame = new Point(1, 1)
                },
                OnClick = (sender, args) =>
                {
                    World.UserInterface.ChangeTool("CancelTasks", sender.HoverWidget);
                },
                HoverWidget = new CancelToolOptions
                {
                    Rect = new Rectangle(0, 0, 200, 100)
                }
            };
        }
    }
}
