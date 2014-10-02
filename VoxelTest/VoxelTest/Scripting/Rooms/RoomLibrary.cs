using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A static class describing all the kinds of rooms. Can create rooms using templates.
    /// </summary>
    public class RoomLibrary
    {
        private static Dictionary<string, RoomType> roomTypes = new Dictionary<string, RoomType>();
        private static bool staticIntialized = false;

        public static IEnumerable<string> GetRoomTypes()
        {
            return roomTypes.Keys;
        }

        public RoomLibrary()
        {
            if(!staticIntialized)
            {
                InitializeStatics();
            }
        }

        public static void InitializeStatics()
        {

            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);

            RoomType stockpile = new RoomType("Stockpile", 7, "Stockpile", new Dictionary<string, ResourceAmount>(), new List<RoomTemplate>(), new ImageFrame(roomIcons, 16, 0, 0))
            {
                Description = "Used to store resources (8 per tile)."
            };

            RegisterType(stockpile);

            Dictionary<string, ResourceAmount> balloonPortResources = new Dictionary<string, ResourceAmount>();
            ResourceAmount balloonStoneRequired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources["Stone"],
                NumResources = 1
            };
            balloonPortResources["Stone"] = balloonStoneRequired;

            RoomTile[,] flagTemplate =
            {
                {
                    RoomTile.None,
                    RoomTile.Wall | RoomTile.Edge
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.Flag
                }
            };

            RoomTile[,] flagAccesories =
            {
                {
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate flag = new RoomTemplate(PlacementType.All, flagTemplate, flagAccesories);


            List<RoomTemplate> balloonTemplates = new List<RoomTemplate>
            {
                flag
            };
            RoomType port = new RoomType("BalloonPort", 0, "Stockpile", balloonPortResources, balloonTemplates, new ImageFrame(roomIcons, 16, 1, 0))
            {
                Description = "Balloons pick up / drop off resources here."
            };
            RegisterType(port);

            Dictionary<string, ResourceAmount> bedroomResources = new Dictionary<string, ResourceAmount>();
            ResourceAmount woodRequired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources["Wood"],
                NumResources = 1
            };
            bedroomResources["Wood"] = woodRequired;

            List<RoomTemplate> bedroomTemplates = new List<RoomTemplate>();

            RoomTile[,] bedTemplate =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.Pillow,
                    RoomTile.Bed
                },
                {
                    RoomTile.None,
                    RoomTile.Open,
                    RoomTile.None
                }
            };

            RoomTile[,] bedAccessories =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.Chair,
                    RoomTile.None
                }
            };
            RoomTemplate bed = new RoomTemplate(PlacementType.All, bedTemplate, bedAccessories);

            RoomTile[,] lampTemplate =
            {
                {
                    RoomTile.None,
                    RoomTile.Wall | RoomTile.Edge
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.Lamp
                }
            };

            RoomTile[,] lampAccessories =
            {
                {
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate lamp = new RoomTemplate(PlacementType.All, lampTemplate, lampAccessories);

            bedroomTemplates.Add(lamp);
            bedroomTemplates.Add(bed);

            RoomType bedroom = new RoomType("BedRoom", 0, "BrownTileFloor", bedroomResources, bedroomTemplates, new ImageFrame(roomIcons, 16, 2, 1))
            {
                Description = "Dwarves relax and rest here"
            };
            RegisterType(bedroom);

            Dictionary<string, ResourceAmount> commonRoomResources = new Dictionary<string, ResourceAmount>();
            commonRoomResources["Wood"] = woodRequired;

            ResourceAmount stoneRquired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources["Stone"],
                NumResources = 1
            };

            commonRoomResources["Stone"] = stoneRquired;


            List<RoomTemplate> commonRoomTemplates = new List<RoomTemplate>();

            RoomTile[,] tableTemps =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Table,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] tableAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.Chair,
                    RoomTile.None
                },
                {
                    RoomTile.Chair,
                    RoomTile.None,
                    RoomTile.Chair
                },
                {
                    RoomTile.None,
                    RoomTile.Chair,
                    RoomTile.None
                }
            };
            RoomTemplate table = new RoomTemplate(PlacementType.All, tableTemps, tableAcc);

            commonRoomTemplates.Add(lamp);
            commonRoomTemplates.Add(table);

            RoomType commonRoom = new RoomType("CommonRoom", 1, "CobblestoneFloor", commonRoomResources, commonRoomTemplates, new ImageFrame(roomIcons, 16, 2, 0))
            {
                Description = "Dwarves come here to socialize and drink"
            };
            RegisterType(commonRoom);


            List<RoomTemplate> workshopTemplates = new List<RoomTemplate>();

            RoomTile[,] anvilTemp =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Forge,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] anvilAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.Anvil,
                    RoomTile.None
                }
            };

            RoomTemplate anvil = new RoomTemplate(PlacementType.All, anvilTemp, anvilAcc);
            workshopTemplates.Add(anvil);

            RoomType workshop = new RoomType("Workshop", 2, "CobblestoneFloor", commonRoomResources, workshopTemplates, new ImageFrame(roomIcons, 16, 1, 1))
            {
                Description = "Craftsdwarves build mechanisms here"
            };
            RegisterType(workshop);

            List<RoomTemplate> trainingTemplates = new List<RoomTemplate>();

            RoomTile[,] targetTemp =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Target,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] strawAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.Strawman
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate straw = new RoomTemplate(PlacementType.All, targetTemp, strawAcc);

            trainingTemplates.Add(lamp);
            trainingTemplates.Add(straw);

            RoomType training = new RoomType("TrainingRoom", 3, "CobblestoneFloor", commonRoomResources, trainingTemplates, new ImageFrame(roomIcons, 16, 3, 0))
            {
                Description = "Military dwarves train here"
            };

            RegisterType(training);

            List<RoomTemplate> libraryTemplates = new List<RoomTemplate>();

            RoomTile[,] bookTemp =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.BookTable,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] bookAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.Chair
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate book = new RoomTemplate(PlacementType.Random, bookTemp, bookAcc);

            RoomTile[,] potionTemp =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.PotionTable,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] potionAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.Chair
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate potion = new RoomTemplate(PlacementType.Random, potionTemp, potionAcc);

            libraryTemplates.Add(lamp);
            libraryTemplates.Add(book);
            libraryTemplates.Add(potion);

            RoomType library = new RoomType("Library", 4, "BlueTileFloor", commonRoomResources, libraryTemplates, new ImageFrame(roomIcons, 16, 0, 1))
            {
                Description = "Mage dwarves do magical research here"
            };
            RegisterType(library);


            List<RoomTemplate> wheatTemplates = new List<RoomTemplate>();

            RoomTile[,] wheatTemp =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.Wheat,
                    RoomTile.Wheat,
                    RoomTile.Wheat
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] wheatAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate wheatFarmTemp = new RoomTemplate(PlacementType.All, wheatTemp, wheatAcc)
            {
                CanRotate = false
            };

            wheatTemplates.Add(wheatFarmTemp);


            RoomType wheatFarm = new RoomType("WheatFarm", 5, "TilledSoil", commonRoomResources, wheatTemplates, new ImageFrame(roomIcons, 16, 0, 2))
            {
                Description = "Dwarves can grow wheat above ground here"
            };
            RegisterType(wheatFarm);


            List<RoomTemplate> mushroomTemplates = new List<RoomTemplate>();

            RoomTile[,] mushTemp =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.Mushroom,
                    RoomTile.Mushroom,
                    RoomTile.Mushroom
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] mushAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate mushroomFarmTemp = new RoomTemplate(PlacementType.All, mushTemp, mushAcc);

            mushroomTemplates.Add(mushroomFarmTemp);
            mushroomFarmTemp.CanRotate = false;

            RoomType mushroomFarm = new RoomType("MushroomFarm", 6, "TilledSoil", commonRoomResources, mushroomTemplates, new ImageFrame(roomIcons, 16, 3, 1))
            {
                Description = "Dwarves can grow mushrooms below ground here"
            };
            RegisterType(mushroomFarm);

            staticIntialized = true;
        }

        public static void RegisterType(RoomType t)
        {
            roomTypes[t.Name] = t;
        }

        public static RoomType GetType(string name)
        {
            return roomTypes.ContainsKey(name) ? roomTypes[name] : null;
        }

        public enum FurnitureRotation
        {
            XMajor,
            ZMajor
        };

        public struct PlacedFurniture
        {
            public Rectangle OccupiedSpace;
            public FurnitureRotation Rotation;
            public VoxelRef Vox;
        };

        public static bool FurnitureIntersects(PlacedFurniture a, PlacedFurniture B)
        {
            return a.OccupiedSpace.Intersects(B.OccupiedSpace);
        }

        public static bool FurnitureIntersects(PlacedFurniture a, List<PlacedFurniture> b)
        {
            return b.Any(p => FurnitureIntersects(a, p));
        }

        public static void GenerateRoomComponentsTemplate(Room room, ComponentManager componentManager, Microsoft.Xna.Framework.Content.ContentManager content, GraphicsDevice graphics)
        {
            RoomTile[,] currentTiles = RoomTemplate.CreateFromRoom(room, room.Chunks);

            foreach (RoomTemplate template in room.RoomType.Templates)
            {
                for (int r = -2; r < currentTiles.GetLength(0) + 1; r++)
                {
                    for (int c = -2; c < currentTiles.GetLength(1) + 1; c++)
                    {
                        for (int rotation = 0; rotation < 5; rotation++)
                        {
                            template.PlaceTemplate(ref currentTiles, r, c);
                            template.RotateClockwise(1);
                        }
                    }
                }
            }

            BoundingBox box = room.GetBoundingBox();

            int thingsMade = 0;
            for(int r = 0; r < currentTiles.GetLength(0); r++)
            {
                for(int c = 0; c < currentTiles.GetLength(1); c++)
                {
                    RoomTile tile = currentTiles[r, c];
                    Body createdComponent = null;

                    switch(tile)
                    {
                        case RoomTile.Wheat:
                            createdComponent = (Body) EntityFactory.GenerateWheat(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;

                        case RoomTile.Mushroom:
                            createdComponent = (Body)EntityFactory.GenerateMushroom(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;

                        case RoomTile.Table:
                            createdComponent = (Body) EntityFactory.GenerateTable(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Lamp:
                            createdComponent = (Body) EntityFactory.GenerateLamp(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Flag:
                            createdComponent = (Body) EntityFactory.GenerateFlag(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Chair:
                            createdComponent = EntityFactory.GenerateChair(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.PotionTable:
                            createdComponent = (Body) EntityFactory.GeneratePotionTable(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.BookTable:
                            createdComponent = (Body) EntityFactory.GenerateBookTable(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Anvil:
                            createdComponent = (Body) EntityFactory.GenerateAnvil(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Forge:
                            createdComponent = (Body) EntityFactory.GenerateForge(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Target:
                            createdComponent = (Body) EntityFactory.GenerateTarget(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Strawman:
                            createdComponent = (Body) EntityFactory.GenerateStrawman(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            thingsMade++;
                            break;
                        case RoomTile.Pillow:

                            for(int dx = -1; dx < 2; dx++)
                            {
                                for(int dy = -1; dy < 2; dy++)
                                {
                                    if(Math.Abs(dx) + Math.Abs(dy) != 1 || r + dx < 0 || r + dx >= currentTiles.GetLength(0) || c + dy < 0 || c + dy >= currentTiles.GetLength(1))
                                    {
                                        continue;
                                    }

                                    if(currentTiles[r + dx, c + dy] != RoomTile.Bed)
                                    {
                                        continue;
                                    }

                                    createdComponent = EntityFactory.GenerateBed(box.Min + new Vector3(r - 1, 1.0f, c - 1), componentManager, content, graphics);

                                    float angle = (float) Math.Atan2(dx, dy);

                                    Vector3 translation = createdComponent.LocalTransform.Translation;
                                    Matrix bedRotation = Matrix.CreateRotationY(angle);
                                    createdComponent.LocalTransform = Matrix.CreateTranslation(new Vector3(-0.5f, 0, -0.5f)) * bedRotation * Matrix.CreateTranslation(new Vector3(0.5f, 0, 0.5f)) * Matrix.CreateTranslation(translation);
                                    createdComponent.BoundingBoxPos = Vector3.Transform(createdComponent.BoundingBoxPos, bedRotation);
                                    createdComponent.BoundingBox.Min = Vector3.Transform(createdComponent.BoundingBox.Min - translation, bedRotation) + translation;
                                    createdComponent.BoundingBox.Max = Vector3.Transform(createdComponent.BoundingBox.Max - translation, bedRotation) + translation; ;
                                    break;
                                }
                            }


                            thingsMade++;
                            break;
                        default:
                            break;
                    }

                    if(createdComponent != null)
                    {
                        Vector3 endPos = createdComponent.LocalTransform.Translation;
                        Matrix offsetTransform = createdComponent.LocalTransform;
                        offsetTransform.Translation += new Vector3(0, -1, 0);
                        createdComponent.LocalTransform = offsetTransform;
                        createdComponent.AnimationQueue.Add(new EaseMotion(0.8f, offsetTransform, endPos));

                        PlayState.ParticleManager.Trigger("puff", endPos + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 10);
                    }
                }
            }

            Console.Out.WriteLine("Things made {0}", thingsMade);
        }

       
    }

}