// Faction.cs
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
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FarmSet
    {
        public enum AddDesignationResult
        {
            AlreadyExisted,
            Added
        }

        public enum RemoveDesignationResult
        {
            DidntExist,
            Removed
        }

        [JsonProperty]
        private Dictionary<ulong, FarmTile> FarmTiles = new Dictionary<ulong, FarmTile>();


        public AddDesignationResult AddFarmTile(FarmTile Tile)
        {
            var key = GetVoxelQuickCompare(Tile.Voxel);

            if (FarmTiles.ContainsKey(key))
                return AddDesignationResult.AlreadyExisted;
            else
            {
                FarmTiles.Add(key, Tile);
                return AddDesignationResult.Added;
            }
        }

        public RemoveDesignationResult RemoveVoxelDesignation(FarmTile Tile)
        {
            var key = GetVoxelQuickCompare(Tile.Voxel);
            if (!FarmTiles.ContainsKey(key)) return RemoveDesignationResult.DidntExist;
            FarmTiles.Remove(key);
            return RemoveDesignationResult.Removed;
        }

        public FarmTile GetVoxelDesignation(VoxelHandle Voxel)
        {
            var key = GetVoxelQuickCompare(Voxel);
            if (!FarmTiles.ContainsKey(key)) return null;
            return FarmTiles[key];
        }

        public bool IsVoxelDesignation(VoxelHandle Voxel)
        {
            var key = GetVoxelQuickCompare(Voxel);
            if (!FarmTiles.ContainsKey(key)) return false;
            return true;
        }

        public IEnumerable<FarmTile> EnumerateDesignations()
        {
            return FarmTiles.Values;
        }
        
        // Todo: Kill this. It checks every designation every frame. Hook the voxel change mechanism so it
        //      only has to check what has changed!
        public void CleanupDesignations()
        {
            var toRemove = new List<FarmTile>();
            foreach (var tile in FarmTiles.Values)
                            if (!tile.Voxel.IsValid || tile.Voxel.IsEmpty || tile.Voxel.Type.Name != "TilledSoil")
                                toRemove.Add(tile);
                

            foreach (var d in toRemove)
                RemoveVoxelDesignation(d);
        }

        private static ulong GetVoxelQuickCompare(VoxelHandle V)
        {
            var coord = V.Coordinate.GetGlobalChunkCoordinate();
            var index = VoxelConstants.DataIndexOf(V.Coordinate.GetLocalVoxelCoordinate());

            ulong q = 0;
            q |= (((ulong)coord.X & 0xFFFF) << 48);
            q |= (((ulong)coord.Y & 0xFFFF) << 32);
            q |= (((ulong)coord.Z & 0xFFFF) << 16);
            q |= ((ulong)index & 0xFFFF);
            return q;
        }
    }
}
