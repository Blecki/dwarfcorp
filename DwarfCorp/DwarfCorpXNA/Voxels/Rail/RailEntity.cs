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

namespace DwarfCorp.Rail
{
    public class RailEntity : Body
#if DEBUG
        , IRenderableComponent
#endif
    {
        public JunctionPiece Piece { get; private set; }
        private VoxelHandle Location;
        
        public RailEntity()
        {
            
        }

        public RailEntity(
            ComponentManager Manager,
            VoxelHandle Location,
            JunctionPiece Piece) :

            base(Manager, "Fixture", 
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
            }));

            UpdatePiece(Piece, Location);
        }

#if DEBUG
        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (GamePerformance.DebugVisualizationEnabled)
            {
                var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation) * GlobalTransform;
                var piece = Rail.RailLibrary.GetRailPiece(Piece.RailPiece);
                foreach (var spline in piece.SplinePoints)
                    for (var i = 1; i < spline.Count; ++i)
                        Drawer3D.DrawLine(Vector3.Transform(spline[i - 1], transform),
                            Vector3.Transform(spline[i], transform), Color.Purple, 0.1f);
            }
        }
#endif

        public List<List<Vector3>> GetTransformedSplines()
        {
            var piece = RailLibrary.GetRailPiece(Piece.RailPiece);
            return piece.SplinePoints.Select(s => s.Select(p => Vector3.Transform(p, GlobalTransform)).ToList()).ToList();
        }

        public void UpdatePiece(JunctionPiece Piece, VoxelHandle Location)
        {
            this.Piece = Piece;
            this.Location = Location;

            LocalTransform = Matrix.CreateTranslation(Location.WorldPosition + new Vector3(Piece.Offset.X, 0, Piece.Offset.Y) + new Vector3(0.5f, 0.2f, 0.5f));

            var piece = RailLibrary.GetRailPiece(Piece.RailPiece);
            var spriteChild = EnumerateChildren().OfType<RailSprite>().FirstOrDefault() as RailSprite;
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

            // Hack to make the listener update it's damn bounding box
            EnumerateChildren().OfType<GenericVoxelListener>().FirstOrDefault().LocalTransform = Matrix.Identity;
        }
    }
}
