using LibNoise;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Math = System.Math;

namespace DwarfCorp
{
    public class OverworldMap
    {
        public static LibNoise.Perlin heightNoise = new LibNoise.Perlin()
        {
            Frequency = 0.7f,
            Lacunarity = 0.8f,
            NoiseQuality = NoiseQuality.Standard,
            OctaveCount = 4,
            Seed = MathFunctions.Random.Next(),
            Persistence = 0.2f
        };

        public OverworldCell[,] Map;
        private static MemoryTexture BiomeBlend = null;

        public OverworldMap()
        {
            BiomeBlend = TextureTool.MemoryTextureFromTexture2D(AssetManager.GetContentTexture("World\\biome-blend"));
            if (BiomeBlend == null || BiomeBlend.Width != VoxelConstants.ChunkSizeX || BiomeBlend.Height != VoxelConstants.ChunkSizeZ)
                BiomeBlend = new MemoryTexture(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeZ);
        }

        public OverworldMap(int Width, int Height) : this()
        {
            Map = new OverworldCell[Width, Height];
        }

        public OverworldMap(OverworldCell[,] Map) : this()
        {
            this.Map = Map;
        }

        public float LinearInterpolate(Vector2 position, OverworldField fieldType)
        {
            float x = position.X;
            float y = position.Y;
            float x1 = (int) MathFunctions.Clamp((float) Math.Ceiling(x), 0, Map.GetLength(0) - 2);
            float y1 = (int) MathFunctions.Clamp((float) Math.Ceiling(y), 0, Map.GetLength(1) - 2);
            float x2 = (int) MathFunctions.Clamp((float) Math.Floor(x), 0, Map.GetLength(0) - 2);
            float y2 = (int) MathFunctions.Clamp((float) Math.Floor(y), 0, Map.GetLength(1) - 2);

            if(Math.Abs(x1 - x2) < 0.5f)
                x1 = x1 + 1;
            
            if(Math.Abs(y1 - y2) < 0.5f)
                y1 = y1 + 1;
         
            float q11 = Map[(int) x1, (int) y1].GetValue(fieldType);
            float q12 = Map[(int) x1, (int) y2].GetValue(fieldType);
            float q21 = Map[(int) x2, (int) y1].GetValue(fieldType);
            float q22 = Map[(int) x2, (int) y2].GetValue(fieldType);

            return MathFunctions.LinearCombination(x, y, x1, y1, x2, y2, q11, q12, q21, q22);
        }

        public static float[,] GenerateHeightMapLookup(int width, int height)
        {
            float[,] toReturn = new float[width, height];

            const float mountainWidth = 0.04f;
            const float continentSize = 0.03f;
            const float hillSize = 0.1f;
            const float smallNoiseSize = 0.15f;
            const float cliffHeight = 0.1f;
            const float invCliffHeight = 1.0f/cliffHeight;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float mountain = (float)Math.Pow(OverworldImageOperations.noise(heightNoise, x, y, 0, mountainWidth), 1);
                    float continent = OverworldImageOperations.noise(heightNoise, x, y, 10, continentSize);
                    float hill = OverworldImageOperations.noise(heightNoise, x, y, 20, hillSize) * 0.02f;
                    float smallnoise = OverworldImageOperations.noise(heightNoise, x, y, 100, smallNoiseSize) * 0.01f;
                    float cliffs = OverworldImageOperations.noise(heightNoise, x, y, 200, continentSize) + 0.5f;
                    float h = OverworldImageOperations.pow(OverworldImageOperations.clamp((continent * mountain) + hill, 0, 1), 1);
                    h += smallnoise;
                    h += 0.4f;
                    h = ((int)(h * invCliffHeight)) * cliffHeight;
                    toReturn[x, y] = h;
                }
            }

            return toReturn;
        }

        public void CreateHeightFromLookupWithErosion(float[,] lookup)
        {
            for (int x = 0; x < Map.GetLength(0); x++)
                for (int y = 0; y < Map.GetLength(1); y++)
                {
                    float h = lookup[x, y];
                    Vector2 vec = new Vector2(x, y);
                    h *= LinearInterpolate(vec, OverworldField.Faults);
                    h += LinearInterpolate(vec, OverworldField.Weathering);
                    h *= LinearInterpolate(vec, OverworldField.Erosion);
                    Map[x, y].Height = OverworldImageOperations.clamp(h, 0, 1);
                }
        }

        public void CreateHeightFromLookup(float[,] lookup)
        {
            for (int x = 0; x < Map.GetLength(0); x++)
                for (int y = 0; y < Map.GetLength(1); y++)
                    Map[x, y].Height = OverworldImageOperations.clamp(lookup[x, y], 0, 1);
        }

        public void GenerateSaveTexture(Color[] worldData)
        {
            for (var x = 0; x < Map.GetLength(0); ++x)
                for (var y = 0; y < Map.GetLength(1); ++y)
                    worldData[(y * Map.GetLength(0)) + x] = new Color(Map[x, y].Height_, (byte)0, (byte)Map[x, y].Biome, (byte)255);
        }

        public static void DecodeSaveTexture(
            OverworldCell[,] map,
            int width,
            int height,
            Color[] worldData)
        {
            for (var x = 0; x < width; ++x)
                for (var y = 0; y < height; ++y)
                {
                    var color = worldData[(y * width) + x];
                    map[x, y].Height_ = color.R;
                    map[x, y].Biome = color.B;

                    if (Library.GetBiome(color.B).HasValue(out var biome))
                    {
                        map[x, y].Rainfall_ = (byte)(biome.Rain * 255);
                        map[x, y].Temperature = (float)(biome.Temp);
                    }
                }
        }

        public void CreateTexture(
            List<OverworldFaction> NativeFactions,
            int scale,
            Color[] worldData,
            float sealevel)
        {
            for (int x = 0; x < Map.GetLength(0); ++x)
            {
                for (int y = 0; y < Map.GetLength(1); ++y)
                {
                    var h1 = Map[x, y].GetValue(OverworldField.Height);
                    var cellColor = Color.DarkBlue;

                    if (h1 < 0.1f)
                        cellColor = new Color(30, 30, 150);
                    else if (h1 >= 0.1f && h1 <= sealevel)
                        cellColor = new Color(50, 50, 255);
                    else if (Library.GetBiome(Map[x, y].Biome).HasValue(out var _biome))
                            cellColor = _biome.MapColor;

                    for (var tx = 0; tx < scale; ++tx)
                        for (var ty = 0; ty < scale; ++ty)
                        {
                            var rx = (x * scale) + tx;
                            var ry = (y * scale) + ty;
                            var pixelIndex = ry * Map.GetLength(0) * scale + rx;
                            if (pixelIndex >= 0 && pixelIndex < worldData.Length)
                                worldData[pixelIndex] = cellColor;
                        }
                }
            }
        }
        
        public void ShadeHeight(int scale, Color[] worldData)
        {
            for (int x = 0; x < Map.GetLength(0); ++x)
            {
                for (int y = 0; y < Map.GetLength(1); ++y)
                {
                    var h1 = Map[x, y].GetValue(OverworldField.Height);

                    for (var tx = 0; tx < scale; ++tx)
                        for (var ty = 0; ty < scale; ++ty)
                        {
                            var h2 = LinearInterpolate(new Vector2((x * scale) + tx, (y * scale) + ty) / scale, OverworldField.Height);
                            var rx = (x * scale) + tx;
                            var ry = (y * scale) + ty;
                            var pixelIndex = ry * Map.GetLength(0) * scale + rx;
                            var cellColor = worldData[pixelIndex];
                            cellColor = new Color((float)(cellColor.R) * (h2 + 0.5f) / 255.0f, (float)(cellColor.G * (h2 + 0.5f)) / 255.0f, (float)(cellColor.B * (h2 + 0.5f)) / 255.0f, 1.0f);
                            worldData[pixelIndex] = cellColor;
                        }
                }
            }
        }

        private static Color GetPixelAt(Color[] data, int x, int y, int width, int height)
        {
            return data[MathFunctions.Clamp(y, 0, height - 1) * width + MathFunctions.Clamp(x, 0, width - 1)];
        }

        private static void SetPixelAt(Color[] data, int x, int y, int width, Color color)
        {
            data[y * width + x] = color;
        }

        public static void Smooth(
            int scale,
            int width,
            int height,
            Color[] worldData)
        {
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    if (x > 0 && y > 0)
                    {
                        var a = GetPixelAt(worldData, (x * scale) - 1, y * scale, width * scale, height * scale);
                        var b = GetPixelAt(worldData, x * scale, (y * scale) - 1, width * scale, height * scale);

                        if (a == b)
                            SetPixelAt(worldData, x * scale, y * scale, width * scale, a);
                    }

                    if (x > 0 && y < height)
                    {
                        var a = GetPixelAt(worldData, (x * scale) - 1, y * scale + scale - 1, width * scale, height * scale);
                        var b = GetPixelAt(worldData, x * scale, (y * scale) + scale, width * scale, height * scale);

                        if (a == b)
                            SetPixelAt(worldData, x * scale, y * scale + scale - 1, width * scale, a);
                    }

                    if (x < width && y < height)
                    {
                        var a = GetPixelAt(worldData, (x * scale) + scale, y * scale + scale - 1, width * scale, height * scale);
                        var b = GetPixelAt(worldData, x * scale + scale - 1, (y * scale) + scale, width * scale, height * scale);

                        if (a == b)
                            SetPixelAt(worldData, x * scale + scale - 1, y * scale + scale - 1, width * scale, a);
                    }

                    if (x < width && y > 0)
                    {
                        var a = GetPixelAt(worldData, (x * scale) + scale, y * scale, width * scale, height * scale);
                        var b = GetPixelAt(worldData, x * scale + scale - 1, (y * scale) - 1, width * scale, height * scale);

                        if (a == b)
                            SetPixelAt(worldData, x * scale + scale - 1, y * scale, width * scale, a);
                    }
                }
            }

        }

        public static Vector2 WorldToOverworld(Vector2 worldXZ)
        {
            return worldXZ / VoxelConstants.OverworldScale;
        }

        public static Vector2 WorldToOverworldRemainder(Vector2 World)
        {
            var x = (float)VoxelConstants.OverworldScale * Math.Floor(World.X / (float)VoxelConstants.OverworldScale);
            var y = (float)VoxelConstants.OverworldScale * Math.Floor(World.Y / (float)VoxelConstants.OverworldScale);
            return new Vector2((float)(World.X - x), (float)(World.Y - y));
        }

        public static Vector2 WorldToOverworld(Vector3 worldXYZ)
        {
            return WorldToOverworld(new Vector2(worldXYZ.X, worldXYZ.Z));
        }

        public IEnumerable<BiomeData> EnumeratePresentBiomes()
        {
            var presentBiomes = new List<byte>();

            for (var x = 0; x < Map.GetLength(0); ++x)
                for (var z = 0; z < Map.GetLength(1); ++z)
                    if (!presentBiomes.Contains(Map[x, z].Biome))
                        presentBiomes.Add(Map[x, z].Biome);

            foreach (var biome in presentBiomes)
                if (Library.GetBiome(biome).HasValue(out var biomeData))
                    yield return biomeData;
        }

        public MaybeNull<BiomeData> GetBiomeAt(Vector3 worldPos)
        {
            var v = WorldToOverworld(worldPos);
            var r = WorldToOverworldRemainder(new Vector2(worldPos.X, worldPos.Z)); // Todo: This needs to be fixed - biome blend aint gonna scale right anymore.
            var blendColor = BiomeBlend.Data[BiomeBlend.Index((int)MathFunctions.Clamp(r.X, 0, VoxelConstants.ChunkSizeX), (int)MathFunctions.Clamp(r.Y, 0, VoxelConstants.ChunkSizeZ))];
            var offsetV = v + new Vector2((blendColor.R - 127.0f) / 128.0f, (blendColor.G - 127.0f) / 128.0f);
            var biome1 = Map[(int)MathFunctions.Clamp(v.X, 0, Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Map.GetLength(1) - 1)].Biome;
            var biome2 = Map[(int)MathFunctions.Clamp(offsetV.X, 0, Map.GetLength(0) - 1), (int)MathFunctions.Clamp(offsetV.Y, 0, Map.GetLength(1) - 1)].Biome;
            return Library.GetBiome(Math.Max(biome1, biome2));
        }

        public float GetValueAt(Vector3 worldPos, OverworldField fieldType)
        {
            Vector2 v = WorldToOverworld(worldPos);
            return OverworldImageOperations.GetValue(Map, v, fieldType);
        }

        public float Height(int x, int y)
        {
            return Map[x, y].Height;
        }
    }
}
