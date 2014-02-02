using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A static class describing all the kinds of rooms. Can create rooms using templates.
    /// </summary>
    public class RoomLibrary
    {
        private static Dictionary<string, RoomType> m_roomTypes = new Dictionary<string, RoomType>();
        private static bool staticIntialized = false;

        public static IEnumerable<string> GetRoomTypes()
        {
            return m_roomTypes.Keys;
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
            RoomType port = new RoomType("BalloonPort", 0, "Stockpile", balloonPortResources, balloonTemplates);
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

            RoomType bedroom = new RoomType("BedRoom", 0, "BrownTileFloor", bedroomResources, bedroomTemplates);
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

            RoomType commonRoom = new RoomType("CommonRoom", 1, "CobblestoneFloor", commonRoomResources, commonRoomTemplates);
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

            RoomType workshop = new RoomType("Workshop", 2, "CobblestoneFloor", commonRoomResources, workshopTemplates);
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

            RoomType training = new RoomType("TrainingRoom", 3, "CobblestoneFloor", commonRoomResources, trainingTemplates);
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

            RoomType library = new RoomType("Library", 4, "BlueTileFloor", commonRoomResources, libraryTemplates);
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


            RoomType wheatFarm = new RoomType("WheatFarm", 5, "TilledSoil", commonRoomResources, wheatTemplates);
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

            RoomType mushroomFarm = new RoomType("MushroomFarm", 6, "TilledSoil", commonRoomResources, mushroomTemplates);
            RegisterType(mushroomFarm);


            staticIntialized = true;
        }

        public static void RegisterType(RoomType t)
        {
            m_roomTypes[t.Name] = t;
        }

        public static RoomType GetType(string name)
        {
            return m_roomTypes.ContainsKey(name) ? m_roomTypes[name] : null;
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
            int count = Math.Max(room.Storage.Count / 12, 5);

            List<int> placedCount = room.RoomType.Templates.Select(template => 0).ToList();

            /*
            for(int i = 0; i < placedCount.Count + count; i++)
            {
                int k = PlayState.Random.Next(0, room.RoomType.Templates.Count);

                const int maxIters = 200;
                if(placedCount[k] >= count)
                {
                    continue;
                }

                RoomTemplate template = room.RoomType.Templates[k];

                if(template.PlacementType == PlacementType.Random)
                {
                    for(int j = 0; j < maxIters; j++)
                    {
                        int randomX = PlayState.Random.Next(0, currentTiles.GetLength(0));
                        int randomY = PlayState.Random.Next(0, currentTiles.GetLength(1));


                        if(template.CanRotate)
                        {
                            int randomOrientation = PlayState.Random.Next(0, 4);

                            template.RotateClockwise(randomOrientation);
                        }

                        if(template.PlaceTemplate(ref currentTiles, randomX, randomY) > 0)
                        {
                            break;
                        }
                    }
                }
                else
                {
                }
            }
             */
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

                    switch(tile)
                    {
                        case RoomTile.Wheat:
                            GameComponent wheat = EntityFactory.GenerateWheat(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(wheat as LocatableComponent);
                            thingsMade++;
                            break;

                        case RoomTile.Mushroom:
                            GameComponent mushroom = EntityFactory.GenerateMushroom(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(mushroom as LocatableComponent);
                            thingsMade++;
                            break;

                        case RoomTile.Table:
                            GameComponent table = EntityFactory.GenerateTable(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(table as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.Lamp:
                            GameComponent lamp = EntityFactory.GenerateLamp(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(lamp as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.Flag:
                            GameComponent flag = EntityFactory.GenerateFlag(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(flag as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.Chair:
                            GameComponent chair = EntityFactory.GenerateChair(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(chair as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.PotionTable:
                            GameComponent potionTable = EntityFactory.GeneratePotionTable(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(potionTable as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.BookTable:
                            GameComponent bookTable = EntityFactory.GenerateBookTable(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(bookTable as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.Anvil:
                            GameComponent anvil = EntityFactory.GenerateAnvil(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(anvil as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.Forge:
                            GameComponent forge = EntityFactory.GenerateForge(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(forge as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.Target:
                            GameComponent target = EntityFactory.GenerateTarget(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(target as LocatableComponent);
                            thingsMade++;
                            break;
                        case RoomTile.Strawman:
                            GameComponent strawman = EntityFactory.GenerateStrawman(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.AddItem(strawman as LocatableComponent);
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

                                    GameComponent bed = EntityFactory.GenerateBed(box.Min + new Vector3(r - 1, 1.0f, c - 1), componentManager, content, graphics);

                                    float angle = (float) Math.Atan2(dx, dy);
                                    LocatableComponent loc = (LocatableComponent) bed;

                                    Vector3 translation = loc.LocalTransform.Translation;
                                    Matrix bedRotation = Matrix.CreateRotationY(angle);
                                    loc.LocalTransform = Matrix.CreateTranslation(new Vector3(-0.5f, 0, -0.5f)) * bedRotation * Matrix.CreateTranslation(new Vector3(0.5f, 0, 0.5f)) * Matrix.CreateTranslation(translation);
                                    loc.BoundingBoxPos = Vector3.Transform(loc.BoundingBoxPos, bedRotation);
                                    loc.BoundingBox.Min = Vector3.Transform(loc.BoundingBox.Min - translation, bedRotation) + translation;
                                    loc.BoundingBox.Max = Vector3.Transform(loc.BoundingBox.Max - translation, bedRotation) + translation;;

                                    room.AddItem(bed as LocatableComponent);
                                    break;
                                }
                            }


                            thingsMade++;
                            break;
                        default:
                            break;
                    }
                }
            }

            Console.Out.WriteLine("Things made {0}", thingsMade);
        }

       
    }

}