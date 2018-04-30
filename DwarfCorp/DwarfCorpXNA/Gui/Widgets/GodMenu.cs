using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class GodMenu : HorizontalMenuTray.Tray
    {
        public GameMaster Master;

        private void ActivateGodTool(String Command)
        {
            (Master.Tools[GameMaster.ToolMode.God] as GodModeTool).Command = Command;
            Master.ChangeTool(GameMaster.ToolMode.God);
        }

        public override void Construct()
        {
            IsRootTray = true;

            ItemSource = new Gui.Widget[]
            {
                new HorizontalMenuTray.MenuItem
                {
                    Text = "CHEAT MODE",
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "DEBUG",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSize = new Point(100, 20),
                        ItemSource = new HorizontalMenuTray.MenuItem[]
                        {
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "SWITCHES",
                                ExpansionChild = new HorizontalMenuTray.Tray
                                {
                                    ItemSize = new Point(200, 20),
                                    ItemSource = Debugger.EnumerateSwitches().Select(s =>
                                    new HorizontalMenuTray.CheckboxMenuItem
                                    {
                                        Text = Debugger.GetNicelyFormattedName(s.Name),
                                        InitialState = s.State,
                                        SetCallback = s.Set
                                    })
                                }
                            },

                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "AI",
                                ExpansionChild = new EmployeeAIDebugPanel
                                {
                                    World = Master.World,
                                    MinimumSize = new Point(300, 200)
                                }
                            },

                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "STATS",
                                ExpansionChild = new HorizontalMenuTray.Tray
                                {
                                    ItemSource = new HorizontalMenuTray.MenuItem[]
                                    {
                                        new HorizontalMenuTray.UpdatingMenuItem
                                        {
                                            OnUpdateMenuItem = (sender) =>
                                            {
                                                sender.Text = String.Format("Entities: {0}", Master.World.ComponentManager.RootComponent.Children.Count);
                                                sender.Invalidate();
                                            }
                                        }
                                    }
                                }
                            },

                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "DEBUG SAVE",
                                OnClick = (sender, args) =>
                                {
                                    // Turn off binary compressed saves and save a nice straight json save for debugging purposes.

                                    // Todo: Why isn't World managing this paused state itself?
                                    bool paused = Master.World.Paused;
                                    var previousSetting = DwarfGame.COMPRESSED_BINARY_SAVES;
                                    DwarfGame.COMPRESSED_BINARY_SAVES = false;
                                    Master.World.Save(
                                        String.Format("{0}_{1}_DEBUG", Overworld.Name, Master.World.GameID),
                                        (success, exception) =>
                                        {
                                            Master.World.MakeAnnouncement(success ? "Debug save created.": "Debug save failed - " + exception.Message);
                                            DwarfGame.COMPRESSED_BINARY_SAVES = previousSetting;
                                            Master.World.Paused = paused;
                                        });                                    
                                }
                            }
                        }
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "BUILD",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSource = RoomLibrary.GetRoomTypes().Select(r =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = r,
                                OnClick = (sender, args) => ActivateGodTool("Build/" + r)
                            })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "SPAWN",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 5,
                        ItemSource = EntityFactory.EnumerateEntityTypes().OrderBy(s => s).Select(s =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = s,
                                OnClick = (sender, args) => ActivateGodTool("Spawn/" + s)
                            })
                    }
                },
                
                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE BLOCK",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 3,
                        ItemSource = VoxelLibrary.PrimitiveMap.Keys
                            .Where(t => t.Name != "empty" && t.Name != "water")
                            .OrderBy(s => s.Name)
                            .Select(s =>
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => ActivateGodTool("Place/" + s.Name)
                                })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "DELETE BLOCK",
                    OnClick = (sender, args) => ActivateGodTool("Delete Block")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "KILL BLOCK",
                    OnClick = (sender, args) => ActivateGodTool("Kill Block")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE GRASS",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 3,
                        ItemSource = GrassLibrary.TypeList
                            .OrderBy(s => s.Name)
                            .Select(s =>
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => ActivateGodTool("Grass/" + s.Name)
                                })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE DECAL",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 3,
                        ItemSource = DecalLibrary.TypeList
                            .OrderBy(s => s.Name)
                            .Select(s =>
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => ActivateGodTool("Decal/" + s.Name)
                                })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE RAIL",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 1,
                        ItemSource = new HorizontalMenuTray.MenuItem[]
                        {
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "RAW PIECES",
                                ExpansionChild = new HorizontalMenuTray.Tray
                                {
                                    Columns = 2,
                                    ItemSize = new Point(200, 20),
                                    ItemSource = Rail.RailLibrary.EnumeratePieces().Select(p =>
                                        new HorizontalMenuTray.MenuItem
                                        {
                                            Text = p.Name,
                                            OnClick = (sender, args) => ActivateGodTool("Rail/" + p.Name)
                                        })
                                }
                            },

                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "USING PATTERNS",
                                ExpansionChild = new HorizontalMenuTray.Tray
                                {
                                    Columns = 1,
                                    ItemSource = Rail.RailLibrary.EnumeratePatterns().Select( p =>
                                        new HorizontalMenuTray.MenuItem
                                        {
                                            Text = p.Name,
                                            OnClick = (sender, args) =>
                                            {
                                                var railTool = Master.Tools[GameMaster.ToolMode.BuildRail] as Rail.BuildRailTool;
                                                railTool.Pattern = p;
                                                Master.ChangeTool(GameMaster.ToolMode.BuildRail);
                                                railTool.GodModeSwitch = true;
                                            }
                                        })
                                }
                            },

                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "PAINT",
                                OnClick = (sender, args) =>
                                {
                                    var railTool = Master.Tools[GameMaster.ToolMode.PaintRail] as Rail.PaintRailTool;
                                    railTool.SelectedResources = new List<ResourceAmount>(new ResourceAmount[] { new ResourceAmount("Rail", 1) });
                                    Master.ChangeTool(GameMaster.ToolMode.PaintRail);
                                    railTool.GodModeSwitch = true;
                                }
                            }
                        }
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "KILL THINGS",
                    OnClick = (sender, args) => ActivateGodTool("Kill Things")
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "DISEASE",
                    OnClick = (sender, args) => ActivateGodTool("Disease")
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "FILL WATER",
                    OnClick = (sender, args) => ActivateGodTool("Fill Water")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "FILL LAVA",
                    OnClick = (sender, args) => ActivateGodTool("Fill Lava")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "FIRE",
                    OnClick = (sender, args) => ActivateGodTool("Fire")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "TRADE ENVOY",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {

                            ItemSource = Master.World.Factions.Factions.Values.Where(f => f.Race.IsIntelligent && f != Master.Faction).Select(s =>
                            {
                                return new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => Master.World.Diplomacy.SendTradeEnvoy(s, Master.World)
                                };

                            }),
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "WAR PARTY",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {

                            ItemSource = Master.World.Factions.Factions.Values.Where(f => f.Race.IsIntelligent && f != Master.Faction).Select(s =>
                            {
                                return new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => Master.World.Diplomacy.SendWarParty(s)
                                };

                            }),
                    }
                },


                new HorizontalMenuTray.MenuItem
                {
                    Text = "DWARF BUX",
                    OnClick = (sender, args) => Master.Faction.AddMoney(100m)
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "PAY",
                    OnClick = (sender, args) => Master.PayEmployees()
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "STARVE",
                    OnClick = (sender, args) =>
                    {
                        foreach(var minion in Master.Faction.Minions)
                        {
                            minion.Status.Hunger.CurrentValue = 0;
                        }
                    }
                },
                 new HorizontalMenuTray.MenuItem
                {
                    Text = "XP",
                    OnClick = (sender, args) =>
                    {
                        foreach(var minion in Master.Faction.Minions)
                        {
                            minion.AddXP(100);
                        }
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "SPAWN TEST",
                    OnClick = (sender, args) =>
                    {
                        // Copy is required because spawning some types results in the creation of new types. EG, snakes create snake meat.
                        var keys = EntityFactory.EnumerateEntityTypes().ToList();
                        foreach(var key in keys)
                            EntityFactory.CreateEntity<GameComponent>(key, Master.World.CursorLightPos);
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "+1 HOUR",
                    OnClick = (sender, args) =>
                    {
                        Master.World.Time.CurrentDate += new TimeSpan(1, 0, 0);
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "FORCE REBUILD",
                    OnClick = (sender, args) =>
                    {
                         foreach (var chunk in Master.World.ChunkManager.ChunkData.ChunkMap)
                            for (int Y = 0; Y < VoxelConstants.ChunkSizeY; ++Y)
                                chunk.InvalidateSlice(Y);
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "REPULSE",
                    OnClick = (sender, args) => ActivateGodTool("Repulse")
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "SLOWMO",
                    OnClick = (sender, args) => GameSettings.Default.EnableSlowMotion = !GameSettings.Default.EnableSlowMotion
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "LET IT SNOW",
                    OnClick = (sender, args) =>
                    {
                        var storm = Weather.CreateStorm(Vector3.One, 100.0f, Master.World);
                        storm.TypeofStorm = StormType.SnowStorm;
                        storm.Start();
                    }
                }
            };

            base.Construct();
        }        
    }
}
