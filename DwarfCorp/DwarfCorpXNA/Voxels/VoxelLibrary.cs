// VoxelLibrary.cs
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

        public static Dictionary<string, VoxelType> Types = new Dictionary<string, VoxelType>(); 

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
            if (PrimitiveMap.Count > 0) return;

            BoxPrimitive grassCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 0), new Point(2, 0), new Point(2, 0));
            BoxPrimitive dirtCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 0), new Point(2, 0), new Point(2, 0));
            BoxPrimitive stoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 2), new Point(1, 0), new Point(4, 2));
            BoxPrimitive sandCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 1), new Point(1, 1), new Point(1, 1));
            BoxPrimitive ironCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 1), new Point(1, 2), new Point(4, 1));
            BoxPrimitive goldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 1), new Point(0, 2), new Point(3, 1));
            BoxPrimitive coalCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 2), new Point(2, 2), new Point(2, 2));
            BoxPrimitive manaCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 1), new Point(6, 1), new Point(7, 1));
            BoxPrimitive frostCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 1), new Point(2, 1), new Point(2, 0));
            BoxPrimitive snowCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 7), new Point(3, 7), new Point(3, 7));
            BoxPrimitive iceCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 7), new Point(2, 7), new Point(2, 7));
            BoxPrimitive scaffoldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 0), new Point(7, 0), new Point(7, 0));
            BoxPrimitive plankCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 0), new Point(4, 0), new Point(4, 0));
            BoxPrimitive waterCube = CreatePrimitive(graphics, cubeTexture, cubeTexture.Width, cubeTexture.Height, new Point(0, 0), new Point(0, 0), new Point(0, 0));
            BoxPrimitive cobblestoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 2), new Point(5, 2), new Point(5, 2));
            BoxPrimitive magicCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 10), new Point(0, 10), new Point(0, 10));
            BoxPrimitive bedrockCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 2), new Point(6, 2), new Point(6, 2));
            BoxPrimitive brownTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 0), new Point(5, 0), new Point(5, 0));
            BoxPrimitive blueTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 0), new Point(6, 0), new Point(6, 0));
            BoxPrimitive tilledSoilCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 1), new Point(2, 0), new Point(2, 0));

            BoxPrimitive redGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 11), new Point(0, 12), new Point(0, 11));
            BoxPrimitive orangeGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 11), new Point(1, 12), new Point(1, 11));
            BoxPrimitive yellowGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 11), new Point(2, 12), new Point(2, 11));
            BoxPrimitive greenGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 11), new Point(3, 12), new Point(3, 11));
            BoxPrimitive blueGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 11), new Point(4, 12), new Point(4, 11));
            BoxPrimitive purpleGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 11), new Point(5, 12), new Point(5, 11));
            
            emptyType = new VoxelType
            {
                Name = "empty",
                ReleasesResource = false,
                IsBuildable = false
            };

            VoxelType tilledSoil = new VoxelType
            {
                Name = "TilledSoil",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = true,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                IsSoil = true,
                IsSurface = true
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
                ParticleType = "stone_particle",
                HasTransitionTextures = true
            };
            RegisterType(cobblestoneFloor, cobblestoneCube);
            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 8), new Point(5, 2), new Point(5, 2), cobblestoneFloor.TransitionTextures);
            
            VoxelType stockpileType = new VoxelType
            {
                Name = "Stockpile",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true
            };
            RegisterType(stockpileType, plankCube);

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 9), new Point(4, 0), new Point(4, 0), stockpileType.TransitionTextures);
            
            VoxelType plankType = new VoxelType
            {
                Name = "Plank",
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Wood,
                StartingHealth = 20,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = true,
                ParticleType = "stone_particle",
                HasTransitionTextures = true
            };


            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 9), new Point(4, 0), new Point(4, 0), plankType.TransitionTextures);

            VoxelType magicType = new VoxelType
            {
                Name = "Magic",
                ProbabilityOfRelease = 0.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Mana,
                StartingHealth = 1,
                ReleasesResource = true,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "star_particle",
                ExplosionSound = ContentPaths.Audio.wurp,
                HasTransitionTextures = false,
                EmitsLight = true
            };


            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 10), new Point(15, 10), new Point(15, 10), magicType.TransitionTextures);


            VoxelType scaffoldType = new VoxelType
            {
                Name = "Scaffold",
                StartingHealth = 20,
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Wood,
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
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                StartingHealth = 10,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                HasTransitionTextures = true,
                IsSoil = true,
                IsSurface = true
            };

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 3), new Point(2, 0), new Point(2, 0), grassType.TransitionTextures);
            


            VoxelType frostType = new VoxelType
            {
                Name = "Frost",
                ProbabilityOfRelease = 0.1f,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                StartingHealth = 10,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                HasTransitionTextures = true,
                IsSurface = true
            };

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 4), new Point(2, 0), new Point(2, 0), frostType.TransitionTextures);

            VoxelType snowType = new VoxelType
            {
                Name = "Snow",
                StartingHealth = 1,
                ReleasesResource = false,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "snow_particle",
                HasTransitionTextures = false,
                IsSurface = true,
                IsSoil = false
            };
            RegisterType(snowType, snowCube);

            VoxelType iceType = new VoxelType
            {
                Name = "Ice",
                StartingHealth = 1,
                ReleasesResource = false,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "snow_particle",
                HasTransitionTextures = false,
                IsSurface = true,
                IsSoil = false
            };
            RegisterType(iceType, iceCube);


            VoxelType desertGrass = new VoxelType
            {
                Name = "DesertGrass",
                ProbabilityOfRelease = 0.1f,
                ResourceToRelease = ResourceLibrary.ResourceType.Sand,
                StartingHealth = 20,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "sand_particle",
                HasTransitionTextures = true,
                IsSurface = true,
                IsSoil = true
            };

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 6), new Point(1, 1), new Point(1, 1), desertGrass.TransitionTextures);

            VoxelType jungleGrass = new VoxelType
            {
                Name = "JungleGrass",
                ProbabilityOfRelease = 0.1f,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                StartingHealth = 30,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                HasTransitionTextures = true,
                IsSurface = true,
                IsSoil = true
            };

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 5), new Point(2, 0), new Point(2, 0), jungleGrass.TransitionTextures);


            VoxelType caveFungus = new VoxelType
            {
                Name = "CaveFungus",
                ProbabilityOfRelease = 0.25f,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                StartingHealth = 30,
                ReleasesResource = true,
                CanRamp = false,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                IsSurface = false,
                IsSoil = true
            };

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 13), new Point(1, 0), new Point(1, 0), caveFungus.TransitionTextures);


            VoxelType dirtType = new VoxelType
            {
                Name = "Dirt",
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                ProbabilityOfRelease = 0.3f,
                StartingHealth = 10,
                RampSize = 0.5f,
                CanRamp = true,
                IsBuildable = true,
                ParticleType = "dirt_particle",
                IsSoil = true
            };

            VoxelType stoneType = new VoxelType
            {
                Name = "Stone",
                ProbabilityOfRelease = 0.5f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                StartingHealth = 40,
                IsBuildable = true,
                ParticleType = "stone_particle"
            };

            VoxelType bedrockType = new VoxelType
            {
                Name = "Bedrock",
                StartingHealth = 255,
                IsBuildable = false,
                IsInvincible = true
            };

            VoxelType waterType = new VoxelType
            {
                Name = "water",
                ReleasesResource = false,
                IsBuildable = false,
                StartingHealth = 255
            };

            VoxelType sandType = new VoxelType
            {
                Name = "Sand",
                ReleasesResource = true,
                StartingHealth = 15,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "sand_particle",
                IsSurface = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Sand,
                ProbabilityOfRelease = 0.5f
            };

            VoxelType ironType = new VoxelType
            {
                Name = "Iron",
                ProbabilityOfRelease = 0.99f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Iron,
                StartingHealth = 80,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 5,
                SpawnVeins = true,
                VeinLength = 10,
                Rarity = 0.0f,
                MinSpawnHeight = 8,
                MaxSpawnHeight = 40,
                SpawnProbability = 0.99f
            };

           
            VoxelType coalType = new VoxelType
            {
                Name = "Coal",
                ProbabilityOfRelease = 0.99f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Coal,
                StartingHealth = 75,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 5,
                MinSpawnHeight = 15,
                MaxSpawnHeight = 50,
                SpawnProbability = 0.3f,
                Rarity = 0.05f,
                SpawnOnSurface = true
            };


            VoxelType goldType = new VoxelType
            {
                Name = "Gold",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Gold,
                StartingHealth = 90,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnVeins = true,
                VeinLength = 20,
                Rarity = 0.2f,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 20,
                SpawnProbability = 0.99f
            };

            VoxelType greenGem = new VoxelType
            {
                Name = "Emerald",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Emerald",
                StartingHealth = 90,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f
            };

            VoxelType redGem = new VoxelType
            {
                Name = "Ruby",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Ruby",
                StartingHealth = 90,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f
            };


            VoxelType purpleGem = new VoxelType
            {
                Name = "Amethyst",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Amethyst",
                StartingHealth = 90,
                IsBuildable = false,
                ParticleType = "stone_particle",
                ClusterSize = 3,
                SpawnClusters = true,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f
            };

            VoxelType blueGem = new VoxelType
            {
                Name = "Sapphire",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Sapphire",
                StartingHealth = 90,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f
            };

            VoxelType yellowGem = new VoxelType
            {
                Name = "Citrine",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Citrine",
                StartingHealth = 90,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f
            };

            VoxelType orangeGem = new VoxelType
            {
                Name = "Garnet",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Garnet",
                StartingHealth = 90,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f
            };

         
            VoxelType manaType = new VoxelType
            {
                Name = "Mana",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Mana,
                StartingHealth = 200,
                IsBuildable = false,
                ParticleType = "stone_particle",
                SpawnVeins = true,
                VeinLength = 25,
                Rarity = 0.1f,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 14,
                SpawnProbability = 0.99f
            };

            RegisterType(greenGem, greenGemCube);
            RegisterType(redGem, redGemCube);
            RegisterType(purpleGem, purpleGemCube);
            RegisterType(blueGem, blueGemCube);
            RegisterType(orangeGem, orangeGemCube);
            RegisterType(yellowGem, yellowGemCube);

            RegisterType(grassType, grassCube);
            RegisterType(frostType, frostCube);
            RegisterType(desertGrass, grassCube);
            RegisterType(jungleGrass, grassCube);
            RegisterType(caveFungus, grassCube);
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
            RegisterType(bedrockType, bedrockCube);
            RegisterType(coalType, coalCube);
            RegisterType(magicType, magicCube);

            foreach (VoxelType type in VoxelType.TypeList)
            {
                Types[type.Name] = type;
            }
        }

        public static void PlaceType(VoxelType type, Voxel voxel)
        {
            voxel.Type = type;
            voxel.Water = new WaterCell();
            voxel.Health = voxel.Type.StartingHealth;
        }


        public static bool IsSolid(Voxel v)
        {
            return (!v.IsEmpty);
        }

        public static void RegisterType(VoxelType type, BoxPrimitive primitive)
        {
            PrimitiveMap[type] = primitive;
        }

        public static VoxelType GetVoxelType(short id)
        {
            // 0 is the "null" type
            return VoxelType.TypeList[id];
        }

        public static VoxelType GetVoxelType(string name)
        {
            return Types[name];
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

        public static List<VoxelType> GetTypes()
        {
            return PrimitiveMap.Keys.ToList();
        }
    }

}