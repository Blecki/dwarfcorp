using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
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

    public class DetailMoteData
    {
        public string Name { get; set; }
        public float RegionScale { get; set; }
        public float SpawnThreshold { get; set; }
        public float MoteScale { get; set; }

        public DetailMoteData(string name, float regionScale, float spawnThresh, float moteScale)
        {
            Name = name;
            RegionScale = regionScale;
            SpawnThreshold = spawnThresh;
            MoteScale = moteScale;
        }
    }

    public class BiomeData
    {
        public Overworld.Biome Biome { get; set; }
        public string Name { get { return Biome.ToString(); } }
        public List<VegetationData> Vegetation { get; set; }
        public List<DetailMoteData> Motes { get; set; }
        public string GrassVoxel { get; set; }
        public string SoilVoxel { get; set; }
        public string SubsurfVoxel { get; set; }
        public string ShoreVoxel { get; set; }

        public BiomeData(Overworld.Biome biome)
        {
            Biome = biome;
            Vegetation = new List<VegetationData>();
            Motes = new List<DetailMoteData>();
        }

    }
}
