using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A biome defines how chunks are to be generated. Biomes specify
    /// what kind of voxels, vegetation, and other bits are included in the generation process.
    /// </summary>
    public class BiomeData
    {
        public Overworld.Biome Biome { get; set; }

        public string Name
        {
            get { return Biome.ToString(); }
        }

        public List<VegetationData> Vegetation { get; set; }
        public List<DetailMoteData> Motes { get; set; }
        public List<FaunaData> Fauna { get; set; } 
        public string GrassVoxel { get; set; }
        public string SoilVoxel { get; set; }
        public string SubsurfVoxel { get; set; }
        public string ShoreVoxel { get; set; }

        public BiomeData(Overworld.Biome biome)
        {
            Biome = biome;
            Vegetation = new List<VegetationData>();
            Motes = new List<DetailMoteData>();
            Fauna = new List<FaunaData>();
        }
    }

}