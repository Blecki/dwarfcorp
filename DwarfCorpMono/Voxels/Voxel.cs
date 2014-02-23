using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{
    public class VoxelType
    {
        public uint ID { get; set; }
        public string name { get; set; }
        public bool releasesResource { get; set; }
        public string resourceToRelease { get; set; }
        public float startingHealth { get; set; }
        public float probabilityOfRelease { get; set; }
        public bool canRamp { get; set; }
        public float rampSize { get; set; }
        public bool isBuildable { get; set; }
        public string particleType { get; set; }
        public string explosionSound { get; set; }
        public bool specialRampTextures { get; set; }
        public Dictionary<RampType, BoxPrimitive> RampPrimitives { get; set; }

        public  VoxelType()
        {
            ID = 0;
            name = "";
            releasesResource = false;
            resourceToRelease = "";
            startingHealth = 0.0f;
            probabilityOfRelease = 0.0f;
            canRamp = false;
            rampSize = 0.0f;
            isBuildable = false;
            particleType = "puff";
            explosionSound = "gravel";
            specialRampTextures = false;
            RampPrimitives = new Dictionary<RampType, BoxPrimitive>();
        }
    }

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

    // Intended to be a smaller memory footprint representation
    // that can be passed around.
    public class VoxelRef : IEquatable<VoxelRef>
    {
        public Point3 ChunkID { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Vector3 GridPosition { get; set; }
        public string TypeName { get; set; }
        public bool isValid;

        public override int GetHashCode()
        {
            return (int)WorldPosition.X ^ (int)WorldPosition.Y ^ (int)WorldPosition.Z;
        }

        public  bool Equals(VoxelRef other)
        {
            return other.ChunkID.Equals(ChunkID) 
                && (int)(GridPosition.X) == (int)(other.GridPosition.X) 
                && (int)(GridPosition.Y) == (int)(other.GridPosition.Y)
                && (int)(GridPosition.Z) == (int)(other.GridPosition.Z);
        }

        public override bool Equals(object obj)
        {
            if (obj is VoxelRef)
            {
                return Equals((VoxelRef)obj);
            }
            else
            {
                return false;
            }
        }

        public BoundingBox GetBoundingBox()
        {
            BoundingBox toReturn = new BoundingBox();
            toReturn.Min = WorldPosition;
            toReturn.Max = WorldPosition + new Vector3(1, 1, 1);
            return toReturn;
        }

        public Voxel CreateEmptyVoxel(ChunkManager manager)
        {
            Voxel emptyVox = new Voxel(WorldPosition, VoxelLibrary.emptyType, null, false);
            emptyVox.Chunk = manager.ChunkMap[ChunkID];

            return emptyVox;
        }

        public Voxel GetVoxel(ChunkManager manager, bool reconstruct)
        {
            if (!manager.ChunkMap.ContainsKey(ChunkID))
            {
                return null;
            }
            else if (manager.ChunkMap[ChunkID].IsCellValid((int)GridPosition.X, (int)GridPosition.Y, (int)GridPosition.Z))
            {
                Voxel vox = manager.ChunkMap[ChunkID].VoxelGrid[(int)GridPosition.X][ (int)GridPosition.Y][ (int)GridPosition.Z];
                if (!reconstruct)
                {
                    return vox;
                }
                else
                {
                    if (vox != null)
                    {
                        return vox;
                    }
                    else
                    {
                        return CreateEmptyVoxel(manager);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public WaterCell GetWater(ChunkManager manager)
        {
            if (!manager.ChunkMap.ContainsKey(ChunkID))
            {
                return null;
            }
            else if (manager.ChunkMap[ChunkID].IsCellValid((int)GridPosition.X, (int)GridPosition.Y, (int)GridPosition.Z))
            {
                return manager.ChunkMap[ChunkID].Water[(int)GridPosition.X][ (int)GridPosition.Y][ (int)GridPosition.Z];
            }
            else
            {
                return null;
            }
        }

        public byte GetWaterLevel(ChunkManager manager)
        {
            if (!manager.ChunkMap.ContainsKey(ChunkID))
            {
                return 0 ;
            }
            else if (manager.ChunkMap[ChunkID].IsCellValid((int)GridPosition.X, (int)GridPosition.Y, (int)GridPosition.Z))
            {
                return manager.ChunkMap[ChunkID].Water[(int)GridPosition.X][ (int)GridPosition.Y][ (int)GridPosition.Z].WaterLevel;
            }
            else
            {
                return 0;
            }
        }

        public void SetWaterLevel(ChunkManager manager, byte level)
        {
            if (!manager.ChunkMap.ContainsKey(ChunkID))
            {
                return;
            }
            else if (manager.ChunkMap[ChunkID].IsCellValid((int)GridPosition.X, (int)GridPosition.Y, (int)GridPosition.Z))
            {
               manager.ChunkMap[ChunkID].Water[(int)GridPosition.X][ (int)GridPosition.Y][ (int)GridPosition.Z].WaterLevel = level;
            }
            else
            {
                return;
            }
        }

        public void AddWaterLevel(ChunkManager manager, byte level)
        {
            if (!manager.ChunkMap.ContainsKey(ChunkID))
            {
                return;
            }
            else if (manager.ChunkMap[ChunkID].IsCellValid((int)GridPosition.X, (int)GridPosition.Y, (int)GridPosition.Z))
            {
                int amount = manager.ChunkMap[ChunkID].Water[(int)GridPosition.X][ (int)GridPosition.Y][ (int)GridPosition.Z].WaterLevel + level;
                manager.ChunkMap[ChunkID].Water[(int)GridPosition.X][ (int)GridPosition.Y][ (int)GridPosition.Z].WaterLevel = (byte)(Math.Min(amount, 255)); 
            }
            else
            {
                return;
            }
        }
    }

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



    public class Voxel : BoundedObject
    {
        public VoxelChunk Chunk { get { return m_chunk; } set { 
            
            GridPosition = Position - value.Origin;
            IsInterior = IsInteriorPoint(new Point3(GridPosition), value);

            m_chunk = value; } }
        public Vector3 Position { get; set; }
        public VoxelType Type { get; set; }
        public BoxPrimitive Primitive { get; set; }
        public bool IsVisible { get; set; }
        public bool InViewFrustrum { get; set; }
        public bool DrawWireFrame { get; set; }
        public Color[] VertexColors;
        //public byte[] AmbientColors { get; set; }
        //public byte[] SunColors { get; set; }
        //public byte[] DynamicColors { get; set; }
        public Vector3 GridPosition { get; set; }
        public bool RecalculateLighting { get; set; }
        public static List<VoxelVertex> VoxelVertexList { get; set; }
        private static bool m_staticsCreated = false;
        private VoxelChunk m_chunk = null;
        private bool m_dead = false;
        public RampType RampType = RampType.None;
        public bool IsInterior = false;

        public uint GetID() { return (uint)GetHashCode(); }

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
            if (!m_staticsCreated)
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

                if (m_health <= 0.0f)
                {
                    Kill();
                }
            }
        }
        private float m_health = 10.0f;

        public void Kill()
        {
            if (m_dead)
            {
                return;
            }

            if (PlayState.ParticleManager != null)
            {
                PlayState.ParticleManager.Trigger(Type.particleType, Position + new Vector3(0.5f, 0.5f, 0.5f), new Color(255, 255, 0), 20);
                PlayState.ParticleManager.Trigger("puff", Position + new Vector3(0.5f, 0.5f, 0.5f), new Color(255, 255, 0), 20);
            }

            if (PlayState.master != null)
            {
                PlayState.master.OnVoxelDestroyed(this);
            }

            SoundManager.PlaySound(Type.explosionSound, Position);
            if (Type.releasesResource)
            {
                float randFloat = (float)PlayState.random.NextDouble();

                if (randFloat < Type.probabilityOfRelease)
                {
                    EntityFactory.GenerateComponent(Type.resourceToRelease, Position + new Vector3(0.5f, 0.5f, 0.5f), Chunk.Manager.Components, Chunk.Manager.Content, Chunk.Manager.Graphics, Chunk.Manager, null, null);
                }
            }

            Chunk.ShouldRebuild = true;
            Chunk.ShouldRecalculateLighting = true;
            Chunk.ReconstructRamps = true;
            Chunk.NotifyChangedComponents();

            if (!IsInterior)
            {
                List<VoxelRef> neighbors = Chunk.GetNeighborsEuclidean((int)this.GridPosition.X, (int)this.GridPosition.Y, (int)this.GridPosition.Z);
                foreach (VoxelRef v in neighbors)
                {
                    Voxel vox = v.GetVoxel(Chunk.Manager, true);
                    if (vox != null)
                    {
                        vox.RecalculateLighting = true;
                        vox.Chunk.ShouldRebuild = true;
                        vox.Chunk.ShouldRecalculateLighting = true;
                        vox.Chunk.ReconstructRamps = true;
                    }

                }
            }

            Chunk.VoxelGrid[(int)GridPosition.X][ (int)GridPosition.Y][ (int)GridPosition.Z] = null;

            m_dead = true;
        }

        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(Position, 1);
        }

        public BoundingBox GetBoundingBox()
        {
            BoundingBox pBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
            if (Primitive != null)
            {
                pBox = Primitive.boundingBox;
            }
            else
            {
                pBox = new BoundingBox(Vector3.Zero, new Vector3(1, 1, 1));
            }
            return new BoundingBox(pBox.Min + Position, pBox.Max + Position);
           
        }


        Color blankColor = new Color(0, 255, 0);
        public Voxel(Vector3 position, VoxelType voxelType, BoxPrimitive primitive, bool isVisible)
        {
            UpdateStatics();
            Position = position;


            Type = voxelType;
            Primitive = primitive;
            IsVisible = isVisible;
            InViewFrustrum = false;
            DrawWireFrame = false;
            Health = 100.0f;

            //AmbientColors = new byte[8];
            //SunColors = new byte[8];
            //DynamicColors = new byte[8];
            RecalculateLighting = true;

            VertexColors = new Color[8];

            for (int i = 0; i < 8; i++)
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
            toReturn.TypeName = Type.name;
            toReturn.isValid = true;

            return toReturn;
        }


        public void Render(GraphicsDevice device, Effect effect, Matrix worldMatrix)
        {
            if (!IsVisible)
            {
                return;
            }

            worldMatrix.Translation += Position;
            effect.Parameters["xWorld"].SetValue(worldMatrix);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
            }

            RasterizerState origState = device.RasterizerState;

            if (!DrawWireFrame)
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
