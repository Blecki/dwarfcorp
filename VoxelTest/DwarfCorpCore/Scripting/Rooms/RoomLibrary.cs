using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        private static Dictionary<string, RoomData> roomTypes = new Dictionary<string, RoomData>();
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
            RegisterType(Stockpile.InitializeData());
            RegisterType(BalloonPort.InitializeData());
            RegisterType(BedRoom.InitializeData());
            RegisterType(CommonRoom.InitializeData());
            RegisterType(LibraryRoom.InitializeData());
            RegisterType(MushroomFarm.InitializeData());
            RegisterType(TrainingRoom.InitializeData());
            RegisterType(WheatFarm.InitializeData());
            RegisterType(WorkshopRoom.InitializeData());
            staticIntialized = true;
        }

        public static void RegisterType(RoomData t)
        {
            roomTypes[t.Name] = t;
        }

        public static RoomData GetData(string name)
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
            public Voxel Vox;
        };

        public static bool FurnitureIntersects(PlacedFurniture a, PlacedFurniture B)
        {
            return a.OccupiedSpace.Intersects(B.OccupiedSpace);
        }

        public static bool FurnitureIntersects(PlacedFurniture a, List<PlacedFurniture> b)
        {
            return b.Any(p => FurnitureIntersects(a, p));
        }

        public static Room CreateRoom(string name, List<Voxel> designations, bool blueprint)
        {
            if (name == BalloonPort.BalloonPortName)
            {
                return blueprint ? new BalloonPort(true, designations, PlayState.ChunkManager) : new BalloonPort(designations, PlayState.ChunkManager);
            } 
            else if (name == BedRoom.BedRoomName)
            {
                return blueprint ? new BedRoom(true, designations, PlayState.ChunkManager) : new BedRoom(designations, PlayState.ChunkManager);
            }
            else if (name == CommonRoom.CommonRoomName)
            {
                return blueprint ? new CommonRoom(true, designations, PlayState.ChunkManager) : new CommonRoom(designations, PlayState.ChunkManager);
            }
            else if (name == LibraryRoom.LibraryRoomName)
            {
                return blueprint ? new LibraryRoom(true, designations, PlayState.ChunkManager) : new LibraryRoom(designations, PlayState.ChunkManager);
            }
            else if (name == MushroomFarm.MushroomFarmName)
            {
                return blueprint ? new MushroomFarm(true, designations, PlayState.ChunkManager) : new MushroomFarm(designations, PlayState.ChunkManager);
            }
            else if (name == TrainingRoom.TrainingRoomName)
            {
                return blueprint ? new TrainingRoom(true, designations, PlayState.ChunkManager) : new TrainingRoom(designations, PlayState.ChunkManager); 
            }
            else if (name == WheatFarm.WheatFarmName)
            {
                return blueprint ? new WheatFarm(true, designations, PlayState.ChunkManager) : new WheatFarm(designations, PlayState.ChunkManager); 
            }
            else if (name == WorkshopRoom.WorkshopName)
            {
                return blueprint ? new WorkshopRoom(true, designations, PlayState.ChunkManager) : new WorkshopRoom(designations, PlayState.ChunkManager); 
            }
            else if (name == Stockpile.StockpileName)
            {
                Stockpile toBuild = new Stockpile("Stockpile " + Stockpile.NextID(), PlayState.ChunkManager);
                foreach (Voxel voxel in designations)
                {
                    toBuild.AddVoxel(voxel);
                }
                return toBuild;
            }
            else
            {
                return null;
            }
        }

        public static void GenerateRoomComponentsTemplate(Room room, ComponentManager componentManager, Microsoft.Xna.Framework.Content.ContentManager content, GraphicsDevice graphics)
        {
            RoomTile[,] currentTiles = RoomTemplate.CreateFromRoom(room, room.Chunks);

            foreach (RoomTemplate template in room.RoomData.Templates)
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
                            createdComponent = EntityFactory.CreateEntity<Body>("Wheat", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;

                        case RoomTile.Mushroom:
                            createdComponent = EntityFactory.CreateEntity<Body>("Mushroom", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;

                        case RoomTile.Table:
                            createdComponent = EntityFactory.CreateEntity<Body>("Table", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.Lamp:
                            createdComponent = EntityFactory.CreateEntity<Body>("Lamp", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.Flag:
                            createdComponent = EntityFactory.CreateEntity<Body>("Flag", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.Chair:
                            createdComponent = EntityFactory.CreateEntity<Body>("Chair", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.BookTable:
                            createdComponent = EntityFactory.CreateEntity<Body>(MathFunctions.RandEvent(0.5f) ? "BookTable" : "PotionTable", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.Anvil:
                            createdComponent = EntityFactory.CreateEntity<Body>("Anvil", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.Forge:
                            createdComponent = EntityFactory.CreateEntity<Body>("Forge", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.Target:
                            createdComponent = EntityFactory.CreateEntity<Body>("Target", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                            thingsMade++;
                            break;
                        case RoomTile.Strawman:
                            createdComponent = EntityFactory.CreateEntity<Body>("Strawman", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
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

                                    createdComponent = EntityFactory.CreateEntity<Body>("Bed", box.Min + new Vector3(r - 1, 1.0f, c - 1));

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
                        room.AddBody(createdComponent);
                        PlayState.ParticleManager.Trigger("puff", endPos + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 10);
                    }
                }
            }
        }

       
    }

}