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
            AutoSizeColumns = true;
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
                            Text = Debugger.GetNicelyFormattedName(s.Name),
                            InitialState = s.State,
                            SetCallback = s.Set
                        })
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
                        Columns = 6,
                        AutoSizeColumns = false,
                        ItemSource = EntityFactory.EnumerateEntityTypes().Where(s => !s.Contains("Resource") || 
                        !ResourceLibrary.GetResourceByName(s.Substring(0, s.Length - " Resource".Length)).Generated).OrderBy(s => s).Select(s =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = s,
                                OnClick = (sender, args) => ActivateGodTool("Spawn/" + s),
                                TextHorizontalAlign = HorizontalAlign.Left,
                                TextVerticalAlign = VerticalAlign.Top
                            })
                    }
                },
                
                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE BLOCK",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 3,
                        ItemSource = VoxelLibrary.GetTypes()
                            .Where(t => t.Name != "_empty" && t.Name != "water")
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

                //new HorizontalMenuTray.MenuItem
                //{
                //    Text = "PLACE DECAL",
                //    ExpansionChild = new HorizontalMenuTray.Tray
                //    {
                //        Columns = 3,
                //        ItemSource = DecalLibrary.TypeList
                //            .OrderBy(s => s.Name)
                //            .Select(s =>
                //                new HorizontalMenuTray.MenuItem
                //                {
                //                    Text = s.Name,
                //                    OnClick = (sender, args) => ActivateGodTool("Decal/" + s.Name)
                //                })
                //    }
                //},

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
                    Text = "AUTO SAVE",
                    OnClick = (sender, args) =>
                    {
                        Master.World.gameState.StateManager.GetState<GameStates.PlayState>().AutoSave();
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "KILL THINGS",
                    OnClick = (sender, args) => ActivateGodTool("Kill Things")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "TRAILER",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSource = new List<HorizontalMenuTray.MenuItem>()
                        {
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "SPIN +",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Zero, 2.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "SPIN -",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Zero, -2.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "ZOOM -",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Zero, 0.0f, 2.5f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "ZOOM +",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Zero, 0.0f, -2.5f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "FWD",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Forward * 5, 0.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "BACK",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Backward * 5, 0.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "LEFT",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Left * 5, 0.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "RIGHT",
                                    OnClick = (sender, args) => this.Master.World.Camera.Trailer(Vector3.Right * 5, 0.0f, 0.0f),
                                },
                        }

                    }

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
                    Text = "EVENT",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                            ItemSource = Master.World.GoalManager.EventScheduler.Events.Events.Select(e =>
                            {
                                return new HorizontalMenuTray.MenuItem
                                {
                                    Text = e.Name,
                                    OnClick = (sender, args) => Master.World.GoalManager.EventScheduler.ActivateEvent(Master.World, e)
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
                    Text = "MINIONS",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSource = new HorizontalMenuTray.MenuItem[]
                        {
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
                                        minion.Status.Hunger.CurrentValue = 0;
                                }
                            },                
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "XP",
                                OnClick = (sender, args) =>
                                {
                                    foreach(var minion in Master.Faction.Minions)
                                        minion.AddXP(100);
                                } 
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "DISEASE",
                                OnClick = (sender, args) => ActivateGodTool("Disease")
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "HAPPY",
                                OnClick = (sender, args) =>
                                {
                                    foreach (var minion in Master.Faction.Minions)
                                    {
                                        var thoughts = minion.GetRoot().GetComponent<DwarfThoughts>();
                                        if (thoughts != null)
                                            thoughts.AddThought(Thought.ThoughtType.CheatedHappy);
                                    }
                                }
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "PISSED",
                                OnClick = (sender, args) =>
                                {
                                    foreach (var minion in Master.Faction.Minions)
                                    {
                                        var thoughts = minion.GetRoot().GetComponent<DwarfThoughts>();
                                        if (thoughts != null)
                                            thoughts.AddThought(Thought.ThoughtType.CheatedPissed);
                                    }
                                }
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "GAMBLE",
                                OnClick = (sender, args) =>
                                {
                                    foreach(var employee in Master.Faction.Minions)
                                    {
                                        employee.Status.Boredom.SetValue(employee.Status.Boredom.MinValue);
                                        employee.AddMoney(100);
                                        employee.AssignTask(new Scripting.GambleTask() { Priority = Task.PriorityType.High });
                                    }
                                }
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "PASS OUT",
                                OnClick = (sender, args) =>
                                {
                                    var employee = Datastructures.SelectRandom(Master.Faction.Minions);
                                    if (employee != null)
                                        employee.Creature.Heal(-employee.Status.Health.CurrentValue * employee.Creature.MaxHealth + 1);
                                }
                            }
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
                        int num = keys.Count();
                        float gridSize = (float)Math.Ceiling(Math.Sqrt((double)num));
                        Vector3 gridCenter = Master.World.CursorLightPos;
                        int i = 0;
                        for (float dx = -gridSize/2; dx <= gridSize/2; dx++)
                        {
                            for (float dz = -gridSize/2; dz <= gridSize/2; dz++)
                            {
                                if (i >= num)
                                {
                                    continue;
                                }

                                Vector3 pos = MathFunctions.Clamp(gridCenter + new Vector3(dx, VoxelConstants.ChunkSizeY, dz), Master.World.ChunkManager.Bounds);
                                VoxelHandle handle = VoxelHelpers.FindFirstVisibleVoxelOnRay(Master.World.ChunkManager.ChunkData, pos, pos + Vector3.Down * 100);
                                if (handle.IsValid)
                                {
                                    EntityFactory.CreateEntity<GameComponent>(keys[i], handle.WorldPosition + Vector3.Up);
                                }
                                i++;
                            }
                        }
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "SPAWN CRAFTS",
                    OnClick = (sender, args) =>
                    {
                        // Copy is required because spawning some types results in the creation of new types. EG, snakes create snake meat.
                        var itemTypes = CraftLibrary.EnumerateCraftables().Where(craft => craft.Type == CraftItem.CraftType.Object).ToList();
                        int num = itemTypes.Count();
                        float gridSize = (float)Math.Ceiling(Math.Sqrt((double)num));
                        Vector3 gridCenter = Master.World.CursorLightPos;

                        int i = 0;
                        for (float dx = -gridSize/2; dx <= gridSize/2; dx++)
                        {
                            for (float dz = -gridSize/2; dz <= gridSize/2; dz++)
                            {
                                if (i < num)
                                {
                                    var item = itemTypes[i];
                                    if (item.Name != "Explosive")
                                    {
                                        Vector3 pos = MathFunctions.Clamp(gridCenter + new Vector3(dx, VoxelConstants.ChunkSizeY, dz), Master.World.ChunkManager.Bounds);
                                        VoxelHandle handle = VoxelHelpers.FindFirstVisibleVoxelOnRay(Master.World.ChunkManager.ChunkData, pos, pos + Vector3.Down * 100);

                                        if (handle.IsValid)
                                        {

                                            var blackboard = new Blackboard();
                                            List<ResourceAmount> resources = item.RequiredResources.Select(r => new ResourceAmount(ResourceLibrary.GetResourcesByTag(r.Type).First(), r.Count)).ToList();
                                            blackboard.SetData<List<ResourceAmount>>("Resources", resources);
                                            blackboard.SetData<string>("CraftType", item.Name);

                                            var entity = EntityFactory.CreateEntity<GameComponent>(item.EntityName, handle.WorldPosition + Vector3.Up + item.SpawnOffset, blackboard);
                                            if (entity != null)
                                            {
                                                if (item.AddToOwnedPool)
                                                    Master.Faction.OwnedObjects.Add(entity as Body);
                                                if (item.Moveable)
                                                    entity.Tags.Add("Moveable");
                                                if (item.Deconstructable)
                                                    entity.Tags.Add("Deconstructable");
                                            }
                                        }
                                    }
                                }
                                i++;
                            }
                        }
                        
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
