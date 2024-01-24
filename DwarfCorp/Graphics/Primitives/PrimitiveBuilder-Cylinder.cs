using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;


namespace DwarfCorp
{
    public static partial class PrimitiveBuilder
    {

        public static RawPrimitive MakeSpriteCellMappedCylinder(SpriteSheet Sheet, int x, int y, int width, int height, int Sides)
        {
            var bounds = Vector4.Zero;
            var uvs = Sheet.GenerateTileUVs(new Point(x, y), new Point(width, height), out bounds);
            var r = new RawPrimitive();

            var startingPoint = new Vector3(1, 0, 0);
            var angle = (float)Math.PI * 2.0f / (float)Sides;
            var uStep = 1.0f / (float)Sides;

            var startingIndex = (short)0;
            r.AddVertex(new ExtendedVertex { Position = startingPoint + new Vector3(0, -1, 0), TextureCoordinate = new Vector2(0, 0) });
            r.AddVertex(new ExtendedVertex { Position = startingPoint + new Vector3(0, 1, 0), TextureCoordinate = new Vector2(0, 1) });

            for (var side = 0; side < Sides; ++side)
            {
                var currentPoint = Vector3.Transform(startingPoint, Matrix.CreateRotationY((side + 1) * angle));
                var currentU = (side + 1) * uStep;

                r.AddVertex(new ExtendedVertex { Position = currentPoint + new Vector3(0, -1, 0), TextureCoordinate = new Vector2(currentU, 0) });
                r.AddVertex(new ExtendedVertex { Position = currentPoint + new Vector3(0, 1, 0), TextureCoordinate = new Vector2(currentU, 1) });

                r.AddIndex(startingIndex);
                r.AddIndex((short)(startingIndex + 1));
                r.AddIndex((short)(startingIndex + 2));
                r.AddIndex((short)(startingIndex + 1));
                r.AddIndex((short)(startingIndex + 3));
                r.AddIndex((short)(startingIndex + 2));
                startingIndex += 2;
            }

            r.TransformEx(v =>
            {
                v.TextureCoordinate.X *= (bounds.Z - bounds.X);
                v.TextureCoordinate.Y *= (bounds.W - bounds.Y);
                v.TextureCoordinate.X += bounds.X;
                v.TextureCoordinate.Y += bounds.Y;
                v.TextureBounds = bounds;
                v.Color = Color.White;
                v.VertColor = Color.White;
                return v;
            });

            return r;
        }


    }
}