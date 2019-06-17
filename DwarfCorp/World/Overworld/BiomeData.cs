using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class BiomeData
    {
        public byte Biome { get; set; }


        public string Name;

        public struct Layer
        {
            public string VoxelType { get; set; }
            public int Depth { get; set; }
        }

        public List<VegetationData> Vegetation { get; set; }
        public List<DetailMoteData> Motes { get; set; }
        public List<FaunaData> Fauna { get; set; }
        public string GrassDecal = "";
        public Layer SoilLayer { get; set; }
        public List<Layer> SubsurfaceLayers { get; set; }
        public string ShoreVoxel { get; set; }
        public bool ClumpGrass { get; set; }
        public float ClumpSize { get; set; }
        public float ClumpTreshold { get; set; }
        public Color MapColor { get; set; }
        public bool Underground { get; set; }
        public float Height { get; set; }
        public float Temp { get; set; }
        public float Rain { get; set; }
        public Point Icon { get; set; }
        //public Color GrassTint = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        public string DayAmbience { get; set; }
        public string NightAmbience { get; set; }
        public bool WaterSurfaceIce { get; set; }
        public bool WaterIsLava { get; set; }
        public string RuinFloorType { get; set; }
        public string RuinWallType { get; set; }

        public BiomeData()
        {
            ShoreVoxel = "Sand";
            WaterIsLava = false;
            RuinFloorType = "Cobble";
            RuinWallType = "Shale";
        }

        // Todo: Move defaults to above and remove this.
        public BiomeData(byte biome)
        {
            WaterIsLava = false;
            Biome = biome;
            Vegetation = new List<VegetationData>();
            Motes = new List<DetailMoteData>();
            Fauna = new List<FaunaData>();
            SubsurfaceLayers = new List<Layer>();
            ClumpGrass = false;
            ClumpSize = 30.0f;
            ClumpTreshold = 0.75f;
            MapColor = Color.White;
            Underground = false;
            ShoreVoxel = "Sand";
            DayAmbience = "";
            NightAmbience = "";
            WaterSurfaceIce = false;
        }
    }

}
