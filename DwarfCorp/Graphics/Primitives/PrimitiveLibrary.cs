using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    /// <summary>
    /// A static collection of certain geometric primitives.
    /// </summary>
    public class PrimitiveLibrary
    {
        public static Dictionary<string, GeometricPrimitive> Primitives;
        private static List<Matrix> treeTransforms = new List<Matrix> { Matrix.Identity, Matrix.CreateRotationY((float)Math.PI / 2.0f) };
        private static List<Color> treeTints = new List<Color> { Color.White, Color.White };

        public enum PrimitiveType
        {
            Quad,
            Cross,
            Box
        }

        public class PrimitiveRecord
        {
            public String Name;
            public PrimitiveType Type;
            public String Asset;
            public List<OldBoxPrimitive.FaceData> BoxFaceData;
            public Vector3 BoxSize;
        }

        public static void AddPrimitiveIfNew(PrimitiveRecord Record)
        {
            if (Primitives.ContainsKey(Record.Name))
                return;

            Primitives[Record.Name] = InitializePrimitive(Record);

        }

        public static void Initialize(ContentManager content)
        {
            Primitives = new Dictionary<string, GeometricPrimitive>();

            var primitives = FileUtils.LoadJsonListFromMultipleSources<PrimitiveRecord>(ContentPaths.primitives, null, p => p.Name);

            foreach (var prim in primitives)
            {
                Primitives[prim.Name] = InitializePrimitive(prim);
            }
        }

        private static GeometricPrimitive InitializePrimitive(PrimitiveRecord prim)
        {
            var spriteSheet = new NamedImageFrame(prim.Asset);
            int width = spriteSheet.SafeGetImage().Width;
            int height = spriteSheet.SafeGetImage().Height;

            switch (prim.Type)
            {
                case PrimitiveType.Box:
                    return new OldBoxPrimitive(DwarfGame.GuiSkin.Device, prim.BoxSize.X, prim.BoxSize.Y, prim.BoxSize.Z,
                        new OldBoxPrimitive.BoxTextureCoords(width, height,
                            prim.BoxFaceData[0], prim.BoxFaceData[1], prim.BoxFaceData[2], prim.BoxFaceData[3], prim.BoxFaceData[4], prim.BoxFaceData[5])); // WTF
                case PrimitiveType.Cross:
                    return CreateCrossPrimitive(spriteSheet);
                case PrimitiveType.Quad:
                    return new BatchBillboardPrimitive(spriteSheet, width, height,
                        new Point(0, 0), width / 32.0f, height / 32.0f, false,
                        new List<Matrix> { treeTransforms[0] },
                        new List<Color> { treeTints[0] },
                        new List<Color> { treeTints[0] });
                default:
                    throw new InvalidProgramException();
            }
        }

        public static GeometricPrimitive CreateCrossPrimitive(NamedImageFrame spriteSheet)
        {
            int width = spriteSheet.SafeGetImage().Width;
            int height = spriteSheet.SafeGetImage().Height;

            return new BatchBillboardPrimitive(spriteSheet, width, height,
                new Point(0, 0), width / 32.0f, height / 32.0f, false, treeTransforms, treeTints, treeTints);
        }

    }
}