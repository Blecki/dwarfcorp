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

namespace DwarfCorp.Rail
{
    public class RailEntity : Body, IRenderableComponent
    {
        public class NeighborConnection
        {
            public uint NeighborID;
            public Vector3 Position;
        }

        [JsonProperty]
        private JunctionPiece Piece;

        [JsonProperty]
        private VoxelHandle Location;

        [JsonIgnore]
        public List<NeighborConnection> NeighborRails = new List<NeighborConnection>();
        
        private VoxelHandle ContainingVoxel {  get { return GetContainingVoxel(); } }

        public VoxelHandle GetLocation()
        {
            return Location;
        }

        public VoxelHandle GetContainingVoxel()
        {
            return new VoxelHandle(Location.Chunk.Manager.ChunkData, Location.Coordinate + new GlobalVoxelOffset(Piece.Offset.X, 0, Piece.Offset.Y));
        }

        public JunctionPiece GetPiece()
        {
            return Piece;
        }

        public RailEntity()
        {
            
        }

        public RailEntity(
            ComponentManager Manager,
            VoxelHandle Location,
            JunctionPiece Piece) :

            base(Manager, "Rail", 
                Matrix.CreateTranslation(Location.WorldPosition + new Vector3(Piece.Offset.X, 0, Piece.Offset.Y)), 
                Vector3.One,
                Vector3.Zero,
                true)
        {
            this.Piece = Piece;
            this.Location = Location;

            CollisionType = CollisionManager.CollisionType.Static;
            AddChild(new Health(Manager, "Hp", 100, 0, 100));
            
            PropogateTransforms();
            CreateCosmeticChildren(Manager);
        }
        
        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            var piece = RailLibrary.GetRailPiece(Piece.RailPiece);

            AddChild(new RailSprite(manager, "Sprite", Matrix.Identity, new SpriteSheet(ContentPaths.rail_tiles, 32, 32), piece.Tile))
                .SetFlag(Flag.ShouldSerialize, false);

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

            UpdatePiece(Piece, Location);
        }

        public Vector3 InterpolateSpline(float t, Vector3 origin, Vector3 destination)
        {
            var piece = Rail.RailLibrary.GetRailPiece(Piece.RailPiece);
            List<Vector3> selectedSpline = null;
            bool isReversed = false;
            var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation) * GlobalTransform;
            double closestEndpoint = double.MaxValue;

            foreach (var spline in piece.SplinePoints)
            {
                var startPoint = Vector3.Transform(spline.First(), transform);
                var distStart = (startPoint - destination).LengthSquared();
                if (distStart < closestEndpoint)
                {
                    isReversed = true;
                    selectedSpline = spline;
                    closestEndpoint = distStart;
                }

                var endPoint = Vector3.Transform(spline.Last(), transform);
                var distEnd = (endPoint - destination).LengthSquared();
                if (distEnd < closestEndpoint)
                {
                    isReversed = false;
                    selectedSpline = spline;
                    closestEndpoint = distEnd;
                }
            }
            
            if (selectedSpline == null)
            {
                return origin + t * (destination - origin);
            }

            if (isReversed)
            {
                t = 1.0f - t;
            }

            float idx = (selectedSpline.Count - 1) * t;
            int k = MathFunctions.Clamp((int)idx, 0, selectedSpline.Count - 1);
            float remainder = idx - k;
            Drawer3D.DrawLine(Vector3.Transform(selectedSpline[k], transform), Vector3.Transform(selectedSpline[k + 1], transform), isReversed ? Color.Red : Color.Blue, 0.1f);
            return Vector3.Transform(selectedSpline[k] * (1.0f - remainder) + selectedSpline[k + 1] * remainder, transform);
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (Debugger.Switches.DrawRailNetwork)
            {
                Drawer3D.DrawBox(GetContainingVoxel().GetBoundingBox(), Color.White, 0.01f, true);
                Drawer3D.DrawLine(GetContainingVoxel().GetBoundingBox().Center(), GlobalTransform.Translation, Color.White, 0.01f);
                var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation) * GlobalTransform;
                var piece = Rail.RailLibrary.GetRailPiece(Piece.RailPiece);

                foreach (var spline in piece.SplinePoints)
                    for (var i = 1; i < spline.Count; ++i)
                        Drawer3D.DrawLine(Vector3.Transform(spline[i - 1], transform),
                            Vector3.Transform(spline[i], transform), Color.Purple, 0.1f);

                foreach (var connection in piece.EnumerateConnections())
                    Drawer3D.DrawLine(Vector3.Transform(connection.Item1, transform) + new Vector3(0.0f, 0.2f, 0.0f),
                        Vector3.Transform(connection.Item2, transform) + new Vector3(0.0f, 0.2f, 0.0f),
                        Color.Brown, 0.1f);


                foreach (var neighborConnection in NeighborRails)
                {
                    var neighbor = Manager.FindComponent(neighborConnection.NeighborID);
                    if (neighbor == null)
                        Drawer3D.DrawLine(Position, Position + Vector3.UnitY, Color.CornflowerBlue, 0.1f);
                    else
                        Drawer3D.DrawLine(Position + new Vector3(0.0f, 0.5f, 0.0f), (neighbor as Body).Position + new Vector3(0.0f, 0.5f, 0.0f), Color.Teal, 0.1f);
                }
            }
        }

        public List<Tuple<Vector3, Vector3>> GetTransformedConnections()
        {
            var piece = RailLibrary.GetRailPiece(Piece.RailPiece);
            var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation) * GlobalTransform;
            return piece.EnumerateConnections().Select(l => Tuple.Create(Vector3.Transform(l.Item1, transform), Vector3.Transform(l.Item2, transform))).ToList();
        }

        private void DetachFromNeighbors()
        {
            foreach (var neighbor in NeighborRails.Select(connection => Manager.FindComponent(connection.NeighborID)))
            {
                if (neighbor is RailEntity)
                    (neighbor as RailEntity).DetachNeighbor(this.GlobalID);
            }

            NeighborRails.Clear();
        }

        private void DetachNeighbor(uint ID)
        {
            NeighborRails.RemoveAll(connection => connection.NeighborID == ID);
        }

        private void AttachToNeighbors()
        {
            System.Diagnostics.Debug.Assert(NeighborRails.Count == 0);

            var myEndPoints = GetTransformedConnections().SelectMany(l => new Vector3[] { l.Item1, l.Item2 });
            foreach (var entity in Manager.World.CollisionManager.EnumerateIntersectingObjects(this.BoundingBox.Expand(0.5f), CollisionManager.CollisionType.Both))
            {
                if (Object.ReferenceEquals(entity, this)) continue;
                var neighborRail = entity as RailEntity;
                if (neighborRail == null) continue;
                var neighborEndPoints = neighborRail.GetTransformedConnections().SelectMany(l => new Vector3[] { l.Item1, l.Item2 });
                foreach (var point in myEndPoints)
                    foreach (var nPoint in neighborEndPoints)
                        if ((nPoint - point).LengthSquared() < 0.01f)
                        {
                            AttachNeighbor(neighborRail.GlobalID, point);
                            neighborRail.AttachNeighbor(this.GlobalID, point);
                            goto __CONTINUE;
                        }
                __CONTINUE: ;
            }
        }

        private void AttachNeighbor(uint ID, Vector3 Position)
        {
            NeighborRails.Add(new NeighborConnection
            {
                NeighborID = ID,
                Position = Position
            });
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

        public void UpdatePiece(JunctionPiece Piece, VoxelHandle Location)
        {
            DetachFromNeighbors();

            this.Piece = Piece;
            this.Location = Location;

            LocalTransform = Matrix.CreateTranslation(Location.WorldPosition + new Vector3(Piece.Offset.X, 0, Piece.Offset.Y) + new Vector3(0.5f, 0.2f, 0.5f));

            var piece = RailLibrary.GetRailPiece(Piece.RailPiece);
            var spriteChild = EnumerateChildren().OfType<RailSprite>().FirstOrDefault() as RailSprite;

            if (spriteChild != null)
            {
                spriteChild.LocalTransform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation);

                switch (piece.Shape)
                {
                    case RailShape.Flat:
                        spriteChild.VertexHeightOffsets[0] = 0.0f;
                        spriteChild.VertexHeightOffsets[1] = 0.0f;
                        spriteChild.VertexHeightOffsets[2] = 0.0f;
                        spriteChild.VertexHeightOffsets[3] = 0.0f;
                        break;
                    case RailShape.TopHalfSlope:
                        spriteChild.VertexHeightOffsets[0] = 1.0f;
                        spriteChild.VertexHeightOffsets[1] = 0.5f;
                        spriteChild.VertexHeightOffsets[2] = 0.5f;
                        spriteChild.VertexHeightOffsets[3] = 1.0f;
                        break;
                    case RailShape.BottomHalfSlope:
                        spriteChild.VertexHeightOffsets[0] = 0.5f;
                        spriteChild.VertexHeightOffsets[1] = 0.0f;
                        spriteChild.VertexHeightOffsets[2] = 0.0f;
                        spriteChild.VertexHeightOffsets[3] = 0.5f;
                        break;
                }

                spriteChild.SetFrame(piece.Tile);
            }

            // Hack to make the listener update it's damn bounding box
            EnumerateChildren().OfType<GenericVoxelListener>().FirstOrDefault().LocalTransform = Matrix.Identity;

            AttachToNeighbors();
        }
    }
}
