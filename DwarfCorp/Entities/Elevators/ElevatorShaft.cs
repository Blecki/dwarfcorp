using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Elevators
{
    public class ElevatorShaft : GameComponent
    {
        [EntityFactory("Elevator Shaft")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var resources = Data.GetData<List<ResourceAmount>>("Resources", null);

            if (resources == null)
                resources = new List<ResourceAmount>() { new ResourceAmount("Wood") };

            return new ElevatorShaft(Manager, Position, resources);
        }

        public UInt32 TrackAbove = ComponentManager.InvalidID;
        public UInt32 TrackBelow = ComponentManager.InvalidID;

        [JsonIgnore]
        public ElevatorStack Shaft = new ElevatorStack();

        [JsonIgnore]
        public bool NeedsShaftUpdate = false;

        [JsonIgnore]
        public bool NeedsConnectionUpdate = true;
        
        private RawPrimitive Primitive;
        private Color VertexColor = Color.White;
        private Color LightRamp = Color.White;

        private SpriteSheet Sheet;
        private Vector4[] Bounds = new Vector4[2];
        private Vector2[][] UVs = new Vector2[2][];

        public float GetQueueSize()
        {
            if (Shaft == null) return 0.0f;
            return Shaft.RiderQueue.Count();
        }

        public ElevatorShaft()
        {
            CollisionType = CollisionType.Static;
            Shaft.Pieces.Add(this);
        }

        public ElevatorShaft(ComponentManager Manager, Vector3 Position, List<ResourceAmount> Resources) :
            base(Manager, "Elevator Shaft", Matrix.CreateTranslation(Position), Vector3.One, Vector3.Zero)
        {
            CollisionType = CollisionType.Static;

            AddChild(new CraftDetails(Manager, "Elevator Shaft", Resources));
            Shaft.Pieces.Add(this);

            CreateCosmeticChildren(Manager);
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (HasMoved)
                NeedsConnectionUpdate = true;

            base.Update(Time, Chunks, Camera);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            Sheet = new SpriteSheet(ContentPaths.Entities.Furniture.elevator, 32, 32);

            AddChild(new GenericVoxelListener(manager, Matrix.Identity, new Vector3(1.5f, 1.5f, 1.5f), Vector3.Zero, (_event) =>
            {
                Primitive = null;
            })).SetFlag(Flag.ShouldSerialize, false);

            UVs[0] = Sheet.GenerateTileUVs(new Point(0, 0), out Bounds[0]);
            UVs[1] = Sheet.GenerateTileUVs(new Point(1, 0), out Bounds[1]);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        private void DrawNeighborConnection(UInt32 NeighborID)
        {
            if (NeighborID == ComponentManager.InvalidID) return;
            var neighbor = Manager.FindComponent(NeighborID);
            if (neighbor is ElevatorShaft neighborElevator)
                Drawer3D.DrawLine(Position, neighborElevator.Position, new Color(0.0f, 1.0f, 1.0f), 0.1f);
        }

        private void AddSideQuad(VoxelHandle Voxel, GlobalVoxelOffset VoxelOffset, float YRotation, Vector3 Offset)
        {
            var neighborVoxel = new VoxelHandle(Voxel.Chunk.Manager, Voxel.Coordinate + VoxelOffset);
            var texture = 0;

            if (!neighborVoxel.IsValid)
                texture = 1;
            else if (!neighborVoxel.IsEmpty)
                texture = 1;
            else
            {
                var below = new VoxelHandle(Voxel.Chunk.Manager, neighborVoxel.Coordinate + new GlobalVoxelOffset(0, -1, 0));
                if (!below.IsValid || below.IsEmpty)
                    texture = 1;
            }

            Primitive.AddQuad(
                Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                * Matrix.CreateRotationY(YRotation)
                * Matrix.CreateTranslation(Offset),
                Color.White, Color.White,
                UVs[texture],
                Bounds[texture]);
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (Debugger.Switches.DrawRailNetwork)
            {
                DrawNeighborConnection(TrackAbove);
                DrawNeighborConnection(TrackBelow);
            }

            if (Primitive == null)
            {
                Primitive = new RawPrimitive();
                var voxel = GetContainingVoxel();
                AddSideQuad(voxel, new GlobalVoxelOffset(1, 0, 0), (float)Math.PI * 0.5f, new Vector3(0.45f, 0.0f, 0.0f));
                AddSideQuad(voxel, new GlobalVoxelOffset(0, 0, 1), 0.0f, new Vector3(0.0f, 0.0f, 0.45f));
                AddSideQuad(voxel, new GlobalVoxelOffset(-1, 0, 0), (float)Math.PI * 0.5f, new Vector3(-0.45f, 0.0f, 0.0f));
                AddSideQuad(voxel, new GlobalVoxelOffset(0, 0, -1), 0.0f, new Vector3(0.0f, 0.0f, -0.45f));
            }

            if (Primitive.VertexCount == 0) return;

            var under = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Position));

            if (under.IsValid)
            {
                Color color = new Color(under.Sunlight ? 255 : 0, 255, 0);
                LightRamp = color;
            }
            else
                LightRamp = new Color(200, 255, 0);

            Color origTint = effect.VertexColorTint;
            if (!Active)
            {
                DoStipple(effect);
            }
            effect.VertexColorTint = VertexColor;
            effect.LightRamp = LightRamp;
            effect.World = GlobalTransform;

            effect.MainTexture = Sheet.GetTexture();


            effect.EnableWind = false;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }

            effect.VertexColorTint = origTint;
            if (!Active && !String.IsNullOrEmpty(previousEffect))
                effect.CurrentTechnique = effect.Techniques[previousEffect];
        }

        private string previousEffect = null;

        public void DoStipple(Shader effect)
        {
#if DEBUG
            if (effect.CurrentTechnique.Name == Shader.Technique.Stipple)
                throw new InvalidOperationException("Stipple technique not cleaned up. Was EndDraw called?");
#endif
            if (effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBuffer] && effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBufferInstanced])
            {
                previousEffect = effect.CurrentTechnique.Name;
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Stipple];
            }
            else
                previousEffect = null;
        }

        public ElevatorShaft GetShaftAbove()
        {
            return Manager.FindComponent(TrackAbove) as ElevatorShaft;
        }

        public ElevatorShaft GetShaftBelow()
        {
            return Manager.FindComponent(TrackBelow) as ElevatorShaft;
        }

        public IEnumerable<VoxelHandle> EnumerateExits()
        {
            foreach (var neighborVoxel in VoxelHelpers.EnumerateManhattanNeighbors2D_Y(GlobalVoxelCoordinate.FromVector3(Position)))
            {
                var below = neighborVoxel + new GlobalVoxelOffset(0, -1, 0);
                var neighborHandle = new VoxelHandle(Manager.World.ChunkManager, neighborVoxel);
                if (neighborHandle.IsValid && neighborHandle.IsEmpty)
                {
                    var belowHandle = new VoxelHandle(Manager.World.ChunkManager, below);
                    if (belowHandle.IsValid && !belowHandle.IsEmpty)
                        yield return neighborHandle;
                }
            }
        }

        public VoxelHandle GetContainingVoxel()
        {
            return new VoxelHandle(Manager.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Position));
        }
    }
}
