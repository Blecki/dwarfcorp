using Microsoft.Xna.Framework;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    {
        public static float GetAverageHeight(int X, int Z, int Width, int Height, ChunkGeneratorSettings Settings)
        {
            var avgHeight = 0;
            var numHeight = 0;

            for (var dx = 0; dx < Width; dx++)
            {
                for (var dz = 0; dz < Height; dz++)
                {
                    var worldPos = new Vector3(X + dx, (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1, Z + dz);
                    var baseVoxel = VoxelHelpers.FindFirstVoxelBelowIncludingWater(Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));

                    if (!baseVoxel.IsValid) continue;

                    avgHeight += baseVoxel.Coordinate.Y + 1;
                    numHeight += 1;
                }
            }

            if (numHeight == 0) return 0;
            return (float)avgHeight / (float)numHeight;
        }
    }
}
