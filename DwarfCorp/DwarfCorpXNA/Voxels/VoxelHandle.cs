// DestinationVoxel.cs
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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DwarfCorp
{
    /// <summary>
    /// An atomic cube in the world which represents a bit of terrain. 
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelHandle : IBoundedObject
    {
        protected bool Equals(VoxelHandle other)
        {
            return Equals(Chunk, other.Chunk) && Index == other.Index;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Chunk != null ? Chunk.GetHashCode() : 0)*397) ^ GridPosition.GetHashCode();
            }
        }

        [JsonIgnore]
        private VoxelChunk _chunk = null;


        [JsonIgnore]
        public VoxelChunk Chunk 
        {
            get { return _chunk; }
            set 
            { 
                _chunk = value;
                if (_chunk != null) ChunkID = value.ID;
            }
        }

        [JsonIgnore]
        public Vector3 WorldPosition 
        {
            get
            {
                var globalPosition = ChunkID + GridPosition;
                return new Vector3(globalPosition.X, globalPosition.Y, globalPosition.Z);
            }
        }

        [JsonIgnore]
        public GlobalVoxelCoordinate Coordinate
        {
            get { return ChunkID + GridPosition; }
        }

        [JsonIgnore]
        public VoxelType Type
        {
            get
            {
                return VoxelType.TypeList[Chunk.Data.Types[Index]];
            }
            set
            {
                Chunk.Data.Types[Index] = (byte) value.ID;
                Chunk.Data.Health[Index] = (byte) value.StartingHealth;
            }
        }

        private int index = 0;
        [JsonIgnore]
        public int Index
        {
            get { return index; }
        }

        [JsonIgnore]
        public bool IsVisible 
        {
            get { return  GridPosition.Y <= Chunk.Manager.ChunkData.MaxViewingLevel; }
        }

        [JsonIgnore]
        public bool IsExplored
        {
            //get { return true; }
            get { return !GameSettings.Default.FogofWar || Chunk.Data.IsExplored[Index]; }
            set { Chunk.Data.IsExplored[Index] = value; }
        }

        private LocalVoxelCoordinate gridpos = new LocalVoxelCoordinate(0,0,0);

        public LocalVoxelCoordinate GridPosition
        {
            get { return gridpos; }
            set 
            { 
                gridpos = value;

                if (Chunk != null)
                {
                    index = VoxelConstants.DataIndexOf(gridpos);
                }
            }
        }

        // This function does the same as setting Chunk then GridPosition except avoids regenerating the quick compare
        // more than once.  Only set generateQuickCompare to false if you intend the voxel to be a throwaway
        // during a time sensitive loop.
        public void ChangeVoxel(VoxelChunk chunk, LocalVoxelCoordinate gridPosition, bool generateQuickCompare = true)
        {
            System.Diagnostics.Debug.Assert(chunk != null, "ChangeVoxel was passed a null chunk.");
            _chunk = chunk;
            chunkID = _chunk.ID;
            gridpos = gridPosition;
            index = VoxelConstants.DataIndexOf(gridpos);
        }

        [JsonIgnore]
        public RampType RampType
        {
            get { return Chunk.Data.RampTypes[Index]; }
            set { Chunk.Data.RampTypes[Index] = value; }
        }

        [JsonIgnore]
        public bool IsInterior
        {
            get { return Chunk.IsInterior((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z); }
        }

        // Todo: %KILL% - Can get from chunk. Verify proper serialization.
        private GlobalChunkCoordinate chunkID = new GlobalChunkCoordinate(0, 0, 0);
        public GlobalChunkCoordinate ChunkID
        {
            get { return chunkID; }
            set { chunkID = value; }
        }

        public VoxelHandle(VoxelHandle other)
        {
            Chunk = other.Chunk;
            GridPosition = other.GridPosition;
        }

        [JsonIgnore]
        public float Health
        {
            get { return (float) Chunk.Data.Health[Index]; }
            set
            {
                if (Type.IsInvincible) return;
                Chunk.Data.Health[Index] = (byte)(Math.Max(Math.Min(value, 255.0f), 0.0f));
            }
        }
      
        public uint GetID()
        {
            return (uint) GetHashCode();
        }

        // Todo: %KILL%
        public bool GetNeighborBySuccessor(GlobalVoxelOffset Offset, ref VoxelHandle neighbor, bool requireQuickCompare = true)
        {
            Debug.Assert(neighbor != null, "Null reference passed");
            Debug.Assert(_chunk != null, "DestinationVoxel has no valid chunk reference");

            var globalPosition = ChunkID + GridPosition;
            var neighborPosition = globalPosition + Offset;
            var neighborChunkID = neighborPosition.GetGlobalChunkCoordinate();
            VoxelChunk neighborChunk = null;
            if (Chunk.Manager.ChunkData.ChunkMap.TryGetValue(neighborChunkID, out neighborChunk))
            {
                neighbor.ChangeVoxel(neighborChunk, neighborPosition.GetLocalVoxelCoordinate(), requireQuickCompare);
                return true;
            }
            return false;
        }
     
        [JsonIgnore]
        public bool IsEmpty
        {
            get { return Type.ID == 0; }
        }

        [JsonIgnore]
        public int SunColor { get { return Chunk.Data.SunColors[Index]; }}

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != this.GetType()) return false;
            return Equals((VoxelHandle) o);
        }

        public List<Body> Kill()
        {
            if (IsEmpty)
            {
                return null;
            }

            if(Chunk.Manager.World.ParticleManager != null)
            {
                Chunk.Manager.World.ParticleManager.Trigger(Type.ParticleType, WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
                Chunk.Manager.World.ParticleManager.Trigger("puff", WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            if(Chunk.Manager.World.Master != null)
            {
                Chunk.Manager.World.Master.Faction.OnVoxelDestroyed(this);
            }

            Type.ExplosionSound.Play(WorldPosition);

            List<Body> emittedResources = null;
            if (Type.ReleasesResource)
            {
                float randFloat = MathFunctions.Rand();

                if (randFloat < Type.ProbabilityOfRelease)
                {
                    emittedResources = new List<Body>
                    {
                        EntityFactory.CreateEntity<Body>(Type.ResourceToRelease + " Resource",
                            WorldPosition + new Vector3(0.5f, 0.5f, 0.5f))
                    };
                }
            }

            Chunk.Manager.KilledVoxels.Add(new TemporaryVoxelHandle(Chunk, GridPosition));
            Chunk.Data.Types[Index] = 0;
            return emittedResources;
        }

        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(WorldPosition, 1);
        }

        public BoundingBox GetBoundingBox()
        {
            var pos = WorldPosition;
            return new BoundingBox(pos, pos + Vector3.One);
        }

        public VoxelHandle()
        {
            
        }

        public VoxelHandle(LocalVoxelCoordinate gridPosition, VoxelChunk chunk)
        {
            Chunk = chunk;
            if (chunk != null)
                chunkID = chunk.ID;
            GridPosition = gridPosition;
        }

        public VoxelHandle(ChunkData Chunks, GlobalVoxelCoordinate Coordinate)
        {
            Chunks.ChunkMap.TryGetValue(Coordinate.GetGlobalChunkCoordinate(), out _chunk);
            if (Chunk != null)
                chunkID = Chunk.ID;
            GridPosition = Coordinate.GetLocalVoxelCoordinate();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            WorldManager world = ((WorldManager) context.Context);
            if (world.ChunkManager.ChunkData.ChunkMap.ContainsKey(chunkID))
            {
                Chunk = world.ChunkManager.ChunkData.ChunkMap[chunkID];
                index = VoxelConstants.DataIndexOf(GridPosition);
            }
        }

        [JsonIgnore]
        public WaterCell Water
        {
            get { return Chunk.Data.Water[Index]; }
            set { Chunk.Data.Water[Index] = value; }
        }

        [JsonIgnore]
        public byte WaterLevel
        {
            get { return Water.WaterLevel; }
            set
            {
                WaterCell cell = Water;
                cell.WaterLevel = value;
                Chunk.Data.Water[Index] = cell;
            }
        }

        public bool GetNeighbor(Vector3 dir, ref VoxelHandle vox)
        {
            return Chunk.Manager.ChunkData.GetVoxel(WorldPosition + dir, ref vox);
        }

        public override string ToString()
        {
            return String.Format("DestinationVoxel {{{0}, {1}, {2}}}", gridpos.X, gridpos.Y, gridpos.Z);
        }

        // Todo: %KILL%
        public TemporaryVoxelHandle tvh
        {
            get
            {
                return new TemporaryVoxelHandle(Chunk, GridPosition);
            }
        }
    }

}
