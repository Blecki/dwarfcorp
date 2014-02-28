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


    /// <summary>
    /// An atomic cube in the world which represents a bit of terrain. 
    /// </summary>
    public class Voxel : IBoundedObject
    {
        [JsonIgnore]
        public VoxelChunk Chunk
        {
            get { return m_chunk; }
            set
            {
                GridPosition = Position - value.Origin;
                IsInterior = IsInteriorPoint(new Point3(GridPosition), value);

                m_chunk = value;
            }
        }

        public Vector3 Position { get; set; }
        public VoxelType Type { get; set; }
        public BoxPrimitive Primitive { get; set; }
        public bool IsVisible { get; set; }
        public bool InViewFrustrum { get; set; }
        public bool DrawWireFrame { get; set; }
        
        public Color[] VertexColors;

        public Vector3 GridPosition { get; set; }
        public bool RecalculateLighting { get; set; }
        public static List<VoxelVertex> VoxelVertexList { get; set; }
        private static bool m_staticsCreated = false;
        private VoxelChunk m_chunk = null;
        private bool m_dead = false;
        public RampType RampType = RampType.None;
        public bool IsInterior = false;

        public uint GetID()
        {
            return (uint) GetHashCode();
        }

        public static bool IsInteriorPoint(Point3 GridPosition, VoxelChunk chunk)
        {
            return GridPosition.X != 0 &&
                   GridPosition.Y != 0 &&
                   GridPosition.Z != 0 &&
                   GridPosition.X != chunk.SizeX - 1 &&
                   GridPosition.Y != chunk.SizeY - 1 &&
                   GridPosition.Z != chunk.SizeZ - 1;
        }

        public static bool HasFlag(RampType ramp, RampType flag)
        {
            return (ramp & flag) == flag;
        }


        public void UpdateStatics()
        {
            if(!m_staticsCreated)
            {
                VoxelVertexList = new List<VoxelVertex>();
                VoxelVertexList.Add(VoxelVertex.BackBottomLeft);
                VoxelVertexList.Add(VoxelVertex.BackBottomRight);
                VoxelVertexList.Add(VoxelVertex.BackTopLeft);
                VoxelVertexList.Add(VoxelVertex.BackTopRight);
                VoxelVertexList.Add(VoxelVertex.FrontBottomRight);
                VoxelVertexList.Add(VoxelVertex.FrontBottomLeft);
                VoxelVertexList.Add(VoxelVertex.FrontTopRight);
                VoxelVertexList.Add(VoxelVertex.FrontTopLeft);
                m_staticsCreated = true;
            }
        }

        public float Health
        {
            get { return m_health; }
            set
            {
                m_health = value;

                if(m_health <= 0.0f)
                {
                    Kill();
                }
            }
        }

        private float m_health = 10.0f;

        public void Kill()
        {
            if(m_dead || Chunk == null)
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
                    if(vox != null)
                    {
                        vox.RecalculateLighting = true;
                        vox.Chunk.ShouldRebuild = true;
                        vox.Chunk.ShouldRecalculateLighting = true;
                        vox.Chunk.ReconstructRamps = true;
                    }
                }
            }

            Chunk.VoxelGrid[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z] = null;

            m_dead = true;
        }

        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(Position, 1);
        }

        public BoundingBox GetBoundingBox()
        {
            BoundingBox pBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
            if(Primitive != null)
            {
                pBox = Primitive.BoundingBox;
            }
            else
            {
                pBox = new BoundingBox(Vector3.Zero, new Vector3(1, 1, 1));
            }
            return new BoundingBox(pBox.Min + Position, pBox.Max + Position);
        }


        private Color blankColor = new Color(0, 255, 0);

        public Voxel(Vector3 position, VoxelType voxelType, BoxPrimitive primitive, bool isVisible)
        {
            UpdateStatics();
            Position = position;


            Type = voxelType;
            Primitive = primitive;
            IsVisible = isVisible;
            InViewFrustrum = false;
            DrawWireFrame = false;
            Health = voxelType.StartingHealth;

            //AmbientColors = new byte[8];
            //SunColors = new byte[8];
            //DynamicColors = new byte[8];
            RecalculateLighting = true;

            VertexColors = new Color[8];

            for(int i = 0; i < 8; i++)
            {
                VertexColors[i] = blankColor;
            }


            /*
            for(int i = 0; i < 8; i++)
            {
                AmbientColors[i] = 255;
            }

            for (int i = 0; i < 8; i++)
            {
                SunColors[i] = 255;
            }

            for (int i = 0; i < 8; i++)
            {
                DynamicColors[i] = 0;
            }
             */
        }


        public VoxelRef GetReference()
        {
            VoxelRef toReturn = new VoxelRef();

            toReturn.ChunkID = Chunk.ID;
            toReturn.GridPosition = GridPosition;
            toReturn.WorldPosition = Position;
            toReturn.TypeName = Type.Name;
            toReturn.IsValid = true;

            return toReturn;
        }


        public void Render(GraphicsDevice device, Effect effect, Matrix worldMatrix)
        {
            if(!IsVisible)
            {
                return;
            }

            worldMatrix.Translation += Position;
            effect.Parameters["xWorld"].SetValue(worldMatrix);

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
            }

            RasterizerState origState = device.RasterizerState;

            if(!DrawWireFrame)
            {
                Primitive.Render(device);
            }
            else
            {
                Primitive.RenderWireframe(device);
                DrawWireFrame = false;
            }

            worldMatrix.Translation -= Position;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
        }
    }

}