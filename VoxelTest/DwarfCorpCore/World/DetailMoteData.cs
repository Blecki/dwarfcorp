namespace DwarfCorp
{
    /// <summary>
    /// A detail mote is a little bit of vegetation or whatever else
    /// which is drawn over certain voxels.
    /// </summary>
    public class DetailMoteData
    {
        public string Name { get; set; }
        public float RegionScale { get; set; }
        public float SpawnThreshold { get; set; }
        public float MoteScale { get; set; }
        public string Asset { get; set; }

        public DetailMoteData(string name, string asset, float regionScale, float spawnThresh, float moteScale)
        {
            Name = name;
            RegionScale = regionScale;
            SpawnThreshold = spawnThresh;
            MoteScale = moteScale;
            Asset = asset;
        }
    }

}