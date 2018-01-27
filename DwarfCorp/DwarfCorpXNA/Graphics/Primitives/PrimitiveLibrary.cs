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
        public static Dictionary<string, OldBoxPrimitive> BoxPrimitives = new Dictionary<string, OldBoxPrimitive>();
        public static Dictionary<string, BillboardPrimitive> BillboardPrimitives = new Dictionary<string, BillboardPrimitive>();
        public static Dictionary<string, BatchBillboardPrimitive> BatchBillboardPrimitives = new Dictionary<string, BatchBillboardPrimitive>();

        private static bool m_initialized = false;

        public PrimitiveLibrary(GraphicsDevice graphics, ContentManager content)
        {
            Initialize(graphics, content);
        }

        public static void CreateIntersecting(string name, string assetName, GraphicsDevice graphics, ContentManager content)
        {
            Texture2D bushSheet = TextureManager.GetContentTexture(assetName);
            List<Matrix> bushTransforms = new List<Matrix>();
            List<Color> bushColors = new List<Color>();
            bushTransforms.Add(Matrix.Identity);
            bushTransforms.Add(Matrix.CreateRotationY(1.57f));
            bushColors.Add(Color.White);
            bushColors.Add(Color.White);

            BatchBillboardPrimitives[name] = new BatchBillboardPrimitive(graphics, bushSheet, bushSheet.Width, bushSheet.Height, new Point(0, 0), 1.0f, 1.0f, false, bushTransforms, bushColors, bushColors);
        }

        public void Initialize(GraphicsDevice graphics, ContentManager content)
        {
            if(!m_initialized)
            {
                Texture2D spriteSheet = TextureManager.GetContentTexture(ContentPaths.Entities.Furniture.bedtex);
                OldBoxPrimitive.BoxTextureCoords boxCoords = new OldBoxPrimitive.BoxTextureCoords(spriteSheet.Width, spriteSheet.Height,
                    new OldBoxPrimitive.FaceData(new Rectangle(0, 24, 24, 16), true),
                    new OldBoxPrimitive.FaceData(new Rectangle(72, 24, 24, 16), true),
                    new OldBoxPrimitive.FaceData(new Rectangle(24, 0, 48, 24), false),
                    new OldBoxPrimitive.FaceData(new Rectangle(0, 0, 1, 1), true),
                    new OldBoxPrimitive.FaceData(new Rectangle(24, 24, 48, 16), true),
                    new OldBoxPrimitive.FaceData(new Rectangle(24, 40, 48, 16), true));
                BoxPrimitives["bed"] = new OldBoxPrimitive(graphics, 0.8f, 0.5f, 1.8f, boxCoords);

                Texture2D bookSheet = TextureManager.GetContentTexture(ContentPaths.Entities.Furniture.bookshelf);
                OldBoxPrimitive.BoxTextureCoords bookshelfTexture = new OldBoxPrimitive.BoxTextureCoords(bookSheet.Width, bookSheet.Height,
                        new OldBoxPrimitive.FaceData(new Rectangle(0, 20, 20, 32), true),
                        new OldBoxPrimitive.FaceData(new Rectangle(28, 20, 20, 32), true),
                        new OldBoxPrimitive.FaceData(new Rectangle(20, 0, 8, 20), false),
                        new OldBoxPrimitive.FaceData(new Rectangle(0, 0, 1, 1), true),
                        new OldBoxPrimitive.FaceData(new Rectangle(20, 20, 8, 32), true),
                        new OldBoxPrimitive.FaceData(new Rectangle(20, 52, 8, 32), true));
                BoxPrimitives["bookshelf"] = new OldBoxPrimitive(graphics, 20.0f / 32.0f, 32.0f / 32.0f, 8.0f / 32.0f, bookshelfTexture);

                m_initialized = false;


                Texture2D sheetTiles = TextureManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
                OldBoxPrimitive.BoxTextureCoords crateCoords = new OldBoxPrimitive.BoxTextureCoords(sheetTiles.Width, sheetTiles.Height, 32, 32,
                    new Point(7, 0),
                    new Point(7, 0),
                    new Point(8, 0),
                    new Point(7, 0),
                    new Point(7, 0),
                    new Point(7, 0));
                BoxPrimitives["crate"] = new OldBoxPrimitive(graphics, 0.9f, 0.9f, 0.9f, crateCoords);
                m_initialized = false;

                List<Matrix> treeTransforms = new List<Matrix>();
                List<Color> treeTints = new List<Color>();
                treeTransforms.Add(Matrix.Identity);
                treeTransforms.Add(Matrix.CreateRotationY(1.57f));
                treeTints.Add(Color.White);
                treeTints.Add(Color.White);

                foreach (var member in typeof(ContentPaths.Entities.Plants).GetFields())
                    if (member.IsStatic && member.FieldType == typeof(String))
                    {
                        var texture = TextureManager.GetContentTexture(member.GetValue(null).ToString());

                        // lol, worst hack. This should be data driven.
                        if (member.Name != "candycane")
                        {
                            BatchBillboardPrimitives[member.Name] = new BatchBillboardPrimitive(graphics, texture, texture.Width, texture.Height,
                                new Point(0, 0), texture.Width / 32.0f, texture.Height / 32.0f, false,
                                treeTransforms, treeTints, treeTints);
                        }
                        else
                        {
                            BatchBillboardPrimitives[member.Name] = new BatchBillboardPrimitive(graphics, texture, texture.Width, texture.Height,
                                new Point(0, 0), texture.Width / 32.0f, texture.Height / 32.0f, false,
                                new List<Matrix>() { treeTransforms[0] }, new List<Color>() { treeTints[0] }, new List<Color>() { treeTints[0] });
                        }
                    }

            }
        }
    }

}