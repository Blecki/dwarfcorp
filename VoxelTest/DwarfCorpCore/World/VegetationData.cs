namespace DwarfCorp
{
    /// <summary>
    /// Vegetation data describes how certain plants (such as trees) are to populate
    /// a chunk.
    /// </summary>
    public class VegetationData
    {
        public string Name { get; set; }
        public float ClumpSize { get; set; }
        public float ClumpThreshold { get; set; }
        public float MeanSize { get; set; }
        public float SizeVariance { get; set; }
        public float VerticalOffset { get; set; }
        public float NoiseOffset { get; set; }
        public float SpawnProbability { get; set; }

        public VegetationData(string name, float meansize, float sizevar, float verticalOffset, float clumpSize, float clumpThreshold, float spawnProbability)
        {
            Name = name;
            MeanSize = meansize;
            SizeVariance = sizevar;
            VerticalOffset = verticalOffset;
            ClumpThreshold = clumpThreshold;
            ClumpSize = clumpSize;
            SpawnProbability = spawnProbability;
            NoiseOffset = MathFunctions.Rand(0, 300.0f);
        }
    }

}