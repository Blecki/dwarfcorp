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
            public string VoxelType;
            public int Depth;
        }

        public List<VegetationData> Vegetation = new List<VegetationData>();
        public List<DetailMoteData> Motes = new List<DetailMoteData>();
        public List<FaunaData> Fauna = new List<FaunaData>();
        public string GrassDecal = "";
        public Layer SoilLayer = new Layer();
        public List<Layer> SubsurfaceLayers = new List<Layer>();
        public string ShoreVoxel = "Sand";
        public bool ClumpGrass = false;
        public float ClumpSize = 30.0f;
        public float ClumpTreshold = 0.75f;
        public Color MapColor = Color.White;
        public bool Underground = false;
        public float Height = 0.0f;
        public float Temp = 0.0f;
        public float Rain = 0.0f;
        public Point Icon = Point.Zero;
        public string DayAmbience = "";
        public string NightAmbience = "";
        public bool WaterSurfaceIce = false;
        public bool WaterIsLava = false;
        public string RuinFloorType = "Cobble";
        public string RuinWallType = "Shale";

        public BiomeData()
        {
        }
    }
}
