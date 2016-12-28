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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Specifies the location of a vertex on a voxel.
    /// </summary>
    public enum VoxelVertex
    {
        FrontTopLeft,
        FrontTopRight,
        FrontBottomLeft,
        FrontBottomRight,
        BackTopLeft,
        BackTopRight,
        BackBottomLeft,
        BackBottomRight,
    }

    /// <summary>
    ///     Specifies how a voxel is to be sloped.
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


    /// <summary>
    ///     Determines a transition texture type. Each phrase
    ///     (front, left, back, right) defines whether or not a tile of the same type is
    ///     on the given face
    /// </summary>
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
    /// Voxels are actually just thin references around the data in the chunk, which is stored
    /// as a flat array in the chunk. This reduces memory usage.   
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Voxel : IBoundedObject
    {
        /// <summary>
        /// True if static data for voxels has been initialized.
        /// </summary>
        private static bool staticsCreated;
        /// <summary>
        /// The lightmap color of unexplored voxels.
        /// </summary>
        private static readonly Color BlankColor = new Color(0, 255, 0);

        /// <summary>
        /// The chunk associated with the voxel.
        /// </summary>
        [JsonIgnore] private VoxelChunk _chunk;
        /// <summary>
        /// The chunk identifier (x, y, z grid position)
        /// </summary>
        private Point3 chunkID = new Point3(0, 0, 0);
        /// <summary>
        /// The grid position of the voxel within the chunk.
        /// </summary>
        private Vector3 gridpos = Vector3.Zero;
        /// <summary>
        /// The linear index of the voxel within the chunk.
        /// </summary>
        private int index;

        public Voxel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel"/> class.
        /// </summary>
        /// <param name="gridPosition">The grid position of the voxel within the chunk.</param>
        /// <param name="chunk">The chunk.</param>
        public Voxel(Point3 gridPosition, VoxelChunk chunk)
        {
            UpdateStatics();
            Chunk = chunk;
            chunkID = chunk.ID;
            GridPosition = new Vector3(gridPosition.X, gridPosition.Y, gridPosition.Z);
        }


        /// <summary>
        /// Gets or sets the chunk.
        /// </summary>
        /// <value>
        /// The chunk.
        /// </value>
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

        /// <summary>
        /// Gets the world position of the leastmost corner of the voxel.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        [JsonIgnore]
        public Vector3 Position
        {
            get { return GridPosition + Chunk.Origin; }
        }

        /// <summary>
        /// Gets or sets the type of the voxel.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonIgnore]
        public VoxelType Type
        {
            get { return VoxelType.TypeList[Chunk.Data.Types[Index]]; }
            set
            {
                Chunk.Data.Types[Index] = (byte) value.ID;
                Chunk.Data.Health[Index] = (byte) value.StartingHealth;
            }
        }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <value>
        /// The name of the type.
        /// </value>
        [JsonIgnore]
        public string TypeName
        {
            get { return Type.Name; }
        }

        /// <summary>
        /// Gets the linear index of the voxel within the chunk.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        [JsonIgnore]
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// Gets the primitive (vertex buffer) associated with the voxel type.
        /// </summary>
        /// <value>
        /// The primitive.
        /// </value>
        [JsonIgnore]
        public BoxPrimitive Primitive
        {
            get { return VoxelLibrary.GetPrimitive(Type); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is visible.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is visible; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsVisible
        {
            get { return GridPosition.Y <= Chunk.Manager.ChunkData.MaxViewingLevel; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is explored.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is explored; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsExplored
        {
            //get { return true; }
            get { return !GameSettings.Default.FogofWar || Chunk.Data.IsExplored[Index]; }
            set { Chunk.Data.IsExplored[Index] = value; }
        }

        /// <summary>
        /// Gets or sets the grid position of the voxel within the chunk.
        /// </summary>
        /// <value>
        /// The grid position.
        /// </value>
        public Vector3 GridPosition
        {
            get { return gridpos; }
            set
            {
                gridpos = value;

                if (Chunk != null)
                    index = Chunk.Data.IndexAt((int) gridpos.X, (int) gridpos.Y, (int) gridpos.Z);
            }
        }

        /// <summary>
        /// Gets or sets the list of all the vertices in a voxel.
        /// </summary>
        /// <value>
        /// The voxel vertex list.
        /// </value>
        [JsonIgnore]
        public static List<VoxelVertex> VoxelVertexList { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is dead (health less than zero)"/>
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is dead; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsDead
        {
            get { return Health <= 0; }
        }

        /// <summary>
        /// Gets or sets the type of the ramp for this voxel.
        /// </summary>
        /// <value>
        /// The type of the ramp.
        /// </value>
        [JsonIgnore]
        public RampType RampType
        {
            get { return Chunk.Data.RampTypes[Index]; }
            set { Chunk.Data.RampTypes[Index] = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is interior (that is, it is not on the border of a chunk).
        /// This is used for optimizations relating to updating voxels along the border.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is interior; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsInterior
        {
            get { return Chunk.IsInterior((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z); }
        }

        /// <summary>
        /// Gets or sets the chunk identifier (x, y, z position)
        /// </summary>
        /// <value>
        /// The chunk identifier.
        /// </value>
        public Point3 ChunkID
        {
            get { return chunkID; }
            set { chunkID = value; }
        }

        /// <summary>
        /// Gets or sets the health of the voxel in the chunk.
        /// </summary>
        /// <value>
        /// The health.
        /// </value>
        [JsonIgnore]
        public float Health
        {
            get { return Chunk.Data.Health[Index]; }
            set
            {
                if (Type.IsInvincible) return;
                Chunk.Data.Health[Index] = (byte) (Math.Max(Math.Min(value, 255.0f), 0.0f));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsEmpty
        {
            get { return Type.ID == 0; }
        }

        /// <summary>
        /// Gets the intensity of the sunlight passing through this voxel.
        /// </summary>
        /// <value>
        /// The color of the sun.
        /// </value>
        [JsonIgnore]
        public byte SunColor
        {
            get { return Chunk.Data.SunColors[Index]; }
        }

        /// <summary>
        /// Gets or sets the water associated with this voxel.
        /// </summary>
        /// <value>
        /// The water.
        /// </value>
        [JsonIgnore]
        public WaterCell Water
        {
            get { return Chunk.Data.Water[Index]; }
            set { Chunk.Data.Water[Index] = value; }
        }

        /// <summary>
        /// Gets or sets the height of the water associated with this voxel.
        /// </summary>
        /// <value>
        /// The water level.
        /// </value>
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

        /// <summary>
        /// Gets the global identifier of this voxel.
        /// </summary>
        /// <returns>A global identifier.</returns>
        public uint GetID()
        {
            return (uint) GetHashCode();
        }

        /// <summary>
        /// Gets the world bounding box of this voxel.
        /// </summary>
        /// <returns>A bounding box in the world containing this voxel.</returns>
        public BoundingBox GetBoundingBox()
        {
            Vector3 pos = Position;
            return new BoundingBox(pos, pos + Vector3.One);
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="other">The other voxel.</param>
        /// <returns>true if this voxel is the same as the other, false otherwise.</returns>
        protected bool Equals(Voxel other)
        {
            return Equals(Chunk, other.Chunk) && Index == other.Index;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Chunk != null ? Chunk.GetHashCode() : 0)*397) ^ GridPosition.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether there is an empty voxel on top of this one.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is top empty]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTopEmpty()
        {
            if (GridPosition.Y >= Chunk.SizeY)
            {
                return true;
            }
            return
                Chunk.Data.Types[
                    Chunk.Data.IndexAt((int) GridPosition.X, (int) GridPosition.Y + 1, (int) GridPosition.Z)] == 0;
        }

        /// <summary>
        /// Gets the voxel above this one.
        /// </summary>
        /// <returns>The voxel above this one if it exists, or null otherwise.</returns>
        public Voxel GetVoxelAbove()
        {
            if (Chunk == null || GridPosition.Y >= Chunk.SizeY - 1)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int) GridPosition.X, (int) GridPosition.Y + 1, (int) GridPosition.Z);
        }

        /// <summary>
        /// Gets the voxel below this one.
        /// </summary>
        /// <returns>The voxel below this one if it exists, or null otherwise.</returns>
        public Voxel GetVoxelBelow()
        {
            if (GridPosition.Y <= 0)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int) GridPosition.X, (int) GridPosition.Y - 1, (int) GridPosition.Z);
        }

        /// <summary>
        /// Determines whether there is an empty voxel below this one.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is bottom empty]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBottomEmpty()
        {
            if (GridPosition.Y <= 0)
            {
                return true;
            }
            return
                Chunk.Data.Types[
                    Chunk.Data.IndexAt((int) GridPosition.X, (int) GridPosition.Y - 1, (int) GridPosition.Z)] == 0;
        }

        /// <summary>
        /// Determines whether the specified grid position is inside a chunk.
        /// </summary>
        /// <param name="gridPosition">The grid position.</param>
        /// <param name="chunk">The chunk.</param>
        /// <returns>
        ///   <c>true</c> if [is interior point] [the specified grid position]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInteriorPoint(Point3 gridPosition, VoxelChunk chunk)
        {
            return chunk.IsInterior(gridPosition.X, gridPosition.Y, gridPosition.Z);
        }

        /// <summary>
        /// Determines whether the specified ramp type has flag. (for determining which direction
        /// the voxel is sloping in)
        /// </summary>
        /// <param name="ramp">The ramp.</param>
        /// <param name="flag">The flag.</param>
        /// <returns>
        ///   <c>true</c> if the specified ramp has flag; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasFlag(RampType ramp, RampType flag)
        {
            return (ramp & flag) == flag;
        }

        /// <summary>
        /// Resets the voxel so that it corresponds to a different one inside another chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="gridPosition">The grid position of the voxel within the chunk.</param>
        public void SetFromData(VoxelChunk chunk, Vector3 gridPosition)
        {
            Chunk = chunk;
            GridPosition = gridPosition;
            index = Chunk.Data.IndexAt((int) gridPosition.X, (int) gridPosition.Y, (int) gridPosition.Z);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="o">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != GetType()) return false;
            return Equals((Voxel) o);
        }

        /// <summary>
        /// Updates the static data for all voxels.
        /// </summary>
        public void UpdateStatics()
        {
            if (staticsCreated)
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


        /// <summary>
        /// Kills this instance, triggering all events associated with its death.
        /// </summary>
        /// <returns>A list of bodies affected by the death of this voxel.</returns>
        public List<Body> Kill()
        {
            if (IsEmpty)
            {
                return null;
            }

            if (PlayState.ParticleManager != null)
            {
                PlayState.ParticleManager.Trigger(Type.ParticleType, Position + new Vector3(0.5f, 0.5f, 0.5f),
                    Color.White, 20);
                PlayState.ParticleManager.Trigger("puff", Position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            if (PlayState.Master != null)
            {
                PlayState.Master.Faction.OnVoxelDestroyed(this);
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

            Chunk.Manager.KilledVoxels.Add(this);
            Chunk.Data.Types[Index] = 0;
            return emittedResources;
        }

        /// <summary>
        /// Gets the bounding sphere of this voxel.
        /// </summary>
        /// <returns>A sphere around this voxel.</returns>
        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(Position + Vector3.One * 0.5f, 1);
        }

        /// <summary>
        /// Called when the voxel is deserialized from a JSON file.
        /// </summary>
        /// <param name="context">The context.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (PlayState.ChunkManager.ChunkData.ChunkMap.ContainsKey(chunkID))
            {
                Chunk = PlayState.ChunkManager.ChunkData.ChunkMap[chunkID];
                index = Chunk.Data.IndexAt((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z);
            }
        }

        /// <summary>
        /// Computes a texture for the top of the voxel given its neighbors.
        /// </summary>
        /// <param name="manhattanNeighbors">The manhattan neighbors of the voxel in x and z</param>
        /// <returns>The texture to draw on top of this voxel.</returns>
        public TransitionTexture ComputeTransitionValue(Voxel[] manhattanNeighbors)
        {
            return Chunk.ComputeTransitionValue((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z,
                manhattanNeighbors);
        }

        /// <summary>
        /// Computes the transition texture for the top of the voxel given its neighbors.
        /// </summary>
        /// <param name="manhattanNeighbors">The manhattan neighbors of the voxel in x and z</param>
        /// <returns>Texture coordinates of the voxel.</returns>
        public BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(Voxel[] manhattanNeighbors)
        {
            if (!Type.HasTransitionTextures && Primitive != null)
            {
                return Primitive.UVs;
            }
            if (Primitive == null)
            {
                return null;
            }
            return Type.TransitionTextures[ComputeTransitionValue(manhattanNeighbors)];
        }

        /// <summary>
        /// Gets the neighbor of the voxel in the specified direction.
        /// </summary>
        /// <param name="dir">The direction to search in (normalized)</param>
        /// <param name="vox">The voxel which is a neighbor of this one in that direction.</param>
        /// <returns>True if such a neighbor exists, or false otherwise.</returns>
        public bool GetNeighbor(Vector3 dir, ref Voxel vox)
        {
            return Chunk.Manager.ChunkData.GetVoxel(Position + dir + Vector3.One * 0.5f, ref vox);
        }
    }
}