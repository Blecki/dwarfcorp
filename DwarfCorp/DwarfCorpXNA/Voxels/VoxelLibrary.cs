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
//using System.Windows.Forms;
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

        private static VoxelType.FringeTileUV[] CreateFringeUVs(Point[] Tiles)
        {
            System.Diagnostics.Debug.Assert(Tiles.Length == 3);

            var r = new VoxelType.FringeTileUV[8];

            // North
            r[0] = new VoxelType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2) + 1, 16, 32);
            // East
            r[1] = new VoxelType.FringeTileUV((Tiles[1].X * 2) + 1, Tiles[1].Y, 32, 16);
            // South
            r[2] = new VoxelType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2), 16, 32);
            // West
            r[3] = new VoxelType.FringeTileUV(Tiles[1].X * 2, Tiles[1].Y, 32, 16);

            // NW
            r[4] = new VoxelType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2) + 1, 32, 32);
            // NE
            r[5] = new VoxelType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2) + 1, 32, 32);
            // SE
            r[6] = new VoxelType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2), 32, 32);
            // SW
            r[7] = new VoxelType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2), 32, 32);

            return r;
        }

        public static void CreateTransitionUVs(GraphicsDevice graphics, Texture2D textureMap, int width, int height, Point top, Point sides, Point bottom, Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords> transitionTextures, VoxelType.TransitionType transitionType = VoxelType.TransitionType.Horizontal)
        {
            for(int i = 0; i < 16; i++)
            {
                Point topPoint = new Point(top.X + i, top.Y);

                if (transitionType == VoxelType.TransitionType.Horizontal)
                {
                    BoxTransition transition = new BoxTransition()
                    {
                        Top = (TransitionTexture) i
                    };
                    transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                        textureMap.Height, width, height, sides, sides, topPoint, bottom, sides, sides);
                }
                else
                {
                    for (int j = 0; j < 16; j++)
                    { 
                         Point sidePoint = new Point(top.X + j, top.Y);
                        // TODO: create every iteration of frontback vs. left right. There should be 16 of these.
                        BoxTransition transition = new BoxTransition()
                        {
                            Left = (TransitionTexture)i,
                            Right = (TransitionTexture)i,
                            Front = (TransitionTexture)j,
                            Back = (TransitionTexture)j
                        };
                        transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                            textureMap.Height, width, height, sidePoint, sidePoint, sides, bottom, topPoint, topPoint);
                    }
                }
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
            BoxPrimitive darkDirtCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(8, 1), new Point(8, 1), new Point(8, 1));
            BoxPrimitive stoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 1), new Point(1, 0), new Point(1, 0));
            BoxPrimitive sandCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 1), new Point(1, 1), new Point(1, 1));
            BoxPrimitive coalCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 11), new Point(0, 12), new Point(0, 12));
            BoxPrimitive ironCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(1, 11), new Point(1, 12), new Point(1, 12));
            BoxPrimitive goldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 11), new Point(2, 12), new Point(2, 12));
            BoxPrimitive manaCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 11), new Point(3, 12), new Point(3, 12));
            BoxPrimitive frostCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 1), new Point(2, 1), new Point(2, 0));
            BoxPrimitive snowCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(3, 7), new Point(3, 7), new Point(3, 7));
            BoxPrimitive iceCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(2, 7), new Point(2, 7), new Point(2, 7));
            BoxPrimitive scaffoldCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 0), new Point(7, 0), new Point(7, 0));
            BoxPrimitive glassCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(10, 0), new Point(10, 0), new Point(10, 0));
            BoxPrimitive brickCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(11, 0), new Point(11, 0), new Point(11, 0));
            BoxPrimitive plankCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 0), new Point(4, 0), new Point(4, 0));
            BoxPrimitive waterCube = CreatePrimitive(graphics, cubeTexture, cubeTexture.Width, cubeTexture.Height, new Point(0, 0), new Point(0, 0), new Point(0, 0));
            BoxPrimitive cobblestoneCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 1), new Point(9, 0), new Point(4, 1));
            BoxPrimitive magicCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(0, 10), new Point(0, 10), new Point(0, 10));
            BoxPrimitive bedrockCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 1), new Point(6, 1), new Point(6, 1));
            BoxPrimitive brownTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 0), new Point(5, 0), new Point(5, 0));
            BoxPrimitive blueTileCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 0), new Point(6, 0), new Point(6, 0));
            BoxPrimitive tilledSoilCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 1), new Point(2, 0), new Point(2, 0));

            BoxPrimitive redGemCube =    CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(4, 11), new Point(4, 12), new Point(4, 12));
            BoxPrimitive orangeGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(5, 11), new Point(5, 12), new Point(5, 12));
            BoxPrimitive yellowGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(6, 11), new Point(6, 12), new Point(6, 12));
            BoxPrimitive greenGemCube =  CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(7, 11), new Point(7, 12), new Point(7, 12));
            BoxPrimitive blueGemCube =   CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(8, 11), new Point(8, 12), new Point(8, 12));
            BoxPrimitive purpleGemCube = CreatePrimitive(graphics, cubeTexture, 32, 32, new Point(9, 11), new Point(9, 12), new Point(9, 12));
            
            emptyType = new VoxelType
            {
                Name = "empty",
                ReleasesResource = false,
                IsBuildable = false,
                ID = 0
            };

            SoundSource dirtPicks = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_dirt_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_dirt_2, ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_dirt_3);

            SoundSource stonePicks = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_stone_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_stone_2, ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_stone_3);

            SoundSource woodPicks = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_wood_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_wood_2, ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_wood_3);

            VoxelType tilledSoil = new VoxelType
            {
                Name = "TilledSoil",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                IsSoil = true,
                IsSurface = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy),
                HitSound = dirtPicks
            };
            RegisterType(tilledSoil, tilledSoilCube);

            VoxelType brownTile = new VoxelType
            {
                Name = "Brown Tile",
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                IsBuildable = true,
                StartingHealth = 20,
                CanRamp = false,
                ParticleType = "stone_particle",
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy),
                HitSound = stonePicks
            };
            RegisterType(brownTile, brownTileCube);

            VoxelType blueTileFloor = new VoxelType
            {
                Name = "Blue Tile",
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                IsBuildable = true,
                StartingHealth = 20,
                CanRamp = false,
                ParticleType = "stone_particle",
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy),
                HitSound = stonePicks
            };
            RegisterType(blueTileFloor, blueTileCube);

            VoxelType cobblestoneFloor = new VoxelType
            {
                Name = "Cobble",
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                IsBuildable = true,
                StartingHealth = 20,
                CanRamp = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy),
                HitSound = stonePicks
            };
            RegisterType(cobblestoneFloor, cobblestoneCube);
            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 8), new Point(9, 0), new Point(4, 1), cobblestoneFloor.TransitionTextures);
            
            VoxelType stockpileType = new VoxelType
            {
                Name = "Stockpile",
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_wood_destroy),
                HitSound = woodPicks
            };
            RegisterType(stockpileType, plankCube);

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 9), new Point(9, 0), new Point(4, 0), stockpileType.TransitionTextures);
            
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
                HasTransitionTextures = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_wood_destroy),
                HitSound = woodPicks
            };


            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 9), new Point(9, 0), new Point(4, 0), plankType.TransitionTextures);

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
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.wurp),
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
                ParticleType = "stone_particle",
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_wood_destroy),
                HitSound = woodPicks
            };

            VoxelType grassType = new VoxelType
            {
                Name = "Grass",
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                StartingHealth = 10,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                HasTransitionTextures = false,
                IsSoil = true,
                IsSurface = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy),
                HitSound = dirtPicks,
                UseBiomeGrassTint = true,
                HasFringeTransitions = true,
                FringeTiles = new Point[]
                {
                    new Point(0,2),
                    new Point(1,2),
                    new Point(2,2)
                },
                FringeTransitionUVs = CreateFringeUVs(new Point[]
                {
                    new Point(0,2),
                    new Point(1,2),
                    new Point(2,2),
                })
            };
                       
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
                IsSoil = false,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_snow_destroy),
                HitSound = dirtPicks
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
                IsSoil = false,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_snow_destroy),
                HitSound = dirtPicks
            };
            RegisterType(iceType, iceCube);
            
            VoxelType caveFungus = new VoxelType
            {
                Name = "CaveFungus",
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                StartingHealth = 30,
                ReleasesResource = true,
                CanRamp = false,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                IsSurface = false,
                IsSoil = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy),
                HitSound = dirtPicks
            };

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 13), new Point(1, 0), new Point(1, 0), caveFungus.TransitionTextures);

            VoxelType dirtType = new VoxelType
            {
                Name = "Dirt",
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                ProbabilityOfRelease = 1.0f,
                StartingHealth = 10,
                RampSize = 0.5f,
                CanRamp = true,
                IsBuildable = true,
                ParticleType = "dirt_particle",
                IsSoil = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy),
                HitSound = dirtPicks
            };

            VoxelType stoneType = new VoxelType
            {
                Name = "Stone",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                StartingHealth = 40,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy),
                HitSound = stonePicks
            };

            VoxelType bedrockType = new VoxelType
            {
                Name = "Bedrock",
                StartingHealth = 255,
                IsBuildable = false,
                IsInvincible = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy),
                HitSound = stonePicks
            };

            VoxelType waterType = new VoxelType
            {
                Name = "water",
                ReleasesResource = false,
                CanRamp = true,
                RampSize = 0.5f,
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
                IsBuildable = true,
                ParticleType = "sand_particle",
                IsSurface = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Sand,
                ProbabilityOfRelease = 1.0f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_sand_destroy),
                HitSound = dirtPicks
            };

            VoxelType ironType = new VoxelType
            {
                Name = "Iron",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Iron,
                StartingHealth = 80,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 5,
                SpawnVeins = true,
                VeinLength = 10,
                Rarity = 0.0f,
                MinSpawnHeight = 8,
                MaxSpawnHeight = 40,
                SpawnProbability = 1.0f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

           
            VoxelType coalType = new VoxelType
            {
                Name = "Coal",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Coal,
                StartingHealth = 75,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 5,
                MinSpawnHeight = 15,
                MaxSpawnHeight = 50,
                SpawnProbability = 0.3f,
                Rarity = 0.05f,
                SpawnOnSurface = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };


            VoxelType goldType = new VoxelType
            {
                Name = "Gold",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Gold,
                StartingHealth = 90,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnVeins = true,
                VeinLength = 20,
                Rarity = 0.2f,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 20,
                SpawnProbability = 0.99f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

            VoxelType greenGem = new VoxelType
            {
                Name = "Emerald",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Emerald",
                StartingHealth = 90,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

            VoxelType redGem = new VoxelType
            {
                Name = "Ruby",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Ruby",
                StartingHealth = 90,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };


            VoxelType purpleGem = new VoxelType
            {
                Name = "Amethyst",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Amethyst",
                StartingHealth = 90,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ClusterSize = 3,
                SpawnClusters = true,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

            VoxelType blueGem = new VoxelType
            {
                Name = "Sapphire",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Sapphire",
                StartingHealth = 90,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

            VoxelType yellowGem = new VoxelType
            {
                Name = "Citrine",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Citrine",
                StartingHealth = 90,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

            VoxelType orangeGem = new VoxelType
            {
                Name = "Garnet",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = "Garnet",
                StartingHealth = 90,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnClusters = true,
                ClusterSize = 3,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 18,
                SpawnProbability = 0.8f,
                Rarity = 0.9f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

         
            VoxelType manaType = new VoxelType
            {
                Name = "Mana",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Mana,
                StartingHealth = 200,
                IsBuildable = true,
                ParticleType = "stone_particle",
                SpawnVeins = true,
                VeinLength = 25,
                Rarity = 0.1f,
                MinSpawnHeight = 0,
                MaxSpawnHeight = 14,
                SpawnProbability = 0.99f,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks
            };

            VoxelType glassType = new VoxelType
            {
                Name = "Glass",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Glass,
                StartingHealth = 1,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy),
                HitSound = stonePicks,
                IsTransparent =  true,
                HasTransitionTextures = true,
                Transitions = VoxelType.TransitionType.Vertical
            };

            CreateTransitionUVs(graphics, cubeTexture, 32, 32, new Point(0, 14), new Point(0, 14), new Point(0, 14), glassType.TransitionTextures,
                VoxelType.TransitionType.Vertical);


            VoxelType brickType = new VoxelType
            {
                Name = "Brick",
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Brick,
                StartingHealth = 30,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy),
                HitSound = stonePicks
            };

            VoxelType darkDirtType = new VoxelType
            {
                Name = "DarkDirt",
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                ProbabilityOfRelease = 1.0f,
                StartingHealth = 10,
                RampSize = 0.5f,
                CanRamp = true,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                IsSoil = true,
                ExplosionSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy),
                HitSound = dirtPicks
            };

            RegisterType(greenGem, greenGemCube);
            RegisterType(redGem, redGemCube);
            RegisterType(purpleGem, purpleGemCube);
            RegisterType(blueGem, blueGemCube);
            RegisterType(orangeGem, orangeGemCube);
            RegisterType(yellowGem, yellowGemCube);

            RegisterType(grassType, grassCube);
            RegisterType(caveFungus, grassCube);
            RegisterType(emptyType, null);
            RegisterType(dirtType, dirtCube);
            RegisterType(darkDirtType, darkDirtCube);
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
            RegisterType(glassType, glassCube);
            RegisterType(brickType, brickCube);
            
            foreach (VoxelType type in VoxelType.TypeList)
            {
                Types[type.Name] = type;
            }
        }

        public static void PlaceType(VoxelType type, VoxelHandle voxel)
        {
            voxel.Type = type;
            voxel.WaterCell = new WaterCell();
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

        public static Texture2D RenderIcons(GraphicsDevice device, Shader shader, ChunkManager chunks, int width, int height, int tileSize)
        {
            RenderTarget2D toReturn = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth16, 16, RenderTargetUsage.PreserveContents);
        
            device.SetRenderTarget(toReturn);
            device.Clear(Color.Transparent);
            shader.SetTexturedTechnique();
            shader.MainTexture = chunks.ChunkData.Tilemap;
            shader.SelfIlluminationEnabled = true;
            shader.SelfIlluminationTexture = chunks.ChunkData.IllumMap;
            shader.EnableShadows = false;
            shader.EnableLighting = false;
            shader.ClippingEnabled = false;
            shader.CameraPosition = new Vector3(-0.5f, 0.5f, 0.5f);
            shader.VertexColorTint = Color.White;
            shader.LightRampTint = Color.White;
            shader.SunlightGradient = chunks.ChunkData.SunMap;
            shader.AmbientOcclusionGradient = chunks.ChunkData.AmbientMap;
            shader.TorchlightGradient = chunks.ChunkData.TorchMap;
            Viewport oldview = device.Viewport;
            List<VoxelType> voxelsByType = Types.Select(type => type.Value).ToList();
            voxelsByType.Sort((a, b) => a.ID < b.ID ? -1 : 1);
            int rows = width/tileSize;
            int cols = height/tileSize;
            device.ScissorRectangle = new Rectangle(0, 0, tileSize, tileSize);
            device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.Default;
            Vector3 half = Vector3.One*0.5f;
            half = new Vector3(half.X, half.Y + 0.3f, half.Z);
            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                foreach (var type in voxelsByType)
                {
                    int row = type.ID/cols;
                    int col = type.ID%cols;
                    BoxPrimitive primitive = GetPrimitive(type);
                    if (primitive == null)
                        continue;

                    if (type.HasTransitionTextures)
                        primitive = new BoxPrimitive(device, 1, 1, 1, type.TransitionTextures[new BoxTransition()]);

                    device.Viewport = new Viewport(col * tileSize, row * tileSize, tileSize, tileSize);
                    Matrix viewMatrix = Matrix.CreateLookAt(new Vector3(-1.5f, 1.3f, -1.5f), Vector3.Zero, Vector3.Up);
                    Matrix projectionMatrix = Matrix.CreateOrthographic(1.75f, 1.75f, 0, 5);
                    shader.View = viewMatrix;
                    shader.Projection = projectionMatrix;
                    shader.World = Matrix.CreateTranslation(-half);
                    pass.Apply();
                    primitive.Render(device);
                }
            }
            device.Viewport = oldview;
            return (Texture2D) toReturn;
        }
    }

}