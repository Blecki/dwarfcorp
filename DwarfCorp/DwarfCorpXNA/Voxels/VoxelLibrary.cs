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
        public static List<VoxelType> TypeList;

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

        public static Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords> CreateTransitionUVs(GraphicsDevice graphics, Texture2D textureMap, int width, int height, Point[] tiles,  VoxelType.TransitionType transitionType = VoxelType.TransitionType.Horizontal)
        {
            var transitionTextures = new Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords>();

            for(int i = 0; i < 16; i++)
            {
                Point topPoint = new Point(tiles[0].X + i, tiles[0].Y);

                if (transitionType == VoxelType.TransitionType.Horizontal)
                {
                    BoxTransition transition = new BoxTransition()
                    {
                        Top = (TransitionTexture) i
                    };
                    transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                        textureMap.Height, width, height, tiles[2], tiles[2], topPoint, tiles[1], tiles[2], tiles[2]);
                }
                else
                {
                    for (int j = 0; j < 16; j++)
                    { 
                         Point sidePoint = new Point(tiles[0].X + j, tiles[0].Y);
                        // TODO: create every iteration of frontback vs. left right. There should be 16 of these.
                        BoxTransition transition = new BoxTransition()
                        {
                            Left = (TransitionTexture)i,
                            Right = (TransitionTexture)i,
                            Front = (TransitionTexture)j,
                            Back = (TransitionTexture)j
                        };
                        transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                            textureMap.Height, width, height, sidePoint, sidePoint, tiles[2], tiles[1], topPoint, topPoint);
                    }
                }
            }

            return transitionTextures;
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
            
            emptyType = new VoxelType
            {
                Name = "empty",
                ReleasesResource = false,
                IsBuildable = false,
                ID = 0
            };

            var dirtPicks = new String[] { ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_dirt_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_dirt_2, ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_dirt_3 };

            var stonePicks = new String[] { ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_stone_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_stone_2, ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_stone_3 };

            var woodPicks = new String[] { ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_wood_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_wood_2, ContentPaths.Audio.Oscar.sfx_ic_dwarf_pick_wood_3 };

            TypeList = new List<VoxelType>
            {
                emptyType,

                new VoxelType
                {
                Name = "TilledSoil",
                Top = new Point(5,1),
                Bottom = new Point(2,0),
                Sides = new Point(2, 0),
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                IsSoil = true,
                IsSurface = true,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy,
                HitSoundResources = dirtPicks
                },

            new VoxelType
            {
                Name = "Brown Tile",
                Top = new Point(5, 0),
                Bottom = new Point(5,0),
                Sides = new Point(5,0),
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                IsBuildable = true,
                StartingHealth = 20,
                CanRamp = false,
                ParticleType = "stone_particle",
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Blue Tile",
                Top = new Point(6,0),
                Bottom = new Point(6,0),
                Sides = new Point(6,0),
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                IsBuildable = true,
                StartingHealth = 20,
                CanRamp = false,
                ParticleType = "stone_particle",
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Cobble",
                Top = new Point(4,1),
                Bottom = new Point(9,0),
                Sides = new Point(4,1),
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                IsBuildable = true,
                StartingHealth = 20,
                CanRamp = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                TransitionTiles = new Point[] { new Point(0, 8), new Point(9,0), new Point(4,1) },
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Stockpile",
                Top = new Point(4,0),
                Bottom = new Point(4,0),
                Sides = new Point(4,0),
                ReleasesResource = false,
                StartingHealth = 20,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                TransitionTiles = new Point[] { new Point(0,9), new Point(9,0), new Point(4,0) },
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_wood_destroy,
                HitSoundResources = woodPicks
            },

                new VoxelType
            {
                Name = "Plank",
                Top = new Point(4, 0),
                Bottom = new Point(4, 0),
                Sides = new Point(4, 0),
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Wood,
                StartingHealth = 20,
                ReleasesResource = true,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = true,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                TransitionTiles = new Point[] { new Point(0, 9), new Point(9, 0), new Point(4, 0) },
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_wood_destroy,
                HitSoundResources = woodPicks
            },

                new VoxelType
            {
                Name = "Magic",
                Top = new Point(0, 10),
                Bottom = new Point(0,10),
                Sides = new Point(0, 10),
                ProbabilityOfRelease = 0.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Mana,
                StartingHealth = 1,
                ReleasesResource = true,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "star_particle",
                ExplosionSoundResource = ContentPaths.Audio.wurp,
                HasTransitionTextures = false,
                TransitionTiles = new Point[] { new Point(0, 10), new Point(15, 10), new Point(15, 10) },
                EmitsLight = true
            },

                new VoxelType
            {
                Name = "Scaffold",
                Top = new Point(7,0),
                Bottom = new Point(7,0),
                Sides = new Point(7,0),
                StartingHealth = 20,
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Wood,
                ReleasesResource = false,
                CanRamp = false,
                RampSize = 0.5f,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_wood_destroy,
                HitSoundResources = woodPicks
            },

                new VoxelType
            {
                Name = "Grass",
                Top = new Point(0, 0),
                Bottom = new Point(2, 0),
                Sides = new Point(2, 0),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy,
                HitSoundResources = dirtPicks,
                UseBiomeGrassTint = true,
                HasFringeTransitions = true,
                FringeTiles = new Point[]
                {
                    new Point(0,2),
                    new Point(1,2),
                    new Point(2,2)
                }
            },
                new VoxelType
            {
                Name = "Snow",
                Top = new Point(3,7),
                Bottom = new Point(3,7),
                Sides = new Point(3,7),
                StartingHealth = 1,
                ReleasesResource = false,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "snow_particle",
                HasTransitionTextures = false,
                IsSurface = true,
                IsSoil = false,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_snow_destroy,
                HitSoundResources = dirtPicks
            },

                new VoxelType
            {
                Name = "Ice",
                Top = new Point(2, 7),
                Bottom = new Point(2, 7),
                Sides = new Point(2, 7),
                StartingHealth = 1,
                ReleasesResource = false,
                CanRamp = false,
                IsBuildable = false,
                ParticleType = "snow_particle",
                HasTransitionTextures = false,
                IsSurface = true,
                IsSoil = false,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_snow_destroy,
                HitSoundResources = dirtPicks
            },

                new VoxelType
            {
                Name = "CaveFungus",
                Top = new Point(0, 0),
                Bottom = new Point(2, 0),
                Sides = new Point(2, 0),
                ProbabilityOfRelease = 1.0f,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                StartingHealth = 30,
                ReleasesResource = true,
                CanRamp = false,
                RampSize = 0.5f,
                IsBuildable = false,
                ParticleType = "stone_particle",
                HasTransitionTextures = true,
                TransitionTiles = new Point[] { new Point(0, 13), new Point(1, 0), new Point(1, 0) },
                IsSurface = false,
                IsSoil = true,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy,
                HitSoundResources = dirtPicks
            },

                new VoxelType
            {
                Name = "Dirt",
                Top = new Point(2,0),
                Bottom = new Point(2,0),
                Sides = new Point(2,0),
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                ProbabilityOfRelease = 1.0f,
                StartingHealth = 10,
                RampSize = 0.5f,
                CanRamp = true,
                IsBuildable = true,
                ParticleType = "dirt_particle",
                IsSoil = true,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy,
                HitSoundResources = dirtPicks
            },

                new VoxelType
            {
                Name = "Stone",
                Top = new Point(3,1),
                Bottom = new Point(1,0),
                Sides = new Point(1,0),
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Stone,
                StartingHealth = 40,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Bedrock",
                Top = new Point(6, 1),
                Bottom = new Point(6, 1),
                Sides = new Point(6, 1),
                StartingHealth = 255,
                IsBuildable = false,
                IsInvincible = true,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "water",
                Top = new Point(0,0),
                Bottom = new Point(0,0),
                Sides = new Point(0,0),
                ReleasesResource = false,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = false,
                StartingHealth = 255
            },

                new VoxelType
            {
                Name = "Sand",
                Top = new Point(1,1),
                Bottom = new Point(1,1),
                Sides = new Point(1,1),
                ReleasesResource = true,
                StartingHealth = 15,
                CanRamp = true,
                RampSize = 0.5f,
                IsBuildable = true,
                ParticleType = "sand_particle",
                IsSurface = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Sand,
                ProbabilityOfRelease = 1.0f,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_sand_destroy,
                HitSoundResources = dirtPicks
            },

                new VoxelType
            {
                Name = "Iron",
                Top = new Point(1,11),
                Bottom = new Point(1,12),
                Sides = new Point(1,12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Coal",
                Top = new Point(0, 11),
                Bottom = new Point(0, 12),
                Sides = new Point(0,12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Gold",
                Top = new Point(2, 11),
                Bottom = new Point(2, 12),
                Sides = new Point(2, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Emerald",
                Top = new Point(7, 11),
                Bottom = new Point(7, 12),
                Sides = new Point(7, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Ruby",
                Top = new Point(4, 11),
                Bottom = new Point(4, 12),
                Sides = new Point(4, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Amethyst",
                Top = new Point(9, 11),
                Bottom = new Point(9, 12),
                Sides = new Point(9, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Sapphire",
                Top = new Point(8, 11),
                Bottom = new Point(8, 12),
                Sides = new Point(8, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Citrine",
                Top = new Point(6, 11),
                Bottom = new Point(6, 12),
                Sides = new Point(6, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Garnet",
                Top = new Point(5, 11),
                Bottom = new Point(5, 12),
                Sides = new Point(5, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Mana",
                Top = new Point(3, 11),
                Bottom = new Point(3, 12),
                Sides = new Point(3, 12),
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
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "Glass",
                Top = new Point(3, 11),
                Bottom = new Point(3, 12),
                Sides = new Point(3, 12),
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Glass,
                StartingHealth = 1,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_metal_destroy,
                HitSoundResources = stonePicks,
                IsTransparent =  true,
                HasTransitionTextures = true,
                TransitionTiles = new Point[] { new Point(0, 14), new Point(0, 14), new Point(0, 14) },
                Transitions = VoxelType.TransitionType.Vertical
            },


                new VoxelType
            {
                Name = "Brick",
                Top = new Point(11, 0),
                Bottom = new Point(11, 0),
                Sides = new Point(11, 0),
                ProbabilityOfRelease = 1.0f,
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Brick,
                StartingHealth = 30,
                IsBuildable = true,
                ParticleType = "stone_particle",
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_stone_destroy,
                HitSoundResources = stonePicks
            },

                new VoxelType
            {
                Name = "DarkDirt",
                Top = new Point(8,1),
                Bottom = new Point(8,1),
                Sides = new Point(8,1),
                ReleasesResource = true,
                ResourceToRelease = ResourceLibrary.ResourceType.Dirt,
                ProbabilityOfRelease = 1.0f,
                StartingHealth = 10,
                RampSize = 0.5f,
                CanRamp = true,
                IsBuildable = false,
                ParticleType = "dirt_particle",
                IsSoil = true,
                ExplosionSoundResource = ContentPaths.Audio.Oscar.sfx_env_voxel_dirt_destroy,
                HitSoundResources = dirtPicks
            }
            };

            FileUtils.SaveJSon(TypeList, ContentPaths.voxel_types, false);

            short ID = 0;
            foreach (VoxelType type in TypeList)
            {
                type.ID = ID;
                ++ID;

                Types[type.Name] = type;
                PrimitiveMap[type] = type.ID == 0 ? null : CreatePrimitive(graphics, cubeTexture, 32, 32, type.Top, type.Bottom, type.Sides);

                if (type.HasTransitionTextures)
                    type.TransitionTextures = CreateTransitionUVs(graphics, cubeTexture, 32, 32, type.TransitionTiles, type.Transitions);

                if (type.HasFringeTransitions)
                    type.FringeTransitionUVs = CreateFringeUVs(type.FringeTiles);

                type.ExplosionSound = SoundSource.Create(type.ExplosionSoundResource);
                type.HitSound = SoundSource.Create(type.HitSoundResources);
            }
        }

        // Todo: Kill
        public static void PlaceType(VoxelType type, VoxelHandle voxel)
        {
            voxel.Type = type;
            voxel.WaterCell = new WaterCell();
        }

        public static VoxelType GetVoxelType(short id)
        {
            return TypeList[id];
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

        // Do not delete: Used to generate block icon texture for menu.
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