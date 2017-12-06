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
                        ItemSource = EntityFactory.EntityFuncs.Keys.OrderBy(s => s).Select(s =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = s,
                                OnClick = (sender, args) => ActivateGodTool("Spawn/" + s)
                            })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE",
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
                    Text = "SPAWN TEST",
                    OnClick = (sender, args) =>
                    {
                        var keys = EntityFactory.EntityFuncs.Keys.ToList();
                        foreach(var key in keys)
                        {
                            EntityFactory.CreateEntity<GameComponent>(key, Master.World.CursorLightPos);
                        }

                    }
                },

                // Shouldn't this go into some kind of 'debug' menu?
                new HorizontalMenuTray.MenuItem
                {
                    Text = "DRAW PATHS",
                    OnClick = (sender, args) =>
                    {
                        GameSettings.Default.DrawPaths = !GameSettings.Default.DrawPaths;
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
                    Text = "LET IT SNOW",
                    OnClick = (sender, args) =>
                    {
                        var storm = Weather.CreateStorm(Vector3.One, 100.0f, Master.World);
                        storm.TypeofStorm = StormType.SnowStorm;
                        storm.Start();
                    }
                },

            };

            base.Construct();
        }        
    }
}
