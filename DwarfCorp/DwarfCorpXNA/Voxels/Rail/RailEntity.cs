// Fixture.cs
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
    public class RailEntity : Body
#if DEBUG
        , IRenderableComponent
#endif
    {
        public Rail.JunctionPiece Piece;

        public RailEntity()
        {
            
        }

        public RailEntity(
            ComponentManager Manager,
            VoxelHandle Location,
            Rail.JunctionPiece Piece) :

            base(Manager, "Fixture", 
                Matrix.CreateTranslation(Location.WorldPosition + new Vector3(Piece.Offset.X, 0, Piece.Offset.Y)), 
                Vector3.One,
                Vector3.Zero,
                true)
        {
            this.Piece = Piece;

            CollisionType = CollisionManager.CollisionType.Static;
            AddChild(new Health(Manager, "Hp", 100, 0, 100));
            
            PropogateTransforms();
            CreateCosmeticChildren(Manager);
        }
        
        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            var piece = Rail.RailLibrary.GetRailPiece(Piece.RailPiece);

            Matrix transform = Matrix.CreateRotationX((float)Math.PI * 0.5f) * Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation);

            AddChild(new SimpleSprite(manager, "Sprite", transform, false, new SpriteSheet(ContentPaths.rail_tiles, 32, 32), piece.Tile)
            { 
                OrientationType = SimpleSprite.OrientMode.Fixed
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new NewVoxelListener(manager, Matrix.Identity, new Vector3(0.8f, 1.5f, 0.8f), Vector3.Zero, (_event) =>
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
            }));
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

        public void UpdatePiece(Rail.JunctionPiece Piece, VoxelHandle Location)
        {
            this.Piece = Piece;

            LocalTransform = Matrix.CreateTranslation(Location.WorldPosition + new Vector3(Piece.Offset.X, 0, Piece.Offset.Y) + new Vector3(0.5f, 0.2f, 0.5f));

            var piece = Rail.RailLibrary.GetRailPiece(Piece.RailPiece);
            var spriteChild = EnumerateChildren().FirstOrDefault(c => c.Name == "Sprite") as SimpleSprite;
            Matrix transform = Matrix.CreateRotationX((float)Math.PI * 0.5f) * Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation);
            spriteChild.LocalTransform = transform;
            spriteChild.SetFrame(piece.Tile);

            // Hack to make the listener update it's damn bounding box
            EnumerateChildren().OfType<NewVoxelListener>().FirstOrDefault().LocalTransform = Matrix.Identity;
        }
    }
}
