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
    public class ElevatorShaft : Body
    {
        [EntityFactory("Elevator Track")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var resources = Data.GetData<List<ResourceAmount>>("Resources", null);

            if (resources == null)
                resources = new List<ResourceAmount>() { new ResourceAmount(ResourceType.Wood) };

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

        public ElevatorShaft()
        {
            CollisionType = CollisionType.Static;
            Shaft.Pieces.Add(this);
        }

        public ElevatorShaft(ComponentManager Manager, Vector3 Position, List<ResourceAmount> Resources) :
            base(Manager, "Elevator Track", Matrix.CreateTranslation(Position), Vector3.One, Vector3.Zero)
        {
            CollisionType = CollisionType.Static;

            AddChild(new CraftDetails(Manager, "Elevator Track", Resources));
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

            Sheet = new SpriteSheet(ContentPaths.rail_tiles, 32, 32);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        private float AngleBetweenVectors(Vector2 A, Vector2 B)
        {
            A.Normalize();
            B.Normalize();
            float DotProduct = Vector2.Dot(A, B);
            DotProduct = MathHelper.Clamp(DotProduct, -1.0f, 1.0f);
            float Angle = (float)global::System.Math.Acos(DotProduct);
            if (CrossZ(A, B) < 0) return -Angle;
            return Angle;
        }

        private float CrossZ(Vector2 A, Vector2 B)
        {
            return (B.Y * A.X) - (B.X * A.Y);
        }

        private void DrawNeighborConnection(UInt32 NeighborID)
        {
            if (NeighborID == ComponentManager.InvalidID) return;
            var neighbor = Manager.FindComponent(NeighborID);
            if (neighbor is ElevatorShaft neighborElevator)
                Drawer3D.DrawLine(Position, neighborElevator.Position, new Color(0.0f, 1.0f, 1.0f), 0.1f);
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
                var bounds = Vector4.Zero;
                var uvs = Sheet.GenerateTileUVs(new Point(0, 0), out bounds);

                Primitive = new RawPrimitive();

                Primitive.AddQuad(
                    Matrix.CreateRotationX((float)Math.PI * 0.5f)
                    * Matrix.CreateRotationY((float)Math.PI * 0.5f)
                    * Matrix.CreateTranslation(0.45f, 0.0f, 0.0f),
                    Color.White, Color.White, uvs, bounds);

                Primitive.AddQuad(
                    Matrix.CreateRotationX((float)Math.PI * 0.5f)
                    * Matrix.CreateTranslation(0.0f, 0.0f, 0.45f),
                    Color.White, Color.White, uvs, bounds);

                Primitive.AddQuad(
                    Matrix.CreateRotationX((float)Math.PI * 0.5f)
                    * Matrix.CreateRotationY((float)Math.PI * 0.5f)
                    * Matrix.CreateTranslation(-0.45f, 0.0f, 0.0f),
                    Color.White, Color.White, uvs, bounds);

                Primitive.AddQuad(
                    Matrix.CreateRotationX((float)Math.PI * 0.5f)
                    * Matrix.CreateTranslation(0.0f, 0.0f, -0.45f),
                    Color.White, Color.White, uvs, bounds);
            }

            if (Primitive.VertexCount == 0) return;

            var under = new VoxelHandle(chunks.ChunkData,
                    GlobalVoxelCoordinate.FromVector3(Position));

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
    }
}
