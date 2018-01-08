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
    public class RailPiece : Body
#if DEBUG
        , IRenderableComponent
#endif
    {
        public Rail.JunctionPiece Piece;

        public RailPiece()
        {
            
        }

        public RailPiece(
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
        }

#if DEBUG
        public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (GamePerformance.DebugVisualizationEnabled)
                Drawer3D.DrawBox(this.GetBoundingBox(), Color.Yellow, 0.2f, false);
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
        }
    }
}
