namespace DwarfCorp
{
    /// <summary>
    /// Vegetation data describes how certain plants (such as trees) are to populate
    /// a chunk.
    /// </summary>
    public class VegetationData
    {
        public string Name { get; set; }
        public float SpawnProbability { get; set; }
        public float MeanSize { get; set; }
        public float SizeVariance { get; set; }
        public float VerticalOffset { get; set; }

        public VegetationData(string name, float spawnProbability, float meansize, float sizevar, float verticalOffset)
        {
            Name = name;
            SpawnProbability = spawnProbability;
            MeanSize = meansize;
            SizeVariance = sizevar;
            VerticalOffset = verticalOffset;
        }
    }

}