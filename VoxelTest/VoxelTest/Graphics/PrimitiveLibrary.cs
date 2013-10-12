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
    public class PrimitiveLibrary
    {
        public static Dictionary<string, BoxPrimitive> BoxPrimitives = new Dictionary<string, BoxPrimitive>();
        public static Dictionary<string, BillboardPrimitive> BillboardPrimitives = new Dictionary<string, BillboardPrimitive>();
        public static Dictionary<string, BatchBillboardPrimitive> BatchBillboardPrimitives = new Dictionary<string, BatchBillboardPrimitive>();

        private static bool m_initialized = false;

        public PrimitiveLibrary(GraphicsDevice graphics, ContentManager content)
        {
            Initialize(graphics, content);
        }

        static void CreateIntersecting(string name, GraphicsDevice graphics, ContentManager content)
        {
            Texture2D bushSheet = content.Load<Texture2D>(name);
            List<Matrix> bushTransforms = new List<Matrix>();
            List<Color> bushColors = new List<Color>();
            bushTransforms.Add(Matrix.Identity);
            bushTransforms.Add(Matrix.CreateRotationY(1.57f));
            bushColors.Add(Color.White);
            bushColors.Add(Color.White);

            BatchBillboardPrimitives[name] = new BatchBillboardPrimitive(graphics, bushSheet, bushSheet.Width, bushSheet.Height, new Point(0, 0), 1.0f, 1.0f, false, bushTransforms, bushColors);

        }

        public void Initialize(GraphicsDevice graphics, ContentManager content)
        {
            if (!m_initialized)
            {
                Texture2D spriteSheet = content.Load<Texture2D>("bedtex");
                BoxPrimitive.BoxTextureCoords boxCoords = new BoxPrimitive.BoxTextureCoords(spriteSheet.Width, spriteSheet.Height, 
                                                        new BoxPrimitive.FaceData(new Rectangle(24, 0, 24, 16), false),
                                                        new BoxPrimitive.FaceData(new Rectangle(24, 72, 24, 16), false),
                                                        new BoxPrimitive.FaceData(new Rectangle(0, 24, 48, 24), true),
                                                        new BoxPrimitive.FaceData(new Rectangle(0, 0, 1, 1), false),
                                                        new BoxPrimitive.FaceData(new Rectangle(24, 24, 48, 16), false),
                                                        new BoxPrimitive.FaceData(new Rectangle(40, 24, 48, 16), false));
                BoxPrimitives["bed"] = new BoxPrimitive(graphics, 0.8f, 0.5f, 1.8f, boxCoords);
                m_initialized = false;


                Texture2D spriteSheetInterior = content.Load<Texture2D>("interior2_wilson");
                BoxPrimitive.BoxTextureCoords tableCoords = new BoxPrimitive.BoxTextureCoords(spriteSheetInterior.Width, spriteSheetInterior.Height, 32, 32,
                    new Point(3, 0),
                    new Point(3, 0),
                    new Point(2, 0),
                    new Point(3, 1),
                    new Point(3, 0),
                    new Point(3, 0));
                BoxPrimitives["table"] = new BoxPrimitive(graphics, 1.0f, 0.5f, 1.0f, tableCoords);
                m_initialized = false;


                BoxPrimitive.BoxTextureCoords chairCoords = new BoxPrimitive.BoxTextureCoords(spriteSheetInterior.Width, spriteSheetInterior.Height, 32, 32,
                    new Point(3, 5),
                    new Point(3, 5),
                    new Point(2, 5),
                    new Point(3, 1),
                    new Point(3, 5),
                    new Point(3, 5));
                BoxPrimitives["chair"] = new BoxPrimitive(graphics, 0.5f, 0.25f, 0.5f, chairCoords);
                m_initialized = false;

                Texture2D treeSheet = content.Load<Texture2D>("pine");
                List<Matrix> treeTransforms = new List<Matrix>();
                List<Color> treeTints = new List<Color>();
                treeTransforms.Add(Matrix.Identity);
                treeTransforms.Add(Matrix.CreateRotationY(1.57f));
                treeTints.Add(Color.White);
                treeTints.Add(Color.White);

                BatchBillboardPrimitives["tree"] = new BatchBillboardPrimitive(graphics, treeSheet, treeSheet.Width, treeSheet.Height, new Point(0, 0), 1.0f, 1.0f, false, treeTransforms, treeTints);


                CreateIntersecting("berrybush", graphics, content);
                CreateIntersecting("FrostGrass", graphics, content);
                CreateIntersecting("flower", graphics, content);
                CreateIntersecting("Grassthing", graphics, content);
                CreateIntersecting("deadbush", graphics, content);
                CreateIntersecting("shroom", graphics, content);
                CreateIntersecting("vine", graphics, content);
                CreateIntersecting("gnarled", graphics, content);


            }

            
        }

    }
}
