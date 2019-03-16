using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class SteamPoweredObject : Body
    {
        [JsonIgnore]
        public List<UInt32> NeighborPipes = new List<UInt32>();

        public float SteamPressure = 0.0f;
        public float GeneratedSteam = 0.0f;
        public bool Generator = false;
        public bool DrawPipes = true;
        public Orientation Orientation = Orientation.North;

        public RawPrimitive Primitive;
        private Color VertexColor = Color.White;
        private Color LightRamp = Color.White;
        private SpriteSheet Sheet;

        public virtual bool CanSendSteam(SteamPoweredObject Other)
        {
            return true;
        }

        public virtual bool CanReceiveSteam(SteamPoweredObject Other)
        {
            return true;
        }
        
        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch (messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    HasMoved = true;
                    break;
            }

            base.ReceiveMessageRecursive(messageToReceive);
        }

        public SteamPoweredObject()
        {
            CollisionType = CollisionType.Static;
        }

        public SteamPoweredObject(
            ComponentManager Manager) :
            base(Manager, "Steam Powered", Matrix.Identity, 
                Vector3.One,
                Vector3.Zero)
        { 
            CollisionType = CollisionType.Static;
            
            CreateCosmeticChildren(Manager);
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

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (Debugger.Switches.DrawPipeNetwork)
            {
                foreach (var neighborConnection in NeighborPipes)
                {
                    var neighbor = Manager.FindComponent(neighborConnection);
                    if (neighbor == null)
                        Drawer3D.DrawLine(Position, Position + Vector3.UnitY, Color.CornflowerBlue, 0.1f);
                    else
                        Drawer3D.DrawLine(Position + new Vector3(0.0f, 0.5f, 0.0f), (neighbor as Body).Position + new Vector3(0.0f, 0.5f, 0.0f), new Color(SteamPressure, 0.0f, 0.0f, 1.0f), 0.1f);
                }

                Drawer3D.DrawBox(GetBoundingBox(), Color.Red, 0.01f, false);
                Drawer3D.DrawLine(Position, Position + Vector3.Transform(new Vector3(1, 0, 0), Matrix.CreateRotationY((float)Math.PI / 2 * (float)Orientation)), new Color(0.0f, 1.0f, 1.0f), 0.03f);
            }

            if (!DrawPipes) return;

            if (Primitive == null)
            {
                var bounds = Vector4.Zero;
                var uvs = Sheet.GenerateTileUVs(new Point(0,0), out bounds);
               
                Primitive = new RawPrimitive();

                foreach (var connection in NeighborPipes)
                {
                    var neighbor = Manager.FindComponent(connection) as Body;
                    if (neighbor == null) continue;

                    var orientationToNeighbor = OrientationHelper.DetectOrientationFromVector(new Vector3(neighbor.Position.X - this.Position.X, 0.0f, neighbor.Position.Z - this.Position.Z));
                    var pipeAngle = Math.PI * 0.5f * ((float)Orientation - (float)orientationToNeighbor);

                    Primitive.AddQuad(
                        Matrix.CreateTranslation(0.5f, 0.0f, 0.0f)
                        * Matrix.CreateScale(0.5f, 0.5f, 0.5f)
                        * Matrix.CreateRotationX((float)Math.PI * 0.5f)
                        //* Matrix.CreateTranslation(0.0f, 0.3f, -0.2f)
                        * Matrix.CreateRotationY((float)pipeAngle),
                        //* Matrix.CreateTranslation(bumperOffset + bumperGap),
                        Color.White, Color.White, uvs, bounds);
                }
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
            if (!Active)
            {
                EndDraw(effect);
            }
        }
        private string previousEffect = null;

        public void DoStipple(Shader effect)
        {
#if DEBUG
            if (effect.CurrentTechnique.Name == Shader.Technique.Stipple)
            {
                throw new InvalidOperationException("Stipple technique not cleaned up. Was EndDraw called?");
            }
#endif
            if (effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBuffer] && effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBufferInstanced])
            {
                previousEffect = effect.CurrentTechnique.Name;
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Stipple];
            }
            else
            {
                previousEffect = null;
            }
        }

        public void EndDraw(Shader shader)
        {
            if (!String.IsNullOrEmpty(previousEffect))
            {
                shader.CurrentTechnique = shader.Techniques[previousEffect];
            }
        }

        public void DetachFromNeighbors()
        {
            foreach (var neighbor in NeighborPipes.Select(connection => Manager.FindComponent(connection)))
            {
                if (neighbor is SteamPoweredObject neighborPipe)
                    neighborPipe.DetachNeighbor(this.GlobalID);
            }

            NeighborPipes.Clear();
            Primitive = null;
        }

        private void DetachNeighbor(uint ID)
        {
            NeighborPipes.RemoveAll(connection => connection == ID);
            Primitive = null;
        }

        public void AttachToNeighbors()
        {
            global::System.Diagnostics.Debug.Assert(NeighborPipes.Count == 0);

            foreach (var entity in Manager.World.EnumerateIntersectingObjects(this.BoundingBox.Expand(0.1f), CollisionType.Static))
            {
                if (Object.ReferenceEquals(entity, this)) continue;
                var neighborPipe = entity as SteamPoweredObject;
                if (neighborPipe == null) continue;

                var distance = (neighborPipe.Position - Position).Length2D();
                if (distance > 1.1f) continue;

                AttachNeighbor(neighborPipe.GlobalID);
                neighborPipe.AttachNeighbor(this.GlobalID);
            }

            Primitive = null;
        }

        private void AttachNeighbor(uint ID)
        {
            NeighborPipes.Add(ID);
            Primitive = null;
        }

        public override void Delete()
        {
            base.Delete();
            DetachFromNeighbors();
        }

        public override void Die()
        {
            base.Die();
            DetachFromNeighbors();
        }
    }
}
