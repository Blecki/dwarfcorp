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
            return Equals(Chunk, other.Chunk) && GridPosition.Equals(other.GridPosition);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                return ((Chunk != null ? Chunk.GetHashCode() : 0)*397) ^ GridPosition.GetHashCode();
            }
        }

        [JsonIgnore]
        public VoxelChunk Chunk { get; set; }

        [JsonIgnore]
        public Vector3 Position 
        {
            get
            {
                return GridPosition + Chunk.Origin;
            }
        }

        [JsonIgnore]
        public byte WaterLevel
        {
            get { return Chunk.Data.Water[Index].WaterLevel; }
            set { Chunk.Data.Water[Index].WaterLevel = value; }
        }

        [JsonIgnore]
        public VoxelType Type
        {
            get
            {
                return VoxelType.TypeList[Chunk.Data.Types[Index]];
            }
            set { Chunk.Data.Types[Index] = (byte) value.ID; }
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
            get { return Chunk.Data.IsVisible[Index]; }
            set { Chunk.Data.IsVisible[Index] = value; }
        }

        private Vector3 gridpos = Vector3.Zero;

        public Vector3 GridPosition
        {
            get { return gridpos; }
            set 
            { 
                gridpos = value;

                if(Chunk != null)
                    index = Chunk.Data.IndexAt((int)gridpos.X, (int)gridpos.Y, (int)gridpos.Z); 
            }
        }

        [JsonIgnore]
        public bool RecalculateLighting 
        {
            get { return Chunk.Data.RecalculateLighting[Index]; }
            set { Chunk.Data.RecalculateLighting[Index] = value;  }
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
            set { chunkID = value; }
        }

        [JsonIgnore]
        public float Health
        {
            get { return (float) Chunk.Data.Health[Index]; }
            set
            {
                Chunk.Data.Health[Index] = (byte)value;

                if (value <= 0.0f)
                {
                    Kill();
                }
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

        public bool IsEmpty
        {
            get { return Type.ID == 0; }
        }

        public int SunColor { get { return Chunk.Data.SunColors[Index]; }}

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



        public void Kill()
        {
            if (IsEmpty)
            {
                return;
            }

            if(PlayState.ParticleManager != null)
            {
                PlayState.ParticleManager.Trigger(Type.ParticleType, Position + new Vector3(0.5f, 0.5f, 0.5f), new Color(255, 255, 0), 20);
                PlayState.ParticleManager.Trigger("puff", Position + new Vector3(0.5f, 0.5f, 0.5f), new Color(255, 255, 0), 20);
            }

            if(PlayState.Master != null)
            {
                PlayState.Master.Faction.OnVoxelDestroyed(this);
            }

            SoundManager.PlaySound(Type.ExplosionSound, Position);
            if (Type.ReleasesResource)
            {
                float randFloat = MathFunctions.Rand();

                if (randFloat < Type.ProbabilityOfRelease)
                {
                    EntityFactory.GenerateResource(Type.ResourceToRelease, Position + new Vector3(0.5f, 0.5f, 0.5f));
                }
            }
            Chunk.ShouldRebuild = true;
            Chunk.ShouldRecalculateLighting = true;
            Chunk.ReconstructRamps = true;
            Chunk.NotifyDestroyed(new Point3(GridPosition));
            Chunk.NotifyChangedComponents();


            if(!IsInterior)
            {
                List<Voxel> neighbors = Chunk.GetNeighborsEuclidean((int) this.GridPosition.X, (int) this.GridPosition.Y, (int) this.GridPosition.Z);
                foreach (Voxel vox in neighbors)
                {
                    if(vox == null)
                    {
                        continue;
                    }

                    vox.RecalculateLighting = true;
                    vox.Chunk.ShouldRebuild = true;
                    vox.Chunk.ShouldRecalculateLighting = true;
                    vox.Chunk.ReconstructRamps = true;
                }
            }

            Chunk.Data.Types[Index] = 0; 
        }

        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(Position, 1);
        }

        public BoundingBox GetBoundingBox()
        {
            BoundingBox pBox = Primitive != null ? Primitive.BoundingBox : new BoundingBox(Vector3.Zero, new Vector3(1, 1, 1));
            return new BoundingBox(pBox.Min + Position, pBox.Max + Position);
        }

        public Voxel()
        {
            
        }

        public Voxel(Point3 gridPosition, VoxelChunk chunk)
        {
            UpdateStatics();
            Chunk = chunk;
            chunkID = chunk.ID;
            GridPosition = new Vector3(gridPosition.X, gridPosition.Y, gridPosition.Z);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Chunk = PlayState.ChunkManager.ChunkData.ChunkMap[chunkID];
            index = Chunk.Data.IndexAt((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z);
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

        public WaterCell GetWater()
        {
            return Chunk.Data.Water[Index];
        }
    }

}