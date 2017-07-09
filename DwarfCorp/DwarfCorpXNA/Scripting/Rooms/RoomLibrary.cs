// RoomLibrary.cs
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
            RegisterType(TrainingRoom.InitializeData());
            RegisterType(WorkshopRoom.InitializeData());
            RegisterType(Kitchen.InitializeData());
            RegisterType(Graveyard.InitializeData());
            RegisterType(AnimalPen.InitializeData());
            RegisterType(Treasury.InitializeData());
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
            public VoxelHandle Vox;
        };

        public static bool FurnitureIntersects(PlacedFurniture a, PlacedFurniture B)
        {
            return a.OccupiedSpace.Intersects(B.OccupiedSpace);
        }

        public static bool FurnitureIntersects(PlacedFurniture a, List<PlacedFurniture> b)
        {
            return b.Any(p => FurnitureIntersects(a, p));
        }
      
        public static Room CreateRoom(Faction faction, string name, List<VoxelHandle> designations, bool blueprint, WorldManager world)
        {
            // TODO(mklingen): omg get rid of this horrible legacy function!
            if (name == BalloonPort.BalloonPortName)
            {
                return blueprint ? new BalloonPort(faction, true, designations, world) : new BalloonPort(faction, designations, world);
            } 
            else if (name == BedRoom.BedRoomName)
            {
                return blueprint ? new BedRoom(true, designations, world) : new BedRoom(designations, world);
            }
            else if (name == CommonRoom.CommonRoomName)
            {
                return blueprint ? new CommonRoom(true, designations, world) : new CommonRoom(designations, world);
            }
            else if (name == LibraryRoom.LibraryRoomName)
            {
                return blueprint ? new LibraryRoom(true, designations, world) : new LibraryRoom(designations, world);
            }
            else if (name == TrainingRoom.TrainingRoomName)
            {
                return blueprint ? new TrainingRoom(true, designations, world) : new TrainingRoom(designations, world); 
            }
            else if (name == WorkshopRoom.WorkshopName)
            {
                return blueprint ? new WorkshopRoom(true, designations, world) : new WorkshopRoom(designations, world); 
            }
            else if (name == Kitchen.KitchenName)
            {
                return blueprint ? new Kitchen(true, designations, world) : new Kitchen(designations, world); 
            }
            else if (name == Stockpile.StockpileName)
            {
                Stockpile toBuild = new Stockpile(faction, world);
                foreach (VoxelHandle voxel in designations)
                {
                    toBuild.AddVoxel(voxel);
                }
                return toBuild;
            }
            else if (name == Graveyard.GraveyardName)
            {
                return blueprint
                    ? new Graveyard(faction, true, designations, world)
                    : new Graveyard(faction, designations, world);
            }
            else if (name == AnimalPen.AnimalPenName)
            {
                return blueprint
                    ? new AnimalPen(true, designations, world) : 
                      new AnimalPen(designations, world);
            }
            else if (name == Treasury.TreasuryName)
            {
                Treasury toBuild = new Treasury(faction, designations, world);
                return toBuild;
            }
            else
            {
                return null;
            }
        }

        public static void BuildAllComponents(List<Body> components, Room room, ParticleManager particles)
        {
            foreach (Body createdComponent in components)
            {
                Vector3 endPos = createdComponent.LocalTransform.Translation;
                Matrix offsetTransform = createdComponent.LocalTransform;
                offsetTransform.Translation += new Vector3(0, -1, 0);
                createdComponent.LocalTransform = offsetTransform;
                createdComponent.AnimationQueue.Add(new EaseMotion(0.8f, offsetTransform, endPos));
                room.AddBody(createdComponent);
                particles.Trigger("puff", endPos + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 10);
                createdComponent.SetFlagRecursive(GameComponent.Flag.Active, true);
            }
        }

        public static List<Body> GenerateRoomComponentsTemplate(RoomData roomData, List<VoxelHandle> voxels , ComponentManager componentManager, 
            Microsoft.Xna.Framework.Content.ContentManager content, GraphicsDevice graphics)
        {
            List<Body> components = new List<Body>();
            RoomTile[,] currentTiles = RoomTemplate.CreateFromRoom(voxels, componentManager.World.ChunkManager);
            float[,] rotations = new float[currentTiles.GetLength(0), currentTiles.GetLength(1)];
            foreach (RoomTemplate myTemp in roomData.Templates)
            {
                RoomTemplate template = new RoomTemplate(myTemp) {Rotation = 0};
                for (int r = -2; r < currentTiles.GetLength(0) + 1; r++)
                {
                    for (int c = -2; c < currentTiles.GetLength(1) + 1; c++)
                    {
                        for (int rotation = 0; rotation < 5; rotation++)
                        {
                            template.PlaceTemplate(ref currentTiles, ref rotations, r, c);
                            template.RotateClockwise(1);
                        }
                    }
                }
            }

            BoundingBox box = MathFunctions.GetBoundingBox(voxels);
            int thingsMade = 0;
            for(int r = 0; r < currentTiles.GetLength(0); r++)
            {
                for(int c = 0; c < currentTiles.GetLength(1); c++)
                {
                    RoomTile tile = currentTiles[r, c];
                    Body createdComponent = null;
                    Vector3 noise =
                        VertexNoise.GetNoiseVectorFromRepeatingTexture(box.Min +
                                                                       new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1));
                    switch(tile)
                    {
                        case RoomTile.Barrel:
                            createdComponent = EntityFactory.CreateEntity<Body>("Barrel", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Wheat:
                            createdComponent = EntityFactory.CreateEntity<Body>("Wheat", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;

                        case RoomTile.Mushroom:
                            createdComponent = EntityFactory.CreateEntity<Body>("Mushroom", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;

                        case RoomTile.Table:
                            createdComponent = EntityFactory.CreateEntity<Body>("Table", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Stove:
                            createdComponent = EntityFactory.CreateEntity<Body>("Stove", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.KitchenTable:
                            createdComponent = EntityFactory.CreateEntity<Body>("Kitchen Table", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Lamp:
                            createdComponent = EntityFactory.CreateEntity<Body>("Lamp", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Flag:
                            createdComponent = EntityFactory.CreateEntity<Body>("Flag", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Chair:
                            createdComponent = EntityFactory.CreateEntity<Body>("Chair", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Books:
                            createdComponent = EntityFactory.CreateEntity<Body>(MathFunctions.RandEvent(0.5f) ? "Books" : "Potions", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Anvil:
                            createdComponent = EntityFactory.CreateEntity<Body>("Anvil", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Forge:
                            createdComponent = EntityFactory.CreateEntity<Body>("Forge", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Target:
                            createdComponent = EntityFactory.CreateEntity<Body>("Target", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.Strawman:
                            createdComponent = EntityFactory.CreateEntity<Body>("Strawman", box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1) + noise);
                            thingsMade++;
                            break;
                        case RoomTile.BookShelf:
                            createdComponent = EntityFactory.CreateEntity<Body>("Bookshelf", box.Min + new Vector3(r - 1 + 0.5f, 1.5f, c - 1 + 0.5f) + noise);
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

                                    createdComponent = EntityFactory.CreateEntity<Body>("Bed", box.Min + new Vector3(r - 1 + 0.5f, 1.5f, c - 1 + 0.5f) + noise);
                                    break;
                                }
                            }


                            thingsMade++;
                            break;
                        default:
                            break;
                    }

                    if (createdComponent == null) continue;
                    createdComponent.LocalTransform = Matrix.CreateRotationY(-(rotations[r, c] + (float)Math.PI * 0.5f)) * createdComponent.LocalTransform;
                    components.Add(createdComponent);
                }
            }
            return components;
        }

       
    }

}
