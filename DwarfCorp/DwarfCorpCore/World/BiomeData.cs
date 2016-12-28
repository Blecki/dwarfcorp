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

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    ///     A biome defines how chunks are to be generated. Biomes specify
    ///     what kind of voxels, vegetation, and other bits are included in the generation process.
    /// </summary>
    public class BiomeData
    {
        public BiomeData(Overworld.Biome biome)
        {
            Biome = biome;
            Vegetation = new List<VegetationData>();
            Motes = new List<DetailMoteData>();
            Fauna = new List<FaunaData>();
            ClumpGrass = false;
            ClumpSize = 30.0f;
            ClumpTreshold = 0.75f;
            MapColor = Color.White;
            Underground = false;
        }

        /// <summary>
        ///     Type of biome.
        /// </summary>
        public Overworld.Biome Biome { get; set; }

        /// <summary>
        ///     Name of the biome (not settable)
        /// </summary>
        public string Name
        {
            get { return Biome.ToString(); }
        }

        /// <summary>
        ///     Defines the kind of vegetation that grows in this biome.
        /// </summary>
        public List<VegetationData> Vegetation { get; set; }

        /// <summary>
        ///     Determines the kind of special detail motes that appear in this biome.
        /// </summary>
        public List<DetailMoteData> Motes { get; set; }

        /// <summary>
        ///     Determines the kind of creatures that live in this biome.
        /// </summary>
        public List<FaunaData> Fauna { get; set; }

        /// <summary>
        ///     Kind of voxel to create at the surface of the biome.
        /// </summary>
        public string GrassVoxel { get; set; }

        /// <summary>
        ///     Kind of soil to create just below the surface of the biome.
        /// </summary>
        public string SoilVoxel { get; set; }

        /// <summary>
        ///     Kind of voxel to create below the soil.
        /// </summary>
        public string SubsurfVoxel { get; set; }

        /// <summary>
        ///     Kind of voxel to create bordering oceans/lake/rivers.
        /// </summary>
        public string ShoreVoxel { get; set; }

        /// <summary>
        ///     If true, grass will appear in clumps instead of blanketing the whole biome.
        /// </summary>
        public bool ClumpGrass { get; set; }

        /// <summary>
        ///     Scale of grass clumps in voxels.
        /// </summary>
        public float ClumpSize { get; set; }

        /// <summary>
        ///     The closer this is to 1.0, the more compact grass clumps will be.
        /// </summary>
        public float ClumpTreshold { get; set; }

        /// <summary>
        ///     The color of this biome as displayed in the overworld map.
        /// </summary>
        public Color MapColor { get; set; }

        /// <summary>
        ///     If true, this biome is underground (i.e in caves)
        /// </summary>
        public bool Underground { get; set; }

        /// <summary>
        ///     This is the representative height of the biome on a scale of [0-1]
        ///     0.0 is under the ocean, 1.0 is the highest mountains.
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        ///     The representative temperature of the biome on a scale of [0-1]
        ///     0.0 is the coldest, 1.0 the hottest
        /// </summary>
        public float Temp { get; set; }

        /// <summary>
        ///     Representative rainfall of the biome on a scale of [0 - 1]
        ///     0.0 is completely dry, 1.0 completely wet.
        /// </summary>
        public float Rain { get; set; }
    }
}