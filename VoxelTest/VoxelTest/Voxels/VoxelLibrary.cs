﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A static collection of voxel types and their properties.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelLibrary
    {
        /// <summary>
        /// Specifies that a specific voxel is a resource which should
        /// spawn in veins.
        /// </summary>
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

        public static void CreateTransitionUVs(GraphicsDevice graphics, Texture2D textureMap, int width, int height, Point top, Point sides, Point bottom, Dictionary<TransitionTexture, BoxPrimitive.BoxTextureCoords> transitionTextures)
        {
            for(int i = 0; i < 16; i++)
            {
                Point topPoint = new Point(top.X + i, top.Y);
                transitionTextures[(TransitionTexture)i] = new BoxPrimitive.BoxTextureCoords(textureMap.Width, textureMap.Height, width, height, sides, sides, topPoint, bottom, sides, sides);
            }
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
                Name = "TilledSoil",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = true,
                IsBuildable = false,
                ParticleType = "dirt_particle"
            };
            RegisterType(tilledSoil, tilledSoilCube);

            VoxelType brownTileFloor = new VoxelType
            {
                Name = "BrownTileFloor",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "stone_particle"
            };
            RegisterType(brownTileFloor, brownTileCube);

            VoxelType blueTileFloor = new VoxelType
            {
                Name = "BlueTileFloor",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "stone_particle"
            };
            RegisterType(blueTileFloor, blueTileCube);

            VoxelType cobblestoneFloor = new VoxelType
            {
                Name = "CobblestoneFloor",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "stone_particle"
            };
            RegisterType(cobblestoneFloor, cobblestoneCube);

            VoxelType stockpileType = new VoxelType
            {
                Name = "Stockpile",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "stone_particle"
            };
            RegisterType(stockpileType, plankCube);

            VoxelType plankType = new VoxelType
            {
                Name = "Plank",
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = "Wood",
                StartingHealth = 20,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = true,
                ParticleType = "stone_particle"
            };

            VoxelType scaffoldType = new VoxelType
            {
                Name = "Scaffold",
                StartingHealth = 20,
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = "Wood",
                ReleasesResource = false,
                CanRamp = false,
                RampSize = 0.5f,
                IsBuildable = true,
                ParticleType = "stone_particle"
            };

            VoxelType grassType = new VoxelType
            {
                Name = "Grass",
                ProbabilityOfRelease = 0.1f,
                ResourceToRelease = "Dirt",
                StartingHealth = 10,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                HasTransitionTextures = true
            };

            //GrassType.RampPrimitives[RampType.None] = GrassCube;
            /*
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
             */
            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 3), new Point(2, 0), new Point(2, 0), grassType.TransitionTextures);
            


            VoxelType frostType = new VoxelType
            {
                Name = "Frost",
                ProbabilityOfRelease = 0.1f,
                ResourceToRelease = "Dirt",
                StartingHealth = 10,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                HasTransitionTextures = true
            };

            //FrostType.RampPrimitives[RampType.None] = FrostCube;
            /*
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
             */

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 4), new Point(2, 0), new Point(2, 0), frostType.TransitionTextures);

            emptyType = new VoxelType
            {
                Name = "empty",
                ReleasesResource = false,
                IsBuildable = false
            };


            VoxelType dirtType = new VoxelType
            {
                Name = "Dirt",
                ReleasesResource = true,
                ResourceToRelease = "Dirt",
                ProbabilityOfRelease = 0.3f,
                StartingHealth = 10,
                RampSize = 0.5f,
                CanRamp = true,
                IsBuildable = true,
                ParticleType = "dirt_particle"
            };

            VoxelType stoneType = new VoxelType
            {
                Name = "Stone",
                ProbabilityOfRelease = 0.5f,
                ReleasesResource = true,
                ResourceToRelease = "Stone",
                StartingHealth = 100,
                IsBuildable = true,
                ParticleType = "stone_particle"
            };

            VoxelType bedrockType = new VoxelType
            {
                Name = "Bedrock",
                StartingHealth = 10000,
                IsBuildable = false
            };

            VoxelType waterType = new VoxelType
            {
                Name = "water",
                ReleasesResource = false,
                IsBuildable = false,
                StartingHealth = 9999
            };

            VoxelType sandType = new VoxelType
            {
                Name = "Sand",
                ReleasesResource = false,
                StartingHealth = 5,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "sand_particle"
            };

            VoxelType ironType = new VoxelType
            {
                Name = "Iron",
                ProbabilityOfRelease = 0.99f,
                ReleasesResource = true,
                ResourceToRelease = "Iron",
                StartingHealth = 200,
                IsBuildable = false,
                ParticleType = "stone_particle"
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
                Name = "Gold",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Gold",
                StartingHealth = 200,
                IsBuildable = false,
                ParticleType = "stone_particle"
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
                Name = "Mana",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Mana",
                StartingHealth = 200,
                IsBuildable = false,
                ParticleType = "stone_particle"
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
            return PrimitiveMap.Keys.FirstOrDefault(v => v.Name == name);
        }

        public static BoxPrimitive GetPrimitive(string name)
        {
            return (from v in PrimitiveMap.Keys
                where v.Name == name
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