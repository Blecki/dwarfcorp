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
                        ItemSize = new Point(200, 20),
                        ItemSource = Debugger.EnumerateSwitches().Select(s =>
                        new HorizontalMenuTray.CheckboxMenuItem
                        {
                            // Todo: Add spaces before capitals.
                            Text = s.Name,
                            InitialState = s.State,
                            SetCallback = s.Set
                        })
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
                    Text = "DUMP ENTITY JSON",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 5,
                        ItemSource = EntityFactory.EnumerateEntityTypes().OrderBy(s => s).Select(s =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = s,
                                OnClick = (sender, args) =>
                                {
                                    var body = EntityFactory.CreateEntity<Body>(s, Vector3.Zero);
                                    if (body != null)
                                    body.PropogateTransforms();

                                    System.IO.Directory.CreateDirectory("DUMPEDENTITIES");
                                    FileUtils.SaveJSon(body, "DUMPEDENTITIES/" + s + ".json", false);

                                    body.Delete();
                                }
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
                                                railTool.SelectedResources = new List<ResourceAmount>(new ResourceAmount[] { new ResourceAmount(ResourceType.Iron, 2) });
                                                Master.ChangeTool(GameMaster.ToolMode.BuildRail);
                                                railTool.GodModeSwitch = true;
                                            }
                                        })
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
                        // Todo: Figure out why the container gets MODIFIED during this.
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
