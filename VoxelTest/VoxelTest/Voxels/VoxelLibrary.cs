using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
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

        public static BoxPrimitive CreatePrimitive(GraphicsDevice graphics, Texture2D textureMap, int width, int height, Point top, Point sides, Point bottom)
        {
            BoxPrimitive.BoxTextureCoords coords = new BoxPrimitive.BoxTextureCoords(textureMap.Width, textureMap.Height, width, height, sides, sides, top, bottom, sides, sides);
            BoxPrimitive cube = new BoxPrimitive(graphics, 1.0f, 1.0f, 1.0f, coords);

            return cube;
        }

        public static void InitializeDefaultLibrary(GraphicsDevice graphics, Texture2D cubeTexture)
        {
            BoxPrimitive grassCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 0), new Point(2, 0), new Point(2, 0));
            BoxPrimitive dirtCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 0), new Point(2, 0), new Point(2, 0));
            BoxPrimitive stoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 2), new Point(1, 0), new Point(4, 2));
            BoxPrimitive sandCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 1), new Point(1, 1), new Point(1, 1));
            BoxPrimitive ironCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 1), new Point(1, 2), new Point(4, 1));
            BoxPrimitive goldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 1), new Point(0, 2), new Point(3, 1));
            BoxPrimitive manaCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 1), new Point(6, 1), new Point(7, 1));
            BoxPrimitive frostCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 1), new Point(2, 1), new Point(2, 0));
            BoxPrimitive scaffoldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 0), new Point(7, 0), new Point(7, 0));
            BoxPrimitive plankCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 0), new Point(4, 0), new Point(4, 0));
            BoxPrimitive waterCube = CreatePrimitive(graphics, cubeTexture, cubeTexture.Width, cubeTexture.Height, new Point(0, 0), new Point(0, 0), new Point(0, 0));
            BoxPrimitive cobblestoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 2), new Point(5, 2), new Point(5, 2));
            BoxPrimitive brownTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 0), new Point(5, 0), new Point(5, 0));
            BoxPrimitive blueTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 0), new Point(6, 0), new Point(6, 0));
            BoxPrimitive tilledSoilCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 1), new Point(2, 0), new Point(2, 0));

            VoxelType tilledSoil = new VoxelType
            {
                name = "TilledSoil",
                releasesResource = false,
                startingHealth = 20,
                canRamp = true,
                isBuildable = false,
                particleType = "dirt_particle"
            };
            RegisterType(tilledSoil, tilledSoilCube);

            VoxelType brownTileFloor = new VoxelType
            {
                name = "BrownTileFloor",
                releasesResource = false,
                startingHealth = 20,
                canRamp = false,
                isBuildable = false,
                particleType = "stone_particle"
            };
            RegisterType(brownTileFloor, brownTileCube);

            VoxelType blueTileFloor = new VoxelType
            {
                name = "BlueTileFloor",
                releasesResource = false,
                startingHealth = 20,
                canRamp = false,
                isBuildable = false,
                particleType = "stone_particle"
            };
            RegisterType(blueTileFloor, blueTileCube);

            VoxelType cobblestoneFloor = new VoxelType
            {
                name = "CobblestoneFloor",
                releasesResource = false,
                startingHealth = 20,
                canRamp = false,
                isBuildable = false,
                particleType = "stone_particle"
            };
            RegisterType(cobblestoneFloor, cobblestoneCube);

            VoxelType stockpileType = new VoxelType
            {
                name = "Stockpile",
                releasesResource = false,
                startingHealth = 20,
                canRamp = false,
                isBuildable = false,
                particleType = "stone_particle"
            };
            RegisterType(stockpileType, plankCube);

            VoxelType plankType = new VoxelType
            {
                name = "Plank",
                probabilityOfRelease = 1.0f,
                resourceToRelease = "Wood",
                startingHealth = 20,
                releasesResource = true,
                canRamp = true,
                rampSize = 0.5f,
                isBuildable = true,
                particleType = "stone_particle"
            };

            VoxelType scaffoldType = new VoxelType
            {
                name = "Scaffold",
                startingHealth = 20,
                probabilityOfRelease = 1.0f,
                resourceToRelease = "Wood",
                releasesResource = false,
                canRamp = false,
                rampSize = 0.5f,
                isBuildable = true,
                particleType = "stone_particle"
            };

            VoxelType grassType = new VoxelType
            {
                name = "Grass",
                probabilityOfRelease = 0.1f,
                resourceToRelease = "Dirt",
                startingHealth = 10,
                releasesResource = true,
                canRamp = true,
                rampSize = 0.5f,
                isBuildable = false,
                particleType = "dirt_particle",
                specialRampTextures = true
            };

            //GrassType.RampPrimitives[RampType.None] = GrassCube;
            grassType.RampPrimitives[RampType.All] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 4), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Front] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Back] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Front | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Front | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Back | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.Back | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 3), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.TopFrontRight] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 4), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.TopFrontLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 4), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.TopBackLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 4), new Point(2, 0), new Point(2, 0));
            grassType.RampPrimitives[RampType.TopBackRight] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 4), new Point(2, 0), new Point(2, 0));


            VoxelType frostType = new VoxelType
            {
                name = "Frost",
                probabilityOfRelease = 0.1f,
                resourceToRelease = "Dirt",
                startingHealth = 10,
                releasesResource = true,
                canRamp = true,
                rampSize = 0.5f,
                isBuildable = false,
                particleType = "dirt_particle",
                specialRampTextures = true
            };

            //FrostType.RampPrimitives[RampType.None] = FrostCube;
            frostType.RampPrimitives[RampType.All] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 4 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Front] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Back] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Front | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Front | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Back | RampType.Right] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.Back | RampType.Left] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 3 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.TopFrontRight] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 4 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.TopFrontLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 4 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.TopBackLeft] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 4 + 2), new Point(2, 0), new Point(2, 0));
            frostType.RampPrimitives[RampType.TopBackRight] = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 4 + 2), new Point(2, 0), new Point(2, 0));

            emptyType = new VoxelType();
            emptyType.name = "empty";
            emptyType.releasesResource = false;
            emptyType.isBuildable = false;


            VoxelType dirtType = new VoxelType
            {
                name = "Dirt",
                releasesResource = true,
                resourceToRelease = "Dirt",
                probabilityOfRelease = 0.3f,
                startingHealth = 10,
                rampSize = 0.5f,
                canRamp = true,
                isBuildable = true,
                particleType = "dirt_particle"
            };

            VoxelType stoneType = new VoxelType
            {
                name = "Stone",
                probabilityOfRelease = 0.5f,
                releasesResource = true,
                resourceToRelease = "Stone",
                startingHealth = 100,
                isBuildable = true,
                particleType = "stone_particle"
            };

            VoxelType bedrockType = new VoxelType
            {
                name = "Bedrock",
                startingHealth = 10000,
                isBuildable = false
            };

            VoxelType waterType = new VoxelType
            {
                name = "water",
                releasesResource = false,
                isBuildable = false,
                startingHealth = 9999
            };

            VoxelType sandType = new VoxelType
            {
                name = "Sand",
                releasesResource = false,
                startingHealth = 5,
                canRamp = true,
                rampSize = 0.5f,
                isBuildable = false,
                particleType = "sand_particle"
            };

            VoxelType ironType = new VoxelType
            {
                name = "Iron",
                probabilityOfRelease = 0.99f,
                releasesResource = true,
                resourceToRelease = "Iron",
                startingHealth = 200,
                isBuildable = false,
                particleType = "stone_particle"
            };

            ResourceSpawns["Iron"] = new ResourceSpawnRate
            {
                VeinSize = 0.1f,
                VeinSpawnThreshold = 0.8f,
                MinimumHeight = -100,
                MaximumHeight = 100,
                Probability = 0.9f
            };

            VoxelType goldType = new VoxelType
            {
                name = "Gold",
                probabilityOfRelease = 1.0f,
                releasesResource = true,
                resourceToRelease = "Gold",
                startingHealth = 200,
                isBuildable = false,
                particleType = "stone_particle"
            };

            ResourceSpawns["Gold"] = new ResourceSpawnRate
            {
                VeinSize = 0.055f,
                VeinSpawnThreshold = 0.8f,
                MinimumHeight = -100,
                MaximumHeight = 100,
                Probability = 0.5f
            };

            VoxelType manaType = new VoxelType
            {
                name = "Mana",
                probabilityOfRelease = 1.0f,
                releasesResource = true,
                resourceToRelease = "Mana",
                startingHealth = 200,
                isBuildable = false,
                particleType = "stone_particle"
            };

            ResourceSpawns["Mana"] = new ResourceSpawnRate
            {
                VeinSize = 0.03f,
                VeinSpawnThreshold = 0.86f,
                MinimumHeight = -1000,
                MaximumHeight = 1000,
                Probability = 0.5f
            };

            RegisterType(grassType, grassCube);
            RegisterType(frostType, frostCube);
            RegisterType(emptyType, null);
            RegisterType(dirtType, dirtCube);
            RegisterType(stoneType, stoneCube);
            RegisterType(waterType, waterCube);
            RegisterType(sandType, sandCube);
            RegisterType(ironType, ironCube);
            RegisterType(goldType, goldCube);
            RegisterType(manaType, manaCube);
            RegisterType(plankType, plankCube);
            RegisterType(scaffoldType, scaffoldCube);
            RegisterType(bedrockType, cobblestoneCube);
        }


        public static bool IsSolid(VoxelRef v)
        {
            return (v.TypeName != "" && v.TypeName != "empty" && v.TypeName != "water");
        }


        public static bool IsSolid(Voxel v)
        {
            return (v != null && v.Type != emptyType && v.Type != GetVoxelType("water"));
        }

        public static void RegisterType(VoxelType type, BoxPrimitive primitive)
        {
            PrimitiveMap[type] = primitive;
        }

        public static VoxelType GetVoxelType(short id)
        {
            // 0 is the "null" type
            return VoxelType.TypeList[id - 1];
        }

        public static VoxelType GetVoxelType(string name)
        {
            return PrimitiveMap.Keys.FirstOrDefault(v => v.name == name);
        }

        public static BoxPrimitive GetPrimitive(string name)
        {
            return (from v in PrimitiveMap.Keys
                where v.name == name
                select GetPrimitive(v)).FirstOrDefault();
        }

        public static BoxPrimitive GetPrimitive(VoxelType type)
        {
            if(PrimitiveMap.ContainsKey(type))
            {
                return PrimitiveMap[type];
            }
            else
            {
                return null;
            }
        }

        public static BoxPrimitive GetPrimitive(short id)
        {
            return GetPrimitive(GetVoxelType(id));
        }
    }

}