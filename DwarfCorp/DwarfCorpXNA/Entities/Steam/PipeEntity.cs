// RailEntity.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class PipeEntity : CraftedBody, ITintable
    {
        [EntityFactory("Pipe")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new PipeEntity(Manager, new VoxelHandle(Manager.World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(Position)));
        }

        [JsonProperty]
        private VoxelHandle Location;

        [JsonIgnore]
        public List<UInt32> NeighborPipes = new List<UInt32>();
        
        private VoxelHandle ContainingVoxel {  get { return GetContainingVoxel(); } }

        private const float sqrt2 = 1.41421356237f;
        private SpriteSheet Sheet;
        private Point Frame;
        private RawPrimitive Primitive;

        private Color VertexColor = Color.White;
        private Color LightRamp = Color.White;

        public void SetVertexColor(Color Tint)
        {
            this.VertexColor = Tint;
        }

        public void SetLightRamp(Color Tint)
        {
            this.LightRamp = Tint;
        }

        public void SetOneShotTint(Color Tint)
        { }

        private static float[,] VertexHeightOffsets =
        {
            { 0.0f, 0.0f, 0.0f, 0.0f },
            { 1.0f, 0.0f, 0.0f, 1.0f },
            { 0.0f, 1.0f, 1.0f, 0.0f },
            { 1.0f, 1.0f, 1.0f, 1.0f }
        };

        public VoxelHandle GetLocation()
        {
            return Location;
        }

        public VoxelHandle GetContainingVoxel()
        {
            return new VoxelHandle(Location.Chunk.Manager.ChunkData, Location.Coordinate);
        }

        public void ResetPrimitive()
        {
            Primitive = null;
        }

        // Perhaps should be handled in base class?
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

        private float AngleBetweenVectors(Vector2 A, Vector2 B)
        {
            A.Normalize();
            B.Normalize();
            float DotProduct = Vector2.Dot(A, B);
            DotProduct = MathHelper.Clamp(DotProduct, -1.0f, 1.0f);
            float Angle = (float)System.Math.Acos(DotProduct);
            if (CrossZ(A, B) < 0) return -Angle;
            return Angle;
        }

        private float CrossZ(Vector2 A, Vector2 B)
        {
            return (B.Y * A.X) - (B.X * A.Y);
        }

        private float Sign(float F)
        {
            if (F < 0) return -1.0f;
            return 1.0f;
        }

        public PipeEntity()
        {
            CollisionType = CollisionType.Static;
        }

        public PipeEntity(
            ComponentManager Manager,
            VoxelHandle Location) :

            base(Manager, "Steam Pipe", 
                Matrix.CreateTranslation(Location.WorldPosition), 
                Vector3.One,
                Vector3.Zero,
                new CraftDetails(Manager, "Rail", new List<ResourceAmount> { new ResourceAmount("Rail", 1) }))
        {
            this.Location = Location;

            CollisionType = CollisionType.Static;
            AddChild(new Health(Manager, "Hp", 100, 0, 100));
            
            PropogateTransforms();
            CreateCosmeticChildren(Manager);
        }
        
        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            Sheet = new SpriteSheet(ContentPaths.rail_tiles, 32, 32);
            
            AddChild(new GenericVoxelListener(manager, Matrix.Identity, new Vector3(0.8f, 1.5f, 0.8f), Vector3.Zero, (_event) =>
            {
                if (!Active) return;

                if (_event.Type == VoxelChangeEventType.VoxelTypeChanged && _event.NewVoxelType == 0)
                {
                    Die();
                    var designation = World.PlayerFaction.Designations.EnumerateEntityDesignations(DesignationType.Craft).FirstOrDefault(d => Object.ReferenceEquals(d.Body, this));
                    if (designation != null)
                    {
                        World.PlayerFaction.Designations.RemoveEntityDesignation(this, DesignationType.Craft);
                        var craftDesignation = designation.Tag as CraftDesignation;
                        if (craftDesignation.WorkPile != null)
                            craftDesignation.WorkPile.Die();
                    }
                }
            })).SetFlag(Flag.ShouldSerialize, false);

            UpdatePiece(Location);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        private static void _addLineSegment(Vector3 A, Vector3 B, Color Color, float Thickness, RawPrimitive Primitive, Vector4 TextureBounds)
        {
            var perp = Vector3.Cross(A, B);
            perp.Normalize();
            perp *= Thickness / 2;

            var startIndex = Primitive.IndexCount;
            Primitive.AddVertex(new ExtendedVertex(A + perp, Color, Color, new Vector2(0, 0), TextureBounds));
            Primitive.AddVertex(new ExtendedVertex(A - perp, Color, Color, new Vector2(0, 0), TextureBounds));
            Primitive.AddVertex(new ExtendedVertex(B - perp, Color, Color, new Vector2(0, 0), TextureBounds));
            Primitive.AddVertex(new ExtendedVertex(B + perp, Color, Color, new Vector2(0, 0), TextureBounds));
            Primitive.AddIndicies(new short[] {
                (short)(startIndex + 0), (short)(startIndex + 1), (short)(startIndex + 3),
                (short)(startIndex + 3), (short)(startIndex + 1), (short)(startIndex + 2) });

        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (Debugger.Switches.DrawPipeNetwork)
            {
                Drawer3D.DrawLine(GetContainingVoxel().GetBoundingBox().Center(), GlobalTransform.Translation, Color.White, 0.01f);

                foreach (var neighborConnection in NeighborPipes)
                {
                    var neighbor = Manager.FindComponent(neighborConnection);
                    if (neighbor == null)
                        Drawer3D.DrawLine(Position, Position + Vector3.UnitY, Color.CornflowerBlue, 0.1f);
                    else
                        Drawer3D.DrawLine(Position + new Vector3(0.0f, 0.5f, 0.0f), (neighbor as Body).Position + new Vector3(0.0f, 0.5f, 0.0f), Color.Teal, 0.1f);
                }
            }

            if (!IsVisible)
                return;

            if (NeighborPipes.Count > 0)
            {
                if (Primitive == null)
                {
                    var bounds = Vector4.Zero;
                    var uvs = Sheet.GenerateTileUVs(Frame, out bounds);
                    Primitive = new RawPrimitive();

                    foreach (var neighborConnection in NeighborPipes)
                    {
                        var neighbor = Manager.FindComponent(neighborConnection);
                        if (neighbor is Body _neighbor)
                            _addLineSegment(Position + new Vector3(0.0f, 0.5f, 0.0f), _neighbor.Position + new Vector3(0.0f, 0.5f, 0.0f), Color.Brown, 0.1f, Primitive, bounds);
                    }
                }

                // Everything that draws should set it's tint, making this pointless.

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

        private void DetachFromNeighbors()
        {
            foreach (var neighbor in NeighborPipes.Select(connection => Manager.FindComponent(connection)))
            {
                if (neighbor is PipeEntity neighborPipe)
                    neighborPipe.DetachNeighbor(this.GlobalID);
            }

            NeighborPipes.Clear();
        }

        private void DetachNeighbor(uint ID)
        {
            NeighborPipes.RemoveAll(connection => connection == ID);
            ResetPrimitive();
        }

        private void AttachToNeighbors()
        {
            System.Diagnostics.Debug.Assert(NeighborPipes.Count == 0);

            foreach (var entity in Manager.World.EnumerateIntersectingObjects(this.BoundingBox.Expand(0.1f), CollisionType.Static))
            {
                if (Object.ReferenceEquals(entity, this)) continue;
                var neighborPipe = entity as PipeEntity;
                if (neighborPipe == null) continue;

                AttachNeighbor(neighborPipe.GlobalID);
                neighborPipe.AttachNeighbor(this.GlobalID);
            }
        }

        private void AttachNeighbor(uint ID)
        {
            NeighborPipes.Add(ID);
            ResetPrimitive();
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
            EntityFactory.CreateEntity<Body>("Rail Resource", MathFunctions.RandVector3Box(GetBoundingBox()));
        }

        public void UpdatePiece(VoxelHandle Location)
        {
            DetachFromNeighbors();

            this.Location = Location;

            LocalTransform = Matrix.CreateTranslation(Location.WorldPosition + new Vector3(0.5f, 0.2f, 0.5f));

            ResetPrimitive();

            // Hack to make the listener update it's damn bounding box
            var deathTrigger = EnumerateChildren().OfType<GenericVoxelListener>().FirstOrDefault();
            if (deathTrigger != null)
                deathTrigger.LocalTransform = Matrix.Identity;

            AttachToNeighbors();
            PropogateTransforms();
        }
    }
}
