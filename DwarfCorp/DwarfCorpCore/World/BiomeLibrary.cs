// BiomeLibrary.cs
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
    ///     A static collection of biome types. Biomes tell the voxel chunk generator what kinds of voxels
    ///     to put into the environment, and how to populate it with animals and plants. Biomes are chosen
    ///     based on a number of parameters like rainfall, height, and temperature.
    /// </summary>
    public class BiomeLibrary
    {
        /// <summary>
        ///     Static dictionary of all biomes.
        /// </summary>
        public static Dictionary<Overworld.Biome, BiomeData> Biomes = new Dictionary<Overworld.Biome, BiomeData>();

        /// <summary>
        ///     Generate biomes from a JSON data file.
        /// </summary>
        public static void InitializeStatics()
        {
            Biomes = ContentPaths.LoadFromJson<Dictionary<Overworld.Biome, BiomeData>>(ContentPaths.World.biomes);
        }

        /// <summary>
        ///     Convenient dictionary from biome name to color.
        /// </summary>
        /// <returns>A dictionary mapping biome name to its color on the map.</returns>
        public static Dictionary<string, Color> CreateBiomeColors()
        {
            var toReturn = new Dictionary<string, Color>();
            foreach (var pair in Biomes)
            {
                toReturn[pair.Value.Name] = pair.Value.MapColor;
            }
            return toReturn;
        }
    }
}