using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using LibNoise;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Math = System.Math;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    {
        private static List<MemoryTexture> RuinTemplates;
        private static bool RuinTemplatesLoaded = false; // Todo: Move this over into the Library

        private static void LoadRuinTemplates()
        {
            if (RuinTemplatesLoaded)
                return;

            RuinTemplatesLoaded = true;
            RuinTemplates = new List<MemoryTexture>();

            var tex = TextureTool.MemoryTextureFromTexture2D(AssetManager.GetContentTexture("World/ruins"));

            for (var x = 0; x < tex.Width; x += 16) // If our texture isn't the right size this will fuck up.
                for (var y = 0; y < tex.Height; y += 16)
                {
                    var template = new MemoryTexture(16, 16);
                    TextureTool.Blit(tex, new Rectangle(x, y, 16, 16), template, new Point(0, 0));
                    RuinTemplates.Add(template);
                }
        }

        public static void GenerateRuin(VoxelChunk Chunk, ChunkGeneratorSettings Settings)
        {
            // Todo: Support ruins deep underground - empty out their interiors.

            LoadRuinTemplates();

            var noiseVector = Chunk.Origin.ToVector3() * Settings.CaveNoiseScale;
            var ruinsNoise = Settings.CaveNoise.GetValue(noiseVector.X, noiseVector.Y, noiseVector.Z);
            if (Math.Abs(ruinsNoise) > GameSettings.Current.GenerationRuinsRate) return;


            var avgHeight = GetAverageHeight(Chunk.Origin.X, Chunk.Origin.Z, 16, 16, Settings);

            var ruinWallType = Library.GetVoxelType("Cobble"); // Todo: Should make this data so this doesn't break if tile names change?
            var ruinFloorType = Library.GetVoxelType("Blue Tile");
            if (Settings.Overworld.Map.GetBiomeAt(Chunk.Origin.ToVector3(), Settings.Overworld.InstanceSettings.Origin).HasValue(out var biome))
            {
                ruinWallType = Library.GetVoxelType(biome.RuinWallType);
                ruinFloorType = Library.GetVoxelType(biome.RuinFloorType);
            }

            if (!ruinWallType.HasValue() || !ruinFloorType.HasValue())
                return;

            int wallHeight = MathFunctions.RandInt(2, 6);
            var template = RuinTemplates[MathFunctions.RandInt(0, RuinTemplates.Count)];

            var rotations = MathFunctions.RandInt(0, 3);
            for (var i = 0; i < rotations; ++i)
                template = TextureTool.RotatedCopy(template);

            for (int dx = 0; dx < 16; dx++)
                for (int dz = 0; dz < 16; dz++)
                {
                    var worldPos = new Vector3(Chunk.Origin.X + dx, avgHeight, Chunk.Origin.Z + dz);

                    var baseVoxel = Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos));
                    var underVoxel = VoxelHelpers.FindFirstVoxelBelow(Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));
                    var decay = Settings.NoiseGenerator.Generate(worldPos.X * 0.05f, worldPos.Y * 0.05f, worldPos.Z * 0.05f);

                    if (decay > 0.7f) continue;
                    if (!baseVoxel.IsValid) continue;
                    if (baseVoxel.Coordinate.Y == (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1) continue;
                    if (!underVoxel.IsValid) continue;


                    var templateColor = template.Data[template.Index(dx, dz)];

                    if (templateColor == new Color(0, 0, 255, 255)) // Border
                        continue;
                    else if (templateColor == new Color(0, 0, 0, 255)) // Space
                        continue;
                    else if (templateColor == new Color(255, 0, 0, 255)) // Wall
                    {
                        baseVoxel.RawSetType(ruinWallType);
                        FillBelowRuins(Settings, ruinWallType, baseVoxel, underVoxel);
                        FillRuinColumn(Settings, ruinWallType, 1, wallHeight, baseVoxel, decay);
                    }
                    else if (templateColor == new Color(128, 128, 0, 255)) // Door
                    {
                        baseVoxel.RawSetType(ruinWallType);
                        FillBelowRuins(Settings, ruinWallType, baseVoxel, underVoxel);
                        FillRuinColumn(Settings, ruinWallType, 3, wallHeight, baseVoxel, decay);
                    }
                    else if (templateColor == new Color(128, 0, 0, 255)) // Floor
                    {
                        FillBelowRuins(Settings, ruinWallType, baseVoxel, underVoxel);
                        baseVoxel.RawSetType(ruinFloorType);
                    }
                }
        }

        private static void FillBelowRuins(ChunkGeneratorSettings Settings, MaybeNull<VoxelType> ruinWallType, VoxelHandle baseVoxel, VoxelHandle underVoxel)
        {
            for (int dy = 1; dy < (baseVoxel.Coordinate.Y - underVoxel.Coordinate.Y); dy++)
            {
                var currVoxel = Settings.World.ChunkManager.CreateVoxelHandle(underVoxel.Coordinate + new GlobalVoxelOffset(0, dy, 0));

                if (!currVoxel.IsValid)
                    continue;

                currVoxel.RawSetType(ruinWallType);
            }

            underVoxel.RawSetGrass(0);
        }

        private static void FillRuinColumn(ChunkGeneratorSettings Settings, MaybeNull<VoxelType> WallType, int gap, int wallHeight, VoxelHandle baseVoxel, float decay)
        {
            for (int dy = gap; dy < wallHeight * (1.0f - decay) && dy < (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 2; dy++)
            {
                var currVoxel = Settings.World.ChunkManager.CreateVoxelHandle(baseVoxel.Coordinate + new GlobalVoxelOffset(0, dy, 0));

                if (currVoxel.IsValid)
                    currVoxel.RawSetType(WallType);
            }
        }
    }
}
