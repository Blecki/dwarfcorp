// Voxel.cs
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
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DwarfCorp
{
    /// <summary>
    /// Specifies the location of a vertex on a voxel.
    /// </summary>
    public enum VoxelVertex
    {
        FrontTopLeft = 0,
        FrontTopRight,
        FrontBottomLeft,
        FrontBottomRight,
        BackTopLeft,
        BackTopRight,
        BackBottomLeft,
        BackBottomRight,
        Count
    }

    /// <summary>
    /// Specifies how a voxel is to be sloped.
    /// </summary>
    [Flags]
    public enum RampType
    {
        None = 0x0,
        TopFrontLeft = 0x1,
        TopFrontRight = 0x2,
        TopBackLeft = 0x4,
        TopBackRight = 0x8,
        Front = TopFrontLeft | TopFrontRight,
        Back = TopBackLeft | TopBackRight,
        Left = TopBackLeft | TopFrontLeft,
        Right = TopBackRight | TopFrontRight,
        All = TopFrontLeft | TopFrontRight | TopBackLeft | TopBackRight
    }

    /// <summary> Determines a transition texture type. Each phrase
    /// (front, left, back, right) defines whether or not a tile of the same type is
    /// on the given face</summary>
    [Flags]
    public enum TransitionTexture
    {
        None = 0,
        Front = 1,
        Right = 2,
        FrontRight = 3,
        Back = 4,
        FrontBack = 5,
        BackRight = 6,
        FrontBackRight = 7,
        Left = 8,
        FrontLeft = 9,
        LeftRight = 10,
        LeftFrontRight = 11,
        LeftBack = 12,
        FrontBackLeft = 13,
        LeftBackRight = 14,
        All = 15
    }

    /// <summary>
    /// An atomic cube in the world which represents a bit of terrain. 
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Voxel : IBoundedObject
    {
        #region Static methods and fields
        public static bool HasFlag(RampType ramp, RampType flag)
        {
            return (ramp & flag) == flag;
        }
        #endregion

        #region Serializable Fields and their Properties
        private Point3 _chunkID = new Point3(0, 0, 0);
        public Point3 ChunkID
        {
            get { return _chunkID; }
            set { _chunkID = value; RegenerateQuickCompare(); }
        }

        private Vector3 _gridpos = Vector3.Zero;

        /// <summary>
        /// The current position of this Voxel in VoxelChunk space.
        /// </summary>
        public Vector3 GridPosition
        {
            get { return _gridpos; }
            set
            {
                _gridpos = value;

                if (Chunk != null)
                {
                    _position = _gridpos + Chunk.Origin;
                    _index = _data.IndexAt((int)_gridpos.X, (int)_gridpos.Y, (int)_gridpos.Z);
                    RegenerateQuickCompare();
                }
            }
        }
        #endregion

        #region Nonserializable fields and their Properties
        [JsonIgnore]
        private int _index;

        /// <summary>
        /// The index into the VoxelData stucture.
        /// </summary>
        [JsonIgnore]
        public int Index
        {
            get { return _index; }
        }

        [JsonIgnore]
        private VoxelChunk _chunk;

        /// <summary>
        /// Reference to the VoxelData structure on the current chunk.  Only used internally.
        /// </summary>
        [JsonIgnore]
        private VoxelChunk.VoxelData _data;

        /// <summary>
        /// The VoxelChunk this Voxel belongs to.
        /// </summary>
        [JsonIgnore]
        public VoxelChunk Chunk
        {
            get { return _chunk; }
            set
            {
                _chunk = value;
                if (_chunk != null)
                {
                    _data = _chunk.Data;
                    ChunkID = value.ID;
                }
                else
                {
                    _data = null;
                }
            }
        }

        /// <summary>
        /// World position.  Recalulated when GridPosition changes to avoid having to calculate on each call.
        /// </summary>
        [JsonIgnore]
        private Vector3 _position;

        /// <summary>
        /// The position of the voxel in World Space
        /// </summary>
        [JsonIgnore]
        public Vector3 Position
        {
            get
            {
                return _position;
            }
        }

        [JsonIgnore]
        private ulong _quickCompare;
        private const ulong invalidCompareValue = 0xFFFFFFFFFFFFFFFFUL;

        [JsonIgnore]
        public ulong QuickCompare
        {
            get
            {
                Debug.Assert(_quickCompare != invalidCompareValue, "Voxel was generated without Quick Compare.  Set using GridPosition instead.");
                return _quickCompare;
            }
        }
        #endregion

        #region VoxelData backed Properties
        [JsonIgnore]
        public int SunColor { get { return _data.SunColors[Index]; } }

        [JsonIgnore]
        public WaterCell Water
        {
            get { return _data.Water[Index]; }
            set { _data.Water[Index] = value; }
        }

        [JsonIgnore]
        public byte WaterLevel
        {
            get { return Water.WaterLevel; }
            set
            {
                WaterCell cell = Water;
                cell.WaterLevel = value;
                _data.Water[Index] = cell;
            }
        }

        /// <summary>
        /// The type of voxel found in this location.
        /// </summary>
        [JsonIgnore]
        public VoxelType Type
        {
            get
            {
                return VoxelType.TypeList[_data.Types[Index]];
            }
            set
            {
                if (_data.Types[Index] == 0)
                {
                    if (value.ID != 0)
                        _data.SetFlag(Index, VoxelFlags.IsEmpty, false);
                }
                else
                {
                    if (value.ID == 0)
                        _data.SetFlag(Index, VoxelFlags.IsEmpty, true);
                }
                _data.Types[Index] = (byte)value.ID;
                _data.Health[Index] = (byte)value.StartingHealth;
                _data.Water[Index] = new WaterCell();
            }
        }

        [JsonIgnore]
        public bool IsEmpty
        {
            get { return _data.GetFlag(_index, VoxelFlags.IsEmpty); }
        }

        /// <summary>
        /// The name of the VoxelType found in this location.
        /// </summary>
        [JsonIgnore]
        public string TypeName
        {
            get { return Type.Name; }
        }

        /// <summary>
        /// The BoxPrimitive object for the current VoxelType in this location.
        /// </summary>
        [JsonIgnore]
        private BoxPrimitive Primitive
        {
            get { return VoxelLibrary.GetPrimitive(Type); }
        }

        /// <summary>
        /// Whether this Voxel has been explored yet or not.
        /// </summary>
        [JsonIgnore]
        public bool IsExplored
        {
            //get { return true; }
            get { return !GameSettings.Default.FogofWar || _data.GetFlag(Index, VoxelFlags.IsExplored); }
            set { _data.SetFlag(Index, VoxelFlags.IsExplored, value); }
        }

        [JsonIgnore]
        public bool IsDead
        {
            get { return Health <= 0; }
        }

        [JsonIgnore]
        public RampType RampType
        {
            get { return _data.RampTypes[Index]; }
            set { _data.RampTypes[Index] = value; }
        }

        [JsonIgnore]
        public float Health
        {
            get { return _data.Health[Index]; }
            set
            {
                if (Type.IsInvincible) return;
                _data.Health[Index] = (byte)(Math.Max(Math.Min(value, 255.0f), 0.0f));
            }
        }
        #endregion

        #region Constructors & Deserializers
        public Voxel()
        {
        }

        public Voxel(Voxel other)
        {
            ChangeVoxel(other);
        }

        public Voxel(Point3 gridPosition, VoxelChunk chunk)
        {
            Chunk = chunk;
            if (chunk != null)
                _chunkID = chunk.ID;
            GridPosition = new Vector3(gridPosition.X, gridPosition.Y, gridPosition.Z);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            WorldManager world = ((WorldManager)context.Context);
            if (world.ChunkManager.ChunkData.ChunkMap.ContainsKey(_chunkID))
            {
                ChangeVoxel(world.ChunkManager.ChunkData.ChunkMap[_chunkID], _gridpos);
            }
        }
        #endregion

        #region Quick voxel changing functions
        /// <summary>
        /// This function does the same as setting Chunk then GridPosition except avoids regenerating the quick compare
        /// more than once.  Only set generateQuickCompare to false if you intend the voxel to be a throwaway
        /// during a time sensitive loop.
        /// </summary>
        /// <param name="chunk">The VoxelChunk this voxel is found in.</param>
        /// <param name="gridPosition">The point in VoxelChunk space where the voxel is found.</param>
        /// <param name="generateQuickCompare">Whether to calculate the QuickCompare field.</param>
        public void ChangeVoxel(VoxelChunk chunk, Vector3 gridPosition, bool generateQuickCompare = true)
        {
            ChangeVoxel(chunk, new Point3(gridPosition), generateQuickCompare);
        }

        /// <summary>
        /// This function does the same as setting Chunk then GridPosition except avoids regenerating the quick compare
        /// more than once.  Only set generateQuickCompare to false if you intend the voxel to be a throwaway
        /// during a time sensitive loop.
        /// </summary>
        /// <param name="chunk">The VoxelChunk this voxel is found in.</param>
        /// <param name="gridPosition">The point in VoxelChunk space where the voxel is found.</param>
        /// <param name="generateQuickCompare">Whether to calculate the QuickCompare field.</param>
        public void ChangeVoxel(VoxelChunk chunk, Point3 gridPosition, bool generateQuickCompare = true)
        {
            Debug.Assert(chunk != null, "ChangeVoxel was passed a null chunk.");
            _chunk = chunk;
            _data = chunk.Data;
            _chunkID = _chunk.ID;
            _gridpos = gridPosition.ToVector3();
            _position = _gridpos + _chunk.Origin;
            _index = _data.IndexAt(gridPosition.X, gridPosition.Y, gridPosition.Z);
            if (generateQuickCompare) RegenerateQuickCompare();
            else _quickCompare = invalidCompareValue;
        }

        /// <summary>
        /// This function is the equivient of just setting the GridPosition except avoids regenerating the quick compare
        /// if requested.  Only set generateQuickCompare to false if you intend the voxel to be a throwaway
        /// during a time sensitive loop.
        /// </summary>
        /// <param name="gridPosition">The point in VoxelChunk space where the voxel is found.</param>
        /// <param name="generateQuickCompare">Whether to calculate the QuickCompare field.</param>
        public void ChangeVoxel(Vector3 gridPosition, bool generateQuickCompare = true)
        {
            _gridpos = gridPosition;
            _position = _gridpos + _chunk.Origin;
            _index = _data.IndexAt((int)gridPosition.X, (int)gridPosition.Y, (int)gridPosition.Z);
            if (generateQuickCompare) RegenerateQuickCompare();
            else _quickCompare = invalidCompareValue;
        }

        /// <summary>
        /// This function is the equivient of just setting the GridPosition except avoids regenerating the quick compare
        /// if requested.  Only set generateQuickCompare to false if you intend the voxel to be a throwaway
        /// during a time sensitive loop.
        /// </summary>
        /// <param name="x">The X position of the point  in VoxelChunk space.</param>
        /// <param name="y">The Y position of the point  in VoxelChunk space.</param>
        /// <param name="z">The Z position of the point  in VoxelChunk space.</param>
        /// <param name="generateQuickCompare">Whether to calculate the QuickCompare field.</param>
        public void ChangeVoxel(int x, int y, int z, bool generateQuickCompare = true)
        {
            _gridpos = new Vector3(x, y, z);
            _position = _gridpos + _chunk.Origin;
            _index = _data.IndexAt(x, y, z);
            if (generateQuickCompare) RegenerateQuickCompare();
            else _quickCompare = invalidCompareValue;
        }

        /// <summary>
        /// Directly copies the data from another Voxel object.
        /// </summary>
        /// <param name="v"></param>
        public void ChangeVoxel(Voxel v)
        {
            Debug.Assert(v != null, "ChangeVoxel was passed a null chunk.");
            _chunk = v._chunk;
            _data = v._data;
            _chunkID = _chunk.ID;
            _gridpos = v._gridpos;
            _position = v._position;
            _index = v._index;
            _quickCompare = v._quickCompare;
        }
        #endregion

        #region Comparison Functions and Object Overrides
        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != GetType()) return false;
            return Equals((Voxel)o);
        }

        private bool Equals(Voxel other)
        {
            return Equals(Chunk, other.Chunk) && Index == other.Index;
        }

        /// <summary>
        /// Compares two voxels using the QuickCompare value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSameAs(Voxel other)
        {
            if (_quickCompare == other._quickCompare) return true;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Chunk != null ? Chunk.GetHashCode() : 0) * 397) ^ GridPosition.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("ChunkID {0} Position {{{1}, {2}, {3}}}", _chunkID, _gridpos.X, _gridpos.Y, _gridpos.Z);
        }
        #endregion

        #region Helper Properties
        /// <summary>
        /// Whether this Voxel is visible at the current slice level.
        /// </summary>
        [JsonIgnore]
        public bool IsVisible
        {
            get { return GridPosition.Y <= Chunk.Manager.ChunkData.MaxViewingLevel; }
        }

        [JsonIgnore]
        public bool IsInterior
        {
            get { return _chunk.Data.GetFlag(_index, VoxelFlags.IsInterior); }
        }
        #endregion

        #region IBoundedObject implementations
        public uint GetID()
        {
            return (uint)GetHashCode();
        }

        public BoundingBox GetBoundingBox()
        {
            var pos = Position;
            return new BoundingBox(pos, pos + Vector3.One);
        }

        #endregion

        private void RegenerateQuickCompare()
        {
            // long build of the ulong.
            ulong q = 0;
            q |= (((ulong)_chunkID.X & 0xFFFF) << 48);
            q |= (((ulong)_chunkID.Y & 0xFFFF) << 32);
            q |= (((ulong)_chunkID.Z & 0xFFFF) << 16);
            q |= ((ulong)_index & 0xFFFF);
            _quickCompare = q;
            //quickCompare = (ulong) (((chunkID.X & 0xFFFF) << 48) | ((chunkID.Y & 0xFFFF) << 32) | ((chunkID.Y & 0xFFFF) << 16) | (index & 0xFFFF));
        }

        public bool IsTopEmpty()
        {
            if (GridPosition.Y >= Chunk.SizeY)
            {
                return true;
            }
            return
                _data.Types[
                    _data.IndexAt((int)GridPosition.X, (int)GridPosition.Y + 1, (int)GridPosition.Z)] == 0;
        }

        public bool IsBottomEmpty()
        {
            if (GridPosition.Y <= 0)
            {
                return true;
            }
            return
                _data.Types[
                    _data.IndexAt((int)GridPosition.X, (int)GridPosition.Y - 1, (int)GridPosition.Z)] == 0;
        }

        public Voxel GetVoxelAbove()
        {
            if (Chunk == null || GridPosition.Y >= Chunk.SizeY - 1)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int)GridPosition.X, (int)GridPosition.Y + 1, (int)GridPosition.Z);
        }

        public Voxel GetVoxelBelow()
        {
            if (GridPosition.Y <= 0)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int)GridPosition.X, (int)GridPosition.Y - 1, (int)GridPosition.Z);
        }

        public bool GetNeighborBySuccessor(Vector3 succ, ref Voxel neighbor, bool requireQuickCompare = true)
        {
            Debug.Assert(neighbor != null, "Null reference passed");
            Debug.Assert(_chunk != null, "Voxel has no valid chunk reference");

            Vector3 newPos = _gridpos + succ;
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
            }
            else
            {
                useChunk = _chunk;
            }
            neighbor.ChangeVoxel(useChunk, newPos, requireQuickCompare);
            return true;
        }

        public List<Body> Kill()
        {
            if (IsEmpty)
            {
                return null;
            }

            if (Chunk.Manager.World.ParticleManager != null)
            {
                Chunk.Manager.World.ParticleManager.Trigger(Type.ParticleType, Position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
                Chunk.Manager.World.ParticleManager.Trigger("puff", Position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            SoundManager.PlaySound(Type.ExplosionSound, Position);

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

            Destroy(true);
            return emittedResources;
        }

        /// <summary>
        /// Changes a voxel to an empty type but does not play a sound or throw items.
        /// </summary>
        /// <param name="justDestroy">Only removes the voxel.  Does not trigger a rebuild or any event handlers.</param>
        public void Destroy(bool justDestroy = false)
        {
            if (!justDestroy)
            {
                Chunk.Manager.KilledVoxels.Enqueue(this);
            }
            Type = VoxelLibrary.GetVoxelType(0);
        }

        /// <summary>
        /// Changes the voxel to the new type.
        /// </summary>
        /// <param name="typeID">Type identifier in numeric form.</param>
        /// <param name="justPlace">Only changes type.  Does not trigger a rebuild or any event handlers.</param>
        public void Place(short typeID, bool justPlace = false)
        {
            Place(VoxelLibrary.GetVoxelType(typeID), justPlace);
        }

        /// <summary>
        /// Changes the voxel to the new type.
        /// </summary>
        /// <param name="voxelType">Type identifier in string form.</param>
        /// <param name="justPlace">Only changes type.  Does not trigger a rebuild or any event handlers.</param>
        public void Place(string voxelType, bool justPlace = false)
        {
            Place(VoxelLibrary.GetVoxelType(voxelType), justPlace);
        }

        /// <summary>
        /// Changes the voxel to the new type.
        /// </summary>
        /// <param name="newType">Type identifier in VoxelType form.</param>
        /// <param name="justPlace">Only changes type.  Does not trigger a rebuild or any event handlers.</param>
        public void Place(VoxelType newType, bool justPlace = false)
        {
            Type = newType;
            if (!justPlace) Chunk.Manager.PlacedVoxels.Enqueue(this);
        }

        private TransitionTexture ComputeTransitionValue(Voxel[] manhattanNeighbors)
        {
            return Chunk.ComputeTransitionValue((int)GridPosition.X, (int)GridPosition.Y, (int)GridPosition.Z, manhattanNeighbors);
        }

        public BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(Voxel[] manhattanNeighbors)
        {
            if (!Type.HasTransitionTextures && Primitive != null) return Primitive.UVs;
            if (Primitive == null) return null;
            return Type.TransitionTextures[ComputeTransitionValue(manhattanNeighbors)];
        }
    }
}
