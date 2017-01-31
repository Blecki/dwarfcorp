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
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;


namespace DwarfCorp
{

    /// <summary>
    /// Specifies the location of a vertex on a voxel.
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
        protected bool Equals(Voxel other)
        {
            return Equals(Chunk, other.Chunk) && Index == other.Index;
        }

        public bool IsSameAs(Voxel other)
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

        private Point3 chunkID = new Point3(0, 0, 0);
        public Point3 ChunkID
        {
            get { return chunkID; }
            set { chunkID = value; RegenerateQuickCompare(); }
        }

        [NonSerialized]
        private ulong quickCompare;
        public ulong QuickCompare
        {
            get { return quickCompare; }
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

        public Voxel(Voxel other)
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

        public Voxel GetVoxelAbove()
        {
            if (Chunk == null || GridPosition.Y >= Chunk.SizeY - 1)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int) GridPosition.X, (int) GridPosition.Y + 1, (int) GridPosition.Z);
        }

        public Voxel GetVoxelBelow()
        {
            if (GridPosition.Y <=0)
            {
                return null;
            }
            return
                Chunk.MakeVoxel((int)GridPosition.X, (int)GridPosition.Y - 1, (int)GridPosition.Z);
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
            return Equals((Voxel) o);
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

            if(WorldManager.ParticleManager != null)
            {
                WorldManager.ParticleManager.Trigger(Type.ParticleType, Position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
                WorldManager.ParticleManager.Trigger("puff", Position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            if(WorldManager.Master != null)
            {
                WorldManager.Master.Faction.OnVoxelDestroyed(this);
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

        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(Position, 1);
        }

        public BoundingBox GetBoundingBox()
        {
            var pos = Position;
            return new BoundingBox(pos, pos + Vector3.One);
        }

        public Voxel()
        {
            
        }

        public Voxel(Point3 gridPosition, VoxelChunk chunk)
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
            if (WorldManager.ChunkManager.ChunkData.ChunkMap.ContainsKey(chunkID))
            {
                Chunk = WorldManager.ChunkManager.ChunkData.ChunkMap[chunkID];
                index = Chunk.Data.IndexAt((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z);
                RegenerateQuickCompare();
            }
        }

        public TransitionTexture ComputeTransitionValue(Voxel[] manhattanNeighbors)
        {
            return Chunk.ComputeTransitionValue((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z, manhattanNeighbors);
        }

        public BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(Voxel[] manhattanNeighbors)
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

        public bool GetNeighbor(Vector3 dir, ref Voxel vox)
        {
            return Chunk.Manager.ChunkData.GetVoxel(Position + dir, ref vox);
        }

        public override string ToString()
        {
            return String.Format("Voxel {{{0}, {1}, {2}}}", gridpos.X, gridpos.Y, gridpos.Z);
        }
    }

}