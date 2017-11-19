// BiomeData.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

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

        public struct Layer
        {
            public string VoxelType { get; set; }
            public int Depth { get; set; }
        }

        public List<VegetationData> Vegetation { get; set; }
        public List<DetailMoteData> Motes { get; set; }
        public List<FaunaData> Fauna { get; set; }
        public Layer GrassLayer { get; set; }
        public string GrassDecal = "grass";
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
        public int Icon { get; set; }
        //public Color GrassTint = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        public string DayAmbience { get; set; }
        public string NightAmbience { get; set; }

        public BiomeData()
        {
            ShoreVoxel = "Sand";
        }

        public BiomeData(Overworld.Biome biome)
        {
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
        }
    }

}
