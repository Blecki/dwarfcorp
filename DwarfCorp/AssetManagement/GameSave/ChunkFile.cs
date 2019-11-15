using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{
    /// <summary>
    ///     Minimal representation of a chunk.
    ///     Exists to write to and from files.
    /// </summary>
    [Serializable]
    public class ChunkFile
    {
        public static string Extension = "chunk";

        public GlobalChunkCoordinate ID;
        public GlobalVoxelCoordinate Origin;

        public byte[] Liquid;
        public byte[] Types;
        public byte[] GrassType;
        public byte[] RampsSunlightExplored;
        public byte[] Decals;

        public Dictionary<int, String> VoxelTypeMap;
        public Dictionary<int, String> GrassTypeMap;
        public Dictionary<int, String> DecalTypeMap;

        public ChunkFile()
        {
        }

        public static ChunkFile CreateFromChunk(VoxelChunk chunk)
        {
            var r = new ChunkFile
            {
                ID = chunk.ID,
                Types = new byte[VoxelConstants.ChunkVoxelCount],
                Liquid = new byte[VoxelConstants.ChunkVoxelCount],
                GrassType = new byte[VoxelConstants.ChunkVoxelCount],
                RampsSunlightExplored = new byte[VoxelConstants.ChunkVoxelCount],
                Decals = new byte[VoxelConstants.ChunkVoxelCount],
                Origin = chunk.Origin
            };

            chunk.Data.Types.CopyTo(r.Types, 0);
            chunk.Data.Grass.CopyTo(r.GrassType, 0);
            chunk.Data.RampsSunlightExploredPlayerBuilt.CopyTo(r.RampsSunlightExplored, 0);
            chunk.Data.Liquid.CopyTo(r.Liquid, 0);
            chunk.Data.Decal.CopyTo(r.Decals, 0);

            r.VoxelTypeMap = Library.GetVoxelTypeMap();
            r.GrassTypeMap = Library.GetGrassTypeMap();
            r.DecalTypeMap = Library.GetDecalTypeMap();

            return r;
        }

        private void Remap(int ElementCount, Dictionary<int, String> StoredMap, Dictionary<int, String> NewMap, Func<int, int> Lookup, Action<int, int> Set)
        {
            if (StoredMap == null) return;

            var newReverseMap = new Dictionary<String, int>();
            foreach (var mapping in NewMap)
                newReverseMap.Add(mapping.Value, mapping.Key);

            var replacementMap = new Dictionary<int, int>();
            foreach (var mapping in StoredMap)
                if (newReverseMap.ContainsKey(mapping.Value))
                {
                    var newId = newReverseMap[mapping.Value];
                    if (mapping.Key != newId)
                        replacementMap.Add(mapping.Key, newId);
                }

            // If there are no changes, skip the expensive iteration.
            if (replacementMap.Count != 0)
                for (var i = 0; i < ElementCount; ++i)
                {
                    var value = Lookup(i);
                    if (replacementMap.ContainsKey(value))
                        Set(i, replacementMap[value]);
                }
        }

        public VoxelChunk ToChunk(ChunkManager Manager)
        {
            var c = new VoxelChunk(Manager, ID);

            for (var i = 0; i < VoxelConstants.ChunkVoxelCount; ++i)
                c.Data.Types[i] = Types[i];

            // Remap the saved voxel ids to the ids of the currently loaded voxels.
            Remap(c.Data.Types.Length, VoxelTypeMap, Library.GetVoxelTypeMap(), (index) => Types[index], (index, value) => c.Data.Types[index] = (byte)value);

            for (var i = 0; i < VoxelConstants.ChunkVoxelCount; ++i)
                if (c.Data.Types[i] > 0)
                    c.Data.VoxelsPresentInSlice[(i >> VoxelConstants.ZDivShift) >> VoxelConstants.XDivShift] += 1;

            if (Liquid != null)
            {
                Liquid.CopyTo(c.Data.Liquid, 0);
                for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                    for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                        for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                            c.Data.LiquidPresent[y] += VoxelHandle.UnsafeCreateLocalHandle(c, new LocalVoxelCoordinate(x, y, z)).LiquidLevel;
            }

            if (RampsSunlightExplored != null)
                RampsSunlightExplored.CopyTo(c.Data.RampsSunlightExploredPlayerBuilt, 0);

            if (GrassType != null)
                GrassType.CopyTo(c.Data.Grass, 0);

            if (Decals != null)
                Decals.CopyTo(c.Data.Decal, 0);

            // Remap grass.
            Remap(c.Data.Grass.Length, GrassTypeMap, Library.GetGrassTypeMap(),
                (index) => c.Data.Grass[index] >> VoxelConstants.GrassTypeShift,
                (index, value) => c.Data.Grass[index] = (byte)((c.Data.Grass[index] & VoxelConstants.GrassDecayMask) | (value << VoxelConstants.GrassTypeShift)));

            // Remap decals.
            Remap(c.Data.Decal.Length, DecalTypeMap, Library.GetDecalTypeMap(),
                (index) => (c.Data.Decal[index] & VoxelConstants.DecalTypeMask) >> VoxelConstants.DecalTypeShift,
                (index, value) => c.Data.Decal[index] = (byte)((c.Data.Decal[index] & VoxelConstants.InverseDecalTypeMask) | (value & VoxelConstants.DecalTypeMask)));

            return c;
        }
    }
}