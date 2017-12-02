// DecalLibrary.cs
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
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DecalLibrary
    {
        public static DecalType emptyType = null;

        public static Dictionary<string, DecalType> Types = new Dictionary<string, DecalType>();
        public static List<DecalType> TypeList;

        public DecalLibrary()
        {
        }

        private static DecalType.FringeTileUV[] CreateFringeUVs(Point[] Tiles)
        {
            System.Diagnostics.Debug.Assert(Tiles.Length == 3);

            var r = new DecalType.FringeTileUV[8];

            // North
            r[0] = new DecalType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2) + 1, 16, 32);
            // East
            r[1] = new DecalType.FringeTileUV((Tiles[1].X * 2) + 1, Tiles[1].Y, 32, 16);
            // South
            r[2] = new DecalType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2), 16, 32);
            // West
            r[3] = new DecalType.FringeTileUV(Tiles[1].X * 2, Tiles[1].Y, 32, 16);

            // NW
            r[4] = new DecalType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2) + 1, 32, 32);
            // NE
            r[5] = new DecalType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2) + 1, 32, 32);
            // SE
            r[6] = new DecalType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2), 32, 32);
            // SW
            r[7] = new DecalType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2), 32, 32);

            return r;
        }


        public static void InitializeDefaultLibrary()
        {
            TypeList = FileUtils.LoadJson<List<DecalType>>(ContentPaths.decal_types, false);
            emptyType = TypeList[0];

            byte ID = 0;
            foreach (var type in TypeList)
            {
                type.ID = ID;
                ++ID;

                Types[type.Name] = type;

                if (type.HasFringeTransitions)
                    type.FringeTransitionUVs = CreateFringeUVs(type.FringeTiles);
            }
        }

        public static DecalType GetDecalType(byte id)
        {
            return TypeList[id];
        }

        public static DecalType GetDecalType(string name)
        {
            DecalType r = null;
            Types.TryGetValue(name, out r);
            return r;
        }
    }
}