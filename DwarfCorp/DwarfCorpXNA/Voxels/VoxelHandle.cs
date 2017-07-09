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
        [JsonIgnore]
        private WorldManager World;

        public GlobalVoxelCoordinate Coordinate { get; private set; }


        protected bool Equals(VoxelHandle other)
        {
            return Equals(Chunk, other.Chunk) && Index == other.Index;
        }

        public bool IsSameAs(VoxelHandle other)
        {
            if (quickCompare == other.quickCompare) return true;
            return false;
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
        public Vector3 Position 
        {
            get
            {
                return GridPosition + Chunk.Origin;
            }
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

        [JsonIgnore]
        public string TypeName
        {
            get { return this.Type.Name; }
        }

        private int index = 0;
        [JsonIgnore]
        public int Index
        {
            get { return index; }
        }

        [JsonIgnore]
        public BoxPrimitive Primitive 
        {
            get { return VoxelLibrary.GetPrimitive(Type); }
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

        private Vector3 gridpos = Vector3.Zero;

        public Vector3 GridPosition
        {
            get { return gridpos; }
            set 
            { 
                gridpos = value;

                if (Chunk != null)
                {
                    index = Chunk.Data.IndexAt((int)gridpos.X, (int)gridpos.Y, (int)gridpos.Z);
                    RegenerateQuickCompare();
                }
            }
        }

        // This function does the same as setting Chunk then GridPosition except avoids regenerating the quick compare
        // more than once.  Only set generateQuickCompare to false if you intend the voxel to be a throwaway
        // during a time sensitive loop.
        public void ChangeVoxel(VoxelChunk chunk, Vector3 gridPosition, bool generateQuickCompare)
        {
            ChangeVoxel(chunk, new Point3(gridPosition), generateQuickCompare);
        }

        // This function does the same as setting Chunk then GridPosition except avoids regenerating the quick compare
        // more than once.  Only set generateQuickCompare to false if you intend the voxel to be a throwaway
        // during a time sensitive loop.
        public void ChangeVoxel(VoxelChunk chunk, Point3 gridPosition, bool generateQuickCompare = true)
        {
            System.Diagnostics.Debug.Assert(chunk != null, "ChangeVoxel was passed a null chunk.");
            _chunk = chunk;
            chunkID = _chunk.ID;
            gridpos = gridPosition.ToVector3();
            index = Chunk.Data.IndexAt((int)gridpos.X, (int)gridpos.Y, (int)gridpos.Z);
            if (generateQuickCompare) RegenerateQuickCompare();
            else quickCompare = invalidCompareValue;
        }


        [JsonIgnore]
        public static List<VoxelVertex> VoxelVertexList { get; set; }
        private static bool staticsCreated;

        [JsonIgnore]
        public bool IsDead
        {
            get { return Health <= 0; }
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
        private static readonly Color BlankColor = new Color(0, 255, 0);

        private GlobalChunkCoordinate chunkID = new GlobalChunkCoordinate(0, 0, 0);
        public GlobalChunkCoordinate ChunkID
        {
            get { return chunkID; }
            set { chunkID = value; RegenerateQuickCompare(); }
        }

        [JsonIgnore]
        private ulong quickCompare;
        private const ulong invalidCompareValue = 0xFFFFFFFFFFFFFFFFUL;

        [JsonIgnore]
        public ulong QuickCompare
        {
            get {
                //System.Diagnostics.Debug.Assert(quickCompare == invalidCompareValue, "DestinationVoxel was generated without Quick Compare.  Set using GridPosition instead.");
                return quickCompare;
            }
        }

        private void RegenerateQuickCompare()
        {
            // long build of the ulong.
            ulong q = 0;
            q |= (((ulong)chunkID.X & 0xFFFF) << 48);
            q |= (((ulong)chunkID.Y & 0xFFFF) << 32);
            q |= (((ulong)chunkID.Z & 0xFFFF) << 16);
            q |= ((ulong)index & 0xFFFF);
            quickCompare = q;
            //quickCompare = (ulong) (((chunkID.X & 0xFFFF) << 48) | ((chunkID.Y & 0xFFFF) << 32) | ((chunkID.Y & 0xFFFF) << 16) | (index & 0xFFFF));
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


        public bool IsTopEmpty()
        {
            if(GridPosition.Y >= Chunk.SizeY)
            {
                return true;
            }
            return
                Chunk.Data.Types[
                    Chunk.Data.IndexAt((int) GridPosition.X, (int) GridPosition.Y + 1, (int) GridPosition.Z)] == 0;
        }

        public VoxelHandle GetVoxelAbove()
        {
            if (Chunk == null || GridPosition.Y >= Chunk.SizeY - 1)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int) GridPosition.X, (int) GridPosition.Y + 1, (int) GridPosition.Z);
        }

        public VoxelHandle GetVoxelBelow()
        {
            if (GridPosition.Y <=0)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int)GridPosition.X, (int)GridPosition.Y - 1, (int)GridPosition.Z);
        }

        public bool GetNeighborBySuccessor(Vector3 succ, ref VoxelHandle neighbor, bool requireQuickCompare = true)
        {
            Debug.Assert(neighbor != null, "Null reference passed");
            Debug.Assert(_chunk != null, "DestinationVoxel has no valid chunk reference");

            Vector3 newPos = gridpos + succ;
            Point3 chunkSuccessor = Point3.Zero;
            bool useSuccessor = false;

            if (newPos.X >= _chunk.SizeX)
            {
                chunkSuccessor.X = 1;
                newPos.X = 0;
                useSuccessor = true;
            }
            else if (newPos.X < 0)
            {
                chunkSuccessor.X = -1;
                newPos.X = _chunk.SizeX - 1;
                useSuccessor = true;
            }

            if (newPos.Y >= _chunk.SizeY)
            {
                chunkSuccessor.Y = 1;
                newPos.Y = 0;
                useSuccessor = true;
            }
            else if (newPos.Y < 0)
            {
                chunkSuccessor.Y = -1;
                newPos.Y = _chunk.SizeY - 1;
                useSuccessor = true;
            }

            if (newPos.Z >= _chunk.SizeZ)
            {
                chunkSuccessor.Z = 1;
                newPos.Z = 0;
                useSuccessor = true;
            }
            else if (newPos.Z < 0)
            {
                chunkSuccessor.Z = -1;
                newPos.Z = _chunk.SizeZ - 1;
                useSuccessor = true;
            }

            VoxelChunk useChunk;
            if (useSuccessor)
            {
                useChunk = _chunk.EuclidianNeighbors[VoxelChunk.SuccessorToEuclidianLookupKey(chunkSuccessor)];
                if (useChunk == null) return false;
            } else
            {
                useChunk = _chunk;
            }
            neighbor.ChangeVoxel(useChunk, newPos, requireQuickCompare);
            return true;
        }

        public bool IsBottomEmpty()
        {
            if (GridPosition.Y <= 0)
            {
                return true;
            }
            return
                Chunk.Data.Types[
                    Chunk.Data.IndexAt((int)GridPosition.X, (int)GridPosition.Y - 1, (int)GridPosition.Z)] == 0;
        }

        public static bool IsInteriorPoint(Point3 gridPosition, VoxelChunk chunk)
        {
            return chunk.IsInterior(gridPosition.X, gridPosition.Y, gridPosition.Z);
        }

        public static bool HasFlag(RampType ramp, RampType flag)
        {
            return (ramp & flag) == flag;
        }
       
        [JsonIgnore]
        public bool IsEmpty
        {
            get { return Type.ID == 0; }
        }

        [JsonIgnore]
        public int SunColor { get { return Chunk.Data.SunColors[Index]; }}

        public void SetFromData(VoxelChunk chunk, Vector3 gridPosition)
        {
            Chunk = chunk;
            GridPosition = gridPosition;
            index = Chunk.Data.IndexAt((int) gridPosition.X, (int) gridPosition.Y, (int) gridPosition.Z);
            RegenerateQuickCompare();
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != this.GetType()) return false;
            return Equals((VoxelHandle) o);
        }

        public void UpdateStatics()
        {
            if(staticsCreated)
            {
                return;
            }

            VoxelVertexList = new List<VoxelVertex>
            {
                VoxelVertex.BackBottomLeft,
                VoxelVertex.BackBottomRight,
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight,
                VoxelVertex.FrontBottomRight,
                VoxelVertex.FrontBottomLeft,
                VoxelVertex.FrontTopRight,
                VoxelVertex.FrontTopLeft
            };
            staticsCreated = true;
        }

        public List<Body> Kill()
        {
            if (IsEmpty)
            {
                return null;
            }

            if(Chunk.Manager.World.ParticleManager != null)
            {
                Chunk.Manager.World.ParticleManager.Trigger(Type.ParticleType, Position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
                Chunk.Manager.World.ParticleManager.Trigger("puff", Position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            if(Chunk.Manager.World.Master != null)
            {
                Chunk.Manager.World.Master.Faction.OnVoxelDestroyed(this);
            }

            Type.ExplosionSound.Play(Position);

            List<Body> emittedResources = null;
            if (Type.ReleasesResource)
            {
                float randFloat = MathFunctions.Rand();

                if (randFloat < Type.ProbabilityOfRelease)
                {
                    emittedResources = new List<Body>
                    {
                        EntityFactory.CreateEntity<Body>(Type.ResourceToRelease + " Resource",
                            Position + new Vector3(0.5f, 0.5f, 0.5f))
                    };
                }
            }

            Chunk.Manager.KilledVoxels.Add(this);
            Chunk.Data.Types[Index] = 0;
            return emittedResources;
        }

        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(Position, 1);
        }

        public BoundingBox GetBoundingBox()
        {
            var pos = Position;
            return new BoundingBox(pos, pos + Vector3.One);
        }

        public VoxelHandle()
        {
            
        }

        public VoxelHandle(Point3 gridPosition, VoxelChunk chunk)
        {
            UpdateStatics();
            Chunk = chunk;
            if (chunk != null)
                chunkID = chunk.ID;
            GridPosition = new Vector3(gridPosition.X, gridPosition.Y, gridPosition.Z);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            WorldManager world = ((WorldManager) context.Context);
            if (world.ChunkManager.ChunkData.ChunkMap.ContainsKey(chunkID))
            {
                Chunk = world.ChunkManager.ChunkData.ChunkMap[chunkID];
                index = Chunk.Data.IndexAt((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z);
                RegenerateQuickCompare();
            }
        }

        public BoxTransition ComputeTransitionValue(VoxelHandle[] manhattanNeighbors)
        {
            return Chunk.ComputeTransitionValue(Type.Transitions, (int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z, manhattanNeighbors);
        }

        public BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(VoxelHandle[] manhattanNeighbors)
        {
            if(!Type.HasTransitionTextures && Primitive != null)
            {
                return Primitive.UVs;
            }
            else if(Primitive == null)
            {
                return null;
            }
            else
            {
                return Type.TransitionTextures[ComputeTransitionValue(manhattanNeighbors)];
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
            return Chunk.Manager.ChunkData.GetVoxel(Position + dir, ref vox);
        }

        public override string ToString()
        {
            return String.Format("DestinationVoxel {{{0}, {1}, {2}}}", gridpos.X, gridpos.Y, gridpos.Z);
        }
    }

}
