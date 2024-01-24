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

        public static RawPrimitive MakeSpriteSheetCellPrimitive(SpriteSheet Sheet, int x, int y, int width, int height)
        {
            var bounds = Vector4.Zero;
            var uvs = Sheet.GenerateTileUVs(new Point(x, y), new Point(width, height), out bounds);
            var r = new RawPrimitive();
            r.AddQuad(Matrix.Identity, Color.White, Color.White, uvs, bounds);
            return r;
        }

        // Assuming we're passed four unaligned quads, assemble them into a cube.
        public static RawPrimitive MakePrismFromSides(RawPrimitive Top, RawPrimitive Left, RawPrimitive Right, RawPrimitive Front, RawPrimitive Back, RawPrimitive Bottom)
        {
            if (Top != null)
                Top.Transform(Matrix.CreateTranslation(0.0f, 0.5f, 0.0f));

            if (Left != null)
            {
                Left.Transform(Matrix.CreateRotationZ((float)Math.PI));
                Left.Transform(Matrix.CreateRotationX((float)Math.PI / 2.0f));
                Left.Transform(Matrix.CreateRotationZ((float)Math.PI));
                Left.Transform(Matrix.CreateTranslation(0.0f, 0.0f, 0.5f));
            }

            if (Left != null)
            {
                Right.Transform(Matrix.CreateRotationZ((float)-Math.PI));
                Right.Transform(Matrix.CreateRotationX((float)Math.PI / 2.0f));
                Right.Transform(Matrix.CreateRotationZ((float)Math.PI));
                Right.Transform(Matrix.CreateTranslation(0.0f, 0.0f, -0.5f));
            }

            if (Front != null)
            {
                Front.Transform(Matrix.CreateRotationZ((float)Math.PI / 2));
                Front.Transform(Matrix.CreateRotationX((float)-Math.PI / 2));
                Front.Transform(Matrix.CreateTranslation(-0.5f, 0.0f, 0.0f));
            }

            if (Back != null)
            {
                Back.Transform(Matrix.CreateRotationZ((float)-Math.PI / 2));
                Back.Transform(Matrix.CreateRotationX((float)-Math.PI / 2));
                Back.Transform(Matrix.CreateTranslation(0.5f, 0.0f, 0.0f));
            }


            if (Bottom != null)
            {
                Bottom.Transform(Matrix.CreateRotationZ((float)Math.PI));
                Bottom.Transform(Matrix.CreateTranslation(0.0f, -0.5f, 0.0f));
            }

            return RawPrimitive.Concat(Top, Left, Right, Front, Back, Bottom);
        }

    }
}