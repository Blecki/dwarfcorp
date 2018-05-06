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

        public static void Initialize(GraphicsDevice graphics, ContentManager content)
        {
            Primitives = new Dictionary<string, GeometricPrimitive>();

            List<Matrix> treeTransforms = new List<Matrix> { Matrix.Identity, Matrix.CreateRotationY((float)Math.PI / 2.0f) };
            List<Color> treeTints = new List<Color> { Color.White, Color.White };

            var primitives = FileUtils.LoadJsonListFromMultipleSources<PrimitiveRecord>(ContentPaths.primitives, null, p => p.Name);

            foreach (var prim in primitives)
            {
                var spriteSheet = AssetManager.GetContentTexture(prim.Asset);

                switch (prim.Type)
                {
                    case PrimitiveType.Box:
                        Primitives[prim.Name] = new OldBoxPrimitive(graphics, prim.BoxSize.X, prim.BoxSize.Y, prim.BoxSize.Z,
                            new OldBoxPrimitive.BoxTextureCoords(spriteSheet.Width, spriteSheet.Height,
                                prim.BoxFaceData[0], prim.BoxFaceData[1], prim.BoxFaceData[2], prim.BoxFaceData[3], prim.BoxFaceData[4], prim.BoxFaceData[5])); // WTF
                        break;
                    case PrimitiveType.Cross:
                        Primitives[prim.Name] = new BatchBillboardPrimitive(graphics, spriteSheet, spriteSheet.Width, spriteSheet.Height,
                            new Point(0, 0), spriteSheet.Width / 32.0f, spriteSheet.Height / 32.0f, false, treeTransforms, treeTints, treeTints);
                        break;
                    case PrimitiveType.Quad:
                        Primitives[prim.Name] = new BatchBillboardPrimitive(graphics, spriteSheet, spriteSheet.Width, spriteSheet.Height,
                            new Point(0, 0), spriteSheet.Width / 32.0f, spriteSheet.Height / 32.0f, false,
                            new List<Matrix> { treeTransforms[0] },
                            new List<Color> { treeTints[0] },
                            new List<Color> { treeTints[0] });
                        break;
                }
            }
        }
    }
}