using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace DwarfCorp
{
    public class VoxelLibrary
    {
        public class ResourceSpawnRate
        {
            public float VeinSize;
            public float VeinSpawnThreshold;
            public float MinimumHeight;
            public float MaximumHeight;
            public float Probability;
        }

        public static Dictionary<VoxelType, BoxPrimitive> PrimitiveMap = new Dictionary<VoxelType, BoxPrimitive>();
        public static VoxelType emptyType = null;
        public static Dictionary<string, ResourceSpawnRate> ResourceSpawns = new Dictionary<string, ResourceSpawnRate>();

        public VoxelLibrary()
        {
        }

        public static BoxPrimitive CreatePrimitive(GraphicsDevice Graphics, Texture2D textureMap, int width, int height, Point top, Point sides, Point bottom)
        {
            BoxPrimitive.BoxTextureCoords coords = new BoxPrimitive.BoxTextureCoords(textureMap.Width, textureMap.Height, width, height, sides, sides, top, bottom, sides, sides);
            BoxPrimitive cube = new BoxPrimitive(Graphics, 1.0f, 1.0f, 1.0f, coords);

            return cube;
        }

        public static void InitializeDefaultLibrary(GraphicsDevice graphics, Texture2D cubeTexture)
        {
            BoxPrimitive GrassCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 0), new Point(2, 0), new Point(2, 0));
            BoxPrimitive DirtCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 0), new Point(2, 0), new Point(2, 0));
            BoxPrimitive StoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 2), new Point(1, 0), new Point(4, 2));
            BoxPrimitive SandCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 1), new Point(1, 1), new Point(1, 1));
            BoxPrimitive IronCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 1), new Point(1, 2), new Point(4, 1));
            BoxPrimitive GoldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 1), new Point(0, 2), new Point(3, 1));
            BoxPrimitive manaCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 1), new Point(6, 1), new Point(7, 1));
            BoxPrimitive FrostCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 1), new Point(2, 1), new Point(2, 0));
            BoxPrimitive ScaffoldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 0), new Point(7, 0), new Point(7, 0));
            BoxPrimitive PlankCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 0), new Point(4, 0), new Point(4, 0));
            BoxPrimitive waterCube = CreatePrimitive(graphics, cubeTexture, cubeTexture.Width, cubeTexture.Height, new Point(0, 0), new Point(0, 0), new Point(0, 0));
            BoxPrimitive cobblestoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 2), new Point(5, 2), new Point(5, 2));
            BoxPrimitive brownTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 0), new Point(5, 0), new Point(5, 0));
            BoxPrimitive blueTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 0), new Point(6, 0), new Point(6, 0));
            BoxPrimitive tilledSoilCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 1), new Point(2, 0), new Point(2, 0));

            VoxelType TilledSoil = new VoxelType();
            TilledSoil.name = "TilledSoil";
            TilledSoil.releasesResource = false;
            TilledSoil.startingHealth = 20;
            TilledSoil.canRamp = true;
            TilledSoil.isBuildable = false;
            TilledSoil.particleType = "dirt_particle";
            RegisterType(TilledSoil, tilledSoilCube);

            VoxelType BrownTileFloor = new VoxelType();
            BrownTileFloor.name = "BrownTileFloor";
            BrownTileFloor.releasesResource = false;
            BrownTileFloor.startingHealth = 20;
            BrownTileFloor.canRamp = false;
            BrownTileFloor.isBuildable = false;
            BrownTileFloor.particleType = "stone_particle";
            RegisterType(BrownTileFloor, brownTileCube);

            VoxelType BlueTileFloor = new VoxelType();
            BlueTileFloor.name = "BlueTileFloor";
            BlueTileFloor.releasesResource = false;
            BlueTileFloor.startingHealth = 20;
            BlueTileFloor.canRamp = false;
            BlueTileFloor.isBuildable = false;
            BlueTileFloor.particleType = "stone_particle";
            RegisterType(BlueTileFloor, blueTileCube);

            VoxelType CobblestoneFloor = new VoxelType();
            CobblestoneFloor.name = "CobblestoneFloor";
            CobblestoneFloor.releasesResource = false;
            CobblestoneFloor.startingHealth = 20;
            CobblestoneFloor.canRamp = false;
            CobblestoneFloor.isBuildable = false;
            CobblestoneFloor.particleType = "stone_particle";
            RegisterType(CobblestoneFloor, cobblestoneCube);

            VoxelType StockpileType = new VoxelType();
            StockpileType.name = "Stockpile";
            StockpileType.releasesResource = false;
            StockpileType.startingHealth = 20;
            StockpileType.canRamp = false;
            StockpileType.isBuildable = false;
            StockpileType.particleType = "stone_particle";
            RegisterType(StockpileType, PlankCube);

            VoxelType PlankType = new VoxelType();
            PlankType.name = "Plank";
            PlankType.probabilityOfRelease = 1.0f;
            PlankType.resourceToRelease = "Wood";
            PlankType.startingHealth = 20;
            PlankType.releasesResource = true;
            PlankType.canRamp = true;
            PlankType.rampSize = 0.5f;
            PlankType.isBuildable = true;
            PlankType.particleType = "stone_particle";

            VoxelType ScaffoldType = new VoxelType();
            ScaffoldType.name = "Scaffold";
            ScaffoldType.startingHealth = 20;
            ScaffoldType.probabilityOfRelease = 1.0f;
            ScaffoldType.resourceToRelease = "Wood";
            ScaffoldType.releasesResource = false;
            ScaffoldType.canRamp = false;
            ScaffoldType.rampSize = 0.5f;
            ScaffoldType.isBuildable = true;
            ScaffoldType.particleType = "stone_particle";


            VoxelType GrassType = new VoxelType();
            GrassType.name = "Grass";
            GrassType.probabilityOfRelease = 0.1f;
            GrassType.resourceToRelease = "Dirt";
            GrassType.startingHealth = 10;
            GrassType.releasesResource = true;
            GrassType.canRamp = true;
            GrassType.rampSize = 0.5f;
            GrassType.isBuildable = false;
            GrassType.particleType = "dirt_particle";

            GrassType.specialRampTextures = true;

            //GrassType.RampPrimitives[RampType.None] = GrassCube;
            GrassType.RampPrimitives[RampType.All] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 4), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Front] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Back] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Front | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Front | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Back | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.Back | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 3), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.TopFrontRight] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 4), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.TopFrontLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 4), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.TopBackLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 4), new Point(2, 0), new Point(2, 0));
            GrassType.RampPrimitives[RampType.TopBackRight] =  CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 4), new Point(2, 0), new Point(2, 0));


            VoxelType FrostType = new VoxelType();
            FrostType.name = "Frost";
            FrostType.probabilityOfRelease = 0.1f;
            FrostType.resourceToRelease = "Dirt";
            FrostType.startingHealth =10;
            FrostType.releasesResource = true;
            FrostType.canRamp = true;
            FrostType.rampSize = 0.5f;
            FrostType.isBuildable = false;
            FrostType.particleType = "dirt_particle";

            FrostType.specialRampTextures = true;

            //FrostType.RampPrimitives[RampType.None] = FrostCube;
            FrostType.RampPrimitives[RampType.All] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 4 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Front] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Back] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Front | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Front | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Back | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.Back | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 3 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.TopFrontRight] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 4 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.TopFrontLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 4 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.TopBackLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 4 + 2), new Point(2, 0), new Point(2, 0));
            FrostType.RampPrimitives[RampType.TopBackRight] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 4 + 2), new Point(2, 0), new Point(2, 0));

            emptyType = new VoxelType();
            emptyType.name = "empty";
            emptyType.releasesResource = false;
            emptyType.isBuildable = false;
           

            VoxelType  DirtType = new VoxelType();
            DirtType.name = "Dirt";
            DirtType.releasesResource = true;
            DirtType.resourceToRelease = "Dirt";
            DirtType.probabilityOfRelease = 0.3f;
            DirtType.startingHealth = 10;
            DirtType.rampSize = 0.5f;
            DirtType.canRamp = true;
            DirtType.isBuildable = true;
            DirtType.particleType = "dirt_particle";

            VoxelType StoneType = new VoxelType();
            StoneType.name = "Stone";
            StoneType.probabilityOfRelease = 0.5f;
            StoneType.releasesResource = true;
            StoneType.resourceToRelease = "Stone";
            StoneType.startingHealth = 100;
            StoneType.isBuildable = true;
            StoneType.particleType = "stone_particle";

            VoxelType waterType = new VoxelType();
            waterType.name = "water";
            waterType.releasesResource = false;
            waterType.isBuildable = false;

            VoxelType SandType = new VoxelType();
            SandType.name = "Sand";
            SandType.releasesResource = false;
            SandType.startingHealth = 5;
            SandType.canRamp = true;
            SandType.rampSize = 0.5f;
            SandType.isBuildable = false;
            SandType.particleType = "sand_particle";

            VoxelType IronType = new VoxelType();
            IronType.name = "Iron";
            IronType.probabilityOfRelease = 0.99f;
            IronType.releasesResource = true;
            IronType.resourceToRelease = "Iron";
            IronType.startingHealth = 200;
            IronType.isBuildable = false;
            IronType.particleType = "stone_particle";
            
            ResourceSpawns["Iron"] = new ResourceSpawnRate();
            ResourceSpawns["Iron"].VeinSize = 0.1f;
            ResourceSpawns["Iron"].VeinSpawnThreshold = 0.8f;
            ResourceSpawns["Iron"].MinimumHeight = -100;
            ResourceSpawns["Iron"].MaximumHeight = 100;
            ResourceSpawns["Iron"].Probability = 0.9f;

            VoxelType GoldType = new VoxelType();
            GoldType.name = "Gold";
            GoldType.probabilityOfRelease = 1.0f;
            GoldType.releasesResource = true;
            GoldType.resourceToRelease = "Gold";
            GoldType.startingHealth = 200;
            GoldType.isBuildable = false;
            GoldType.particleType = "stone_particle";

            ResourceSpawns["Gold"] = new ResourceSpawnRate();
            ResourceSpawns["Gold"].VeinSize = 0.055f;
            ResourceSpawns["Gold"].VeinSpawnThreshold = 0.8f;
            ResourceSpawns["Gold"].MinimumHeight = -100;
            ResourceSpawns["Gold"].MaximumHeight = 100;
            ResourceSpawns["Gold"].Probability = 0.5f;

            VoxelType manaType = new VoxelType();
            manaType.name = "Mana";
            manaType.probabilityOfRelease = 1.0f;
            manaType.releasesResource = true;
            manaType.resourceToRelease = "Mana";
            manaType.startingHealth = 200;
            manaType.isBuildable = false;
            manaType.particleType = "stone_particle";

            ResourceSpawns["Mana"] = new ResourceSpawnRate();
            ResourceSpawns["Mana"].VeinSize = 0.03f;
            ResourceSpawns["Mana"].VeinSpawnThreshold = 0.86f;
            ResourceSpawns["Mana"].MinimumHeight = -1000;
            ResourceSpawns["Mana"].MaximumHeight = 1000;
            ResourceSpawns["Mana"].Probability = 0.5f;

            RegisterType(GrassType, GrassCube);
            RegisterType(FrostType, FrostCube);
            RegisterType(emptyType, null);
            RegisterType(DirtType, DirtCube);
            RegisterType(StoneType, StoneCube);
            RegisterType(waterType, waterCube);
            RegisterType(SandType, SandCube);
            RegisterType(IronType, IronCube);
            RegisterType(GoldType, GoldCube);
            RegisterType(manaType, manaCube);
            RegisterType(PlankType, PlankCube);
            RegisterType(ScaffoldType, ScaffoldCube);
        }


        public static bool IsSolid(VoxelRef v)
        {
            return (v.TypeName != "" && v.TypeName != "empty" && v.TypeName != "water");
        }


        public static bool IsSolid(Voxel v)
        {
            return (v != null && v.Type!= emptyType && v.Type != GetVoxelType("water"));
        }

        public static void RegisterType(VoxelType type, BoxPrimitive primitive)
        {
            PrimitiveMap[type] = primitive;
        }

        public static VoxelType GetVoxelType(string name)
        {
            foreach (VoxelType v in PrimitiveMap.Keys)
            {
                if (v.name == name)
                {
                    return v;
                }
            }

            return null;
        }

        public static BoxPrimitive GetPrimitive(string name)
        {
            foreach (VoxelType v in PrimitiveMap.Keys)
            {
                if (v.name == name)
                {
                    return GetPrimitive(v);
                }
            }

            return null;
        }

        public static BoxPrimitive GetPrimitive(VoxelType type)
        {
            if (PrimitiveMap.ContainsKey(type))
            {
                return PrimitiveMap[type];
            }
            else
            {
                return null;
            }
        }

        

    }
}
