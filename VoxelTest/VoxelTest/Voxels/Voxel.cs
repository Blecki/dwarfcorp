using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Voxel : IBoundedObject
    {
        [JsonIgnore]
        public VoxelChunk Chunk
        {
            get { return chunk; }
            set
            {
                GridPosition = Position - value.Origin;
                IsInterior = IsInteriorPoint(new Point3(GridPosition), value);

                chunk = value;
            }
        }

        public Vector3 Position { get; set; }
        public VoxelType Type { get; set; }
        public BoxPrimitive Primitive { get; set; }
        public bool IsVisible { get; set; }
        
        public Color[] VertexColors;

        public Vector3 GridPosition { get; set; }
        public bool RecalculateLighting { get; set; }
        public static List<VoxelVertex> VoxelVertexList { get; set; }
        private static bool staticsCreated;
        private VoxelChunk chunk;
        private bool dead;
        public RampType RampType = RampType.None;
        public bool IsInterior = false;
        private static readonly Color BlankColor = new Color(0, 255, 0);

        public float Health
        {
            get { return health; }
            set
            {
                health = value;

                if (health <= 0.0f)
                {
                    Kill();
                }
            }
        }

        private float health = 10.0f;

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
            return Chunk.VoxelGrid[(int)GridPosition.X][(int)GridPosition.Y + 1][(int)GridPosition.Z] == null;
        }

        public bool IsBottomEmpty()
        {
            if (GridPosition.Y <= 0)
            {
                return true;
            }
            return Chunk.VoxelGrid[(int)GridPosition.X][(int)GridPosition.Y - 1][(int)GridPosition.Z] == null;
        }

        public static bool IsInteriorPoint(Point3 gridPosition, VoxelChunk chunk)
        {
            return gridPosition.X != 0 &&
                   gridPosition.Y != 0 &&
                   gridPosition.Z != 0 &&
                   gridPosition.X != chunk.SizeX - 1 &&
                   gridPosition.Y != chunk.SizeY - 1 &&
                   gridPosition.Z != chunk.SizeZ - 1;
        }

        public static bool HasFlag(RampType ramp, RampType flag)
        {
            return (ramp & flag) == flag;
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
            if(dead || Chunk == null)
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
            if(Type.ReleasesResource)
            {
                float randFloat = (float) PlayState.Random.NextDouble();

                if(randFloat < Type.ProbabilityOfRelease)
                {
                    EntityFactory.GenerateComponent(Type.ResourceToRelease, Position + new Vector3(0.5f, 0.5f, 0.5f), Chunk.Manager.Components, Chunk.Manager.Content, Chunk.Manager.Graphics, Chunk.Manager, null, null);
                }
            }

            Chunk.ShouldRebuild = true;
            Chunk.ShouldRecalculateLighting = true;
            Chunk.ReconstructRamps = true;
            Chunk.NotifyChangedComponents();

            if(!IsInterior)
            {
                List<VoxelRef> neighbors = Chunk.GetNeighborsEuclidean((int) this.GridPosition.X, (int) this.GridPosition.Y, (int) this.GridPosition.Z);
                foreach(VoxelRef v in neighbors)
                {
                    Voxel vox = v.GetVoxel(true);
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

            Chunk.VoxelGrid[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z] = null;

            dead = true;
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


        public Voxel(Vector3 position, VoxelType voxelType, BoxPrimitive primitive, bool isVisible)
        {
            UpdateStatics();
            Position = position;


            Type = voxelType;
            Primitive = primitive;
            IsVisible = isVisible;
            Health = voxelType.StartingHealth;
            RecalculateLighting = true;

            VertexColors = new Color[8];

            for(int i = 0; i < 8; i++)
            {
                VertexColors[i] = BlankColor;
            }
        }


        public VoxelRef GetReference()
        {
            VoxelRef toReturn = new VoxelRef
            {
                ChunkID = Chunk.ID,
                GridPosition = GridPosition,
                WorldPosition = Position,
                TypeName = Type.Name,
                IsValid = true
            };

            return toReturn;
        }

        public TransitionTexture ComputeTransitionValue()
        {
            return Chunk.ComputeTransitionValue((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z);
        }

        public BoxPrimitive.BoxTextureCoords ComputeTransitionTexture()
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
                return Type.TransitionTextures[ComputeTransitionValue()];
            }
        }
    }

}