using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class RoomLibrary
    {
        static Dictionary<string, RoomType> m_roomTypes = new Dictionary<string, RoomType>();
        static bool staticIntialized = false;

        public static IEnumerable<string> GetRoomTypes() { return m_roomTypes.Keys; }

        public RoomLibrary()
        {
            if (!staticIntialized)
            {
                InitializeStatics();
            }
        }

        public static void InitializeStatics()
        {

            Dictionary<string, ResourceAmount> balloonPortResources = new Dictionary<string, ResourceAmount>();
            ResourceAmount BalloonStoneRequired = new ResourceAmount();
            BalloonStoneRequired.ResourceType = ResourceLibrary.Resources["Stone"];
            BalloonStoneRequired.NumResources = 0.1f;
            balloonPortResources["Stone"] = BalloonStoneRequired;

            RoomTile[,] flagTemplate = { { RoomTile.None, RoomTile.Wall | RoomTile.Edge},
                                         { RoomTile.Wall | RoomTile.Edge, RoomTile.Flag } };

            RoomTile[,] flagAccesories = { { RoomTile.None, RoomTile.None},
                                            { RoomTile.None, RoomTile.None } };

            RoomTemplate flag = new RoomTemplate(flagTemplate, flagAccesories);
            

            List<RoomTemplate> BalloonTemplates = new List<RoomTemplate>();
            BalloonTemplates.Add(flag);
            RoomType port = new RoomType("BalloonPort", 0, "Stockpile", balloonPortResources, BalloonTemplates);
            RegisterType(port);

            Dictionary<string, ResourceAmount> bedroomResources = new Dictionary<string, ResourceAmount>();
            ResourceAmount WoodRequired = new ResourceAmount();
            WoodRequired.ResourceType = ResourceLibrary.Resources["Wood"];
            WoodRequired.NumResources = 0.25f;
            bedroomResources["Wood"] = WoodRequired;

            List<RoomTemplate> bedroomTemplates = new List<RoomTemplate>();

            RoomTile[,] bedTemplate = { { RoomTile.None,                 RoomTile.None, RoomTile.None},
                                        { RoomTile.Wall | RoomTile.Edge, RoomTile.Pillow, RoomTile.Bed},
                                        { RoomTile.None,                 RoomTile.Open, RoomTile.None}};

            RoomTile[,] bedAccessories = { { RoomTile.None, RoomTile.None, RoomTile.None},
                                           { RoomTile.None, RoomTile.None, RoomTile.None},
                                           { RoomTile.None, RoomTile.Chair, RoomTile.None} };
            RoomTemplate bed = new RoomTemplate(bedTemplate, bedAccessories);

            RoomTile[,] lampTemplate = { { RoomTile.None, RoomTile.Wall | RoomTile.Edge},
                                         { RoomTile.Wall | RoomTile.Edge, RoomTile.Lamp } };

            RoomTile[,] lampAccessories = { { RoomTile.None, RoomTile.None},
                                            { RoomTile.None, RoomTile.None } };

            RoomTemplate lamp = new RoomTemplate(lampTemplate, lampAccessories);

            bedroomTemplates.Add(bed);
            bedroomTemplates.Add(lamp);

            RoomType bedroom = new RoomType("BedRoom", 0, "BrownTileFloor", bedroomResources, bedroomTemplates);
            RegisterType(bedroom);

            Dictionary<string, ResourceAmount> commonRoomResources = new Dictionary<string, ResourceAmount>();
            commonRoomResources["Wood"] = WoodRequired;

            ResourceAmount StoneRquired = new ResourceAmount();
            StoneRquired.ResourceType = ResourceLibrary.Resources["Stone"];
            StoneRquired.NumResources = 0.2f;

            commonRoomResources["Stone"] = StoneRquired;


            List<RoomTemplate> commonRoomTemplates = new List<RoomTemplate>();

            RoomTile[,] tableTemps = { { RoomTile.Open, RoomTile.Open, RoomTile.Open},
                                        {  RoomTile.Open, RoomTile.Table,  RoomTile.Open},
                                        { RoomTile.Open,  RoomTile.Open, RoomTile.Open}};

            RoomTile[,] tableAcc =         { { RoomTile.None, RoomTile.Chair, RoomTile.None},
                                           { RoomTile.Chair, RoomTile.None, RoomTile.Chair},
                                           { RoomTile.None, RoomTile.Chair, RoomTile.None} };
            RoomTemplate table = new RoomTemplate(tableTemps, tableAcc);

            commonRoomTemplates.Add(table);
            commonRoomTemplates.Add(lamp);

            RoomType commonRoom = new RoomType("CommonRoom", 1, "CobblestoneFloor", commonRoomResources, commonRoomTemplates);
            RegisterType(commonRoom);


            List<RoomTemplate> workshopTemplates = new List<RoomTemplate>();

            RoomTile[,] anvilTemp =   { { RoomTile.Open, RoomTile.Open, RoomTile.Open},
                                        {  RoomTile.Open, RoomTile.Forge,  RoomTile.Open},
                                        { RoomTile.Open,  RoomTile.Open, RoomTile.Open}};

            RoomTile[,] anvilAcc =    { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.None, RoomTile.None,  RoomTile.None},
                                        { RoomTile.None,  RoomTile.Anvil, RoomTile.None}};

            RoomTemplate anvil = new RoomTemplate(anvilTemp, anvilAcc);
            workshopTemplates.Add(anvil);

            RoomType workshop = new RoomType("Workshop", 2, "CobblestoneFloor", commonRoomResources, workshopTemplates);
            RegisterType(workshop);

            List<RoomTemplate> trainingTemplates = new List<RoomTemplate>();

            RoomTile[,] targetTemp =   { { RoomTile.Open, RoomTile.Open, RoomTile.Open},
                                        {  RoomTile.Open, RoomTile.Target,  RoomTile.Open},
                                        { RoomTile.Open,  RoomTile.Open, RoomTile.Open}};

            RoomTile[,] strawAcc =    { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.None, RoomTile.None,  RoomTile.Strawman},
                                        { RoomTile.None,  RoomTile.None, RoomTile.None}};

            RoomTemplate straw = new RoomTemplate(targetTemp, strawAcc);

            trainingTemplates.Add(straw);
            trainingTemplates.Add(lamp);

            RoomType training = new RoomType("TrainingRoom", 3, "CobblestoneFloor", commonRoomResources, trainingTemplates);
            RegisterType(training);

            List<RoomTemplate> libraryTemplates = new List<RoomTemplate>();

            RoomTile[,] bookTemp =   { { RoomTile.Open, RoomTile.Open, RoomTile.Open},
                                        {  RoomTile.Open, RoomTile.BookTable,  RoomTile.Open},
                                        { RoomTile.Open,  RoomTile.Open, RoomTile.Open}};

            RoomTile[,] bookAcc =    { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.None, RoomTile.None,  RoomTile.Chair},
                                        { RoomTile.None,  RoomTile.None, RoomTile.None}};

            RoomTemplate book = new RoomTemplate(bookTemp, bookAcc);

            RoomTile[,] potionTemp =   { { RoomTile.Open, RoomTile.Open, RoomTile.Open},
                                        {  RoomTile.Open, RoomTile.PotionTable,  RoomTile.Open},
                                        { RoomTile.Open,  RoomTile.Open, RoomTile.Open}};

            RoomTile[,] potionAcc =    { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.None, RoomTile.None,  RoomTile.Chair},
                                        { RoomTile.None,  RoomTile.None, RoomTile.None}};

            RoomTemplate potion = new RoomTemplate(potionTemp, potionAcc);

            libraryTemplates.Add(potion);
            libraryTemplates.Add(book);
            libraryTemplates.Add(lamp);

            RoomType library = new RoomType("Library", 4, "BlueTileFloor", commonRoomResources, libraryTemplates);
            RegisterType(library);


            List<RoomTemplate> wheatTemplates = new List<RoomTemplate>();

            RoomTile[,] wheatTemp =   { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.Wheat, RoomTile.Wheat,  RoomTile.Wheat},
                                        { RoomTile.Open,  RoomTile.Open, RoomTile.Open}};

            RoomTile[,] wheatAcc =    { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.None, RoomTile.None,  RoomTile.None},
                                        { RoomTile.None,  RoomTile.None, RoomTile.None}};

            RoomTemplate wheatFarmTemp = new RoomTemplate(wheatTemp, wheatAcc);
            wheatFarmTemp.CanRotate = false;

            wheatTemplates.Add(wheatFarmTemp);


            RoomType wheatFarm = new RoomType("WheatFarm", 5, "TilledSoil", commonRoomResources, wheatTemplates);
            RegisterType(wheatFarm);
         

            List<RoomTemplate> mushroomTemplates = new List<RoomTemplate>();

            RoomTile[,] mushTemp =   { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.Mushroom, RoomTile.Mushroom,  RoomTile.Mushroom},
                                        { RoomTile.Open,  RoomTile.Open, RoomTile.Open}};

            RoomTile[,] mushAcc =    { { RoomTile.None, RoomTile.None, RoomTile.None},
                                        {  RoomTile.None, RoomTile.None,  RoomTile.None},
                                        { RoomTile.None,  RoomTile.None, RoomTile.None}};

            RoomTemplate mushroomFarmTemp = new RoomTemplate(mushTemp, mushAcc);


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
            if (m_roomTypes.ContainsKey(name))
            {
                return m_roomTypes[name];
            }
            else
            {
                return null;
            }
        }

        public enum FurnitureRotation { XMajor, ZMajor };
        public struct PlacedFurniture { public Rectangle occupiedSpace; public FurnitureRotation rotation; public VoxelRef vox; };

        public static bool FurnitureIntersects(PlacedFurniture A, PlacedFurniture B)
        {
            return A.occupiedSpace.Intersects(B.occupiedSpace);
        }

        public static bool FurnitureIntersects(PlacedFurniture A, List<PlacedFurniture> B)
        {
            foreach (PlacedFurniture p in B)
            {
                if (FurnitureIntersects(A, p))
                {
                    return true;
                }
            }

            return false;
        }

        public static void GenerateRoomComponentsTemplate(Room room, ComponentManager componentManager, Microsoft.Xna.Framework.Content.ContentManager content, GraphicsDevice graphics)
        {
            RoomTile[,] currentTiles = RoomTemplate.CreateFromRoom(room, room.Chunks);
            int count = Math.Max(room.Storage.Count / 12, 1);

            List<int> placedCount = new List<int>();


            foreach (RoomTemplate template in room.RoomType.Templates)
            {
                placedCount.Add(0);
            }

            for(int i = 0; i < placedCount.Count + count; i++)
            {
                int k = PlayState.random.Next(0, room.RoomType.Templates.Count);

                int maxIters = 200;
                if (placedCount[k] < count)
                {
                    RoomTemplate template = room.RoomType.Templates[k];
                    for (int j = 0; j < maxIters; j++)
                    {
                        int randomX = PlayState.random.Next(0, currentTiles.GetLength(0) - 1);
                        int randomY = PlayState.random.Next(0, currentTiles.GetLength(1) - 1);


                        if (template.CanRotate)
                        {
                            int randomOrientation = PlayState.random.Next(0, 4);

                            template.RotateClockwise(randomOrientation);
                        }

                        if (template.PlaceTemplate(ref currentTiles, randomX, randomY) > 0)
                        {
                            break;
                        }
                    }
                }
            }

            BoundingBox box = room.GetBoundingBox();

            int thingsMade = 0;
            for (int r = 0; r < currentTiles.GetLength(0); r++)
            {
                for (int c = 0; c < currentTiles.GetLength(1); c++)
                {
                    RoomTile tile = currentTiles[r, c];

                    switch (tile)
                    {
                        case RoomTile.Wheat:
                            GameComponent wheat = EntityFactory.GenerateWheat(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(wheat);
                            thingsMade++;
                            break;

                        case RoomTile.Mushroom:
                            GameComponent mushroom = EntityFactory.GenerateMushroom(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(mushroom);
                            thingsMade++;
                            break;

                        case RoomTile.Table:
                            GameComponent table = EntityFactory.GenerateTable(box.Min  + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(table);
                            thingsMade++;
                            break;
                        case RoomTile.Lamp:
                            GameComponent lamp = EntityFactory.GenerateLamp(box.Min + new Vector3(r + 0.5f - 1 , 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(lamp);
                            thingsMade++;
                            break;
                        case RoomTile.Flag:
                            GameComponent flag = EntityFactory.GenerateFlag(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(flag);
                            thingsMade++;
                            break;
                        case RoomTile.Chair:
                            GameComponent chair = EntityFactory.GenerateChair(box.Min  + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(chair);
                            thingsMade++;
                            break;
                        case RoomTile.PotionTable:
                            GameComponent potionTable = EntityFactory.GeneratePotionTable(box.Min  + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(potionTable);
                            thingsMade++;
                            break;
                        case RoomTile.BookTable:
                            GameComponent bookTable = EntityFactory.GenerateBookTable(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(bookTable);
                            thingsMade++;
                            break;
                        case RoomTile.Anvil:
                            GameComponent anvil = EntityFactory.GenerateAnvil(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(anvil);
                            thingsMade++;
                            break;
                        case RoomTile.Forge:
                            GameComponent forge = EntityFactory.GenerateForge(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(forge);
                            thingsMade++;
                            break;
                        case RoomTile.Target:
                            GameComponent target = EntityFactory.GenerateTarget(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(target);
                            thingsMade++;
                            break;
                        case RoomTile.Strawman:
                            GameComponent strawman = EntityFactory.GenerateStrawman(box.Min + new Vector3(r + 0.5f - 1, 1.5f, c + 0.5f - 1), componentManager, content, graphics);
                            room.Components.Add(strawman);
                            thingsMade++;
                            break;
                        case RoomTile.Pillow:

                            for (int dx = -1; dx < 2; dx++)
                            {
                                for (int dy = -1; dy < 2; dy++)
                                {
                                    if (Math.Abs(dx) + Math.Abs(dy) == 1 && r + dx >= 0 && r + dx < currentTiles.GetLength(0) && c + dy >= 0 && c + dy < currentTiles.GetLength(1))
                                    {
                                        if (currentTiles[r + dx, c + dy] == RoomTile.Bed)
                                        {
                                            GameComponent bed = EntityFactory.GenerateBed(box.Min + new Vector3(r - 1, 1.0f, c  - 1), componentManager, content, graphics);
                                            room.Components.Add(bed);
                                            float angle = (float)Math.Atan2(dx, dy);
                                            LocatableComponent loc = (LocatableComponent)bed;

                                            loc.LocalTransform = Matrix.CreateTranslation(new Vector3(-0.5f, 0, -0.5f)) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(new Vector3(0.5f, 0, 0.5f)) * Matrix.CreateTranslation(loc.LocalTransform.Translation);
                                      
                                            break;
                                        }
                                    }
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

        public static void GenerateRoomComponents(Room room, ComponentManager componentManager, Microsoft.Xna.Framework.Content.ContentManager content, GraphicsDevice graphics)
        {
           
            if (room.RoomType == GetType("BedRoom"))
            {
                if (room.Storage.Count > 1)
                {
                    int maxBeds = room.Storage.Count / 4;

                    BoundingBox box = room.GetBoundingBox();

                    List<PlacedFurniture> placedFurniture = new List<PlacedFurniture>();

                    for (int i = 0; i < maxBeds; i++)
                    {
                        foreach (VoxelStorage storage in room.Storage)
                        {
                            VoxelRef voxel = storage.Voxel;
                            PlacedFurniture furniture = new PlacedFurniture();
                            furniture.occupiedSpace = new Rectangle((int)voxel.WorldPosition.X, (int)voxel.WorldPosition.Z, 2, 1);
                            furniture.vox = voxel;
                            furniture.rotation = FurnitureRotation.ZMajor;

                            if (!FurnitureIntersects(furniture, placedFurniture))
                            {
                                placedFurniture.Add(furniture);

                                GameComponent bed = EntityFactory.GenerateBed(voxel.WorldPosition + new Vector3(0.0f, 0.85f, 0.0f), componentManager, content, graphics);
                                room.Components.Add(bed);
                                break;
                            }
                        }
                    }

                    /*
                    BoundingBox box = room.GetBoundingBox();
                    float y = box.Min.Y;
                    List<int> existingIndecies = new List<int>();
                    for (float x = box.Min.X + 1.0f; x < box.Max.X - 1.0f; x += 2.0f)
                    {
                        for (float z = box.Min.Z + 1.0f; z < box.Max.Z - 2.0f; z += 4.0f)
                        {
                            int index = room.GetClosestVoxelTo(new Vector3(x, y, z));


                            if (!existingIndecies.Contains(index))
                            {
                                GameComponent bed = EntityFactory.GenerateBed(room.Voxels[index].WorldPosition + new Vector3(0.0f, 0.85f, 0.0f), componentManager, content, graphics);
                                existingIndecies.Add(index);
                                room.Components.Add(bed);
                            }
                        }
                    }
                     */

                    float y = box.Min.Y;
                    for (float x = box.Min.X; x <= box.Max.X; x += (box.Max.X - box.Min.X))
                    {
                        for (float z = box.Min.Z; z <= box.Max.Z; z += (box.Max.Z - box.Min.Z))
                        {
                            VoxelRef voxel = room.GetNearestFreeVoxel(new Vector3(x, y, z));

                            GameComponent lamp = EntityFactory.GenerateLamp(voxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f), componentManager, content, graphics);
                            room.Components.Add(lamp);
                        }
                    }


                }
            }
            else if (room.RoomType.Name == "CommonRoom")
            {
                BoundingBox box = room.GetBoundingBox();
                float y = box.Min.Y;


                for (float x = box.Min.X + 1.0f; x < box.Max.X; x += 2.0f)
                {
                    for (float z = box.Min.Z + 1.0f; z < box.Max.Z; z += 2.0f)
                    {
                        VoxelRef voxel = room.GetNearestFreeVoxel(new Vector3(x, y, z));

                        GameComponent table = EntityFactory.GenerateTable(voxel.WorldPosition + new Vector3(0.0f, 1.2f, 0.0f), componentManager, content, graphics);
                        room.Components.Add(table);

                    }
                }

                for (float x = box.Min.X; x <= box.Max.X; x += (box.Max.X - box.Min.X))
                {
                    for (float z = box.Min.Z; z <= box.Max.Z; z += (box.Max.Z - box.Min.Z))
                    {
                        VoxelRef voxel = room.GetNearestFreeVoxel(new Vector3(x, y, z));

                        GameComponent lamp = EntityFactory.GenerateLamp(voxel.WorldPosition + new Vector3(0.5f, 1.2f, 0.5f), componentManager, content, graphics);
                        room.Components.Add(lamp);
                    }
                }
            }
        }

    }

}
