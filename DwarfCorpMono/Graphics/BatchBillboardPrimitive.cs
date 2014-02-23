using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    
    public class BatchBillboardPrimitive : GeometricPrimitive
    {
        Texture2D SpriteSheet { get; set; }

        public BillboardPrimitive.BoardTextureCoords UVs { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public List<Matrix> m_relativeTransforms;
        public List<Color> m_tints;

        public BatchBillboardPrimitive(GraphicsDevice device,
                                       Texture2D spriteSheet,
                                       int frameWidth,
                                       int frameHeight,
                                       Point frame,
                                        float width, float height, bool flipped, List<Matrix> relativeTransforms, List<Color> tints)
        {
            m_relativeTransforms = relativeTransforms;
            UVs = new BillboardPrimitive.BoardTextureCoords(spriteSheet.Width, spriteSheet.Height, frameWidth, frameHeight, frame, flipped);
            Width = width;
            m_tints = tints;
            Height = height;
            CreateVerticies(Color.White);
            ResetBuffer(device);
        }


        protected void CreateVerticies(Color color)
        {
            m_vertices = new ExtendedVertex[m_relativeTransforms.Count * 6];

            // Calculate the position of the vertices on the top face.
            Vector3 topLeftFront = new Vector3(-0.5f * Width, 0.5f * Height, 0.0f);
            Vector3 topLeftBack = new Vector3(-0.5f * Width, 0.5f * Height, 0.0f);
            Vector3 topRightFront = new Vector3(0.5f * Width, 0.5f * Height, 0.0f);
            Vector3 topRightBack = new Vector3(0.5f * Width, 0.5f * Height, 0.0f);

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = new Vector3(-0.5f * Width, -0.5f * Height, 0.0f);
            Vector3 btmLeftBack = new Vector3(-0.5f * Width, -0.5f * Height, 0.0f);
            Vector3 btmRightFront = new Vector3(0.5f * Width, -0.5f * Height, 0.0f);
            Vector3 btmRightBack = new Vector3(0.5f * Width, -0.5f * Height, 0.0f);




            for (int i = 0; i < m_relativeTransforms.Count; i++)
            {
                // Add the vertices for the FRONT face.
                m_vertices[i * 6 + 0] = new ExtendedVertex(Vector3.Transform(topLeftFront, m_relativeTransforms[i]), m_tints[i], UVs.m_uvs[0], UVs.Bounds);
                m_vertices[i * 6 + 1] = new ExtendedVertex(Vector3.Transform(btmLeftFront, m_relativeTransforms[i]), m_tints[i], UVs.m_uvs[1], UVs.Bounds);
                m_vertices[i * 6 + 2] = new ExtendedVertex(Vector3.Transform(topRightFront, m_relativeTransforms[i]), m_tints[i], UVs.m_uvs[2], UVs.Bounds);
                m_vertices[i * 6 + 3] = new ExtendedVertex(Vector3.Transform(btmLeftFront, m_relativeTransforms[i]), m_tints[i], UVs.m_uvs[3], UVs.Bounds);
                m_vertices[i * 6 + 4] = new ExtendedVertex(Vector3.Transform(btmRightFront, m_relativeTransforms[i]), m_tints[i], UVs.m_uvs[4], UVs.Bounds);
                m_vertices[i * 6 + 5] = new ExtendedVertex(Vector3.Transform(topRightFront, m_relativeTransforms[i]), m_tints[i], UVs.m_uvs[5], UVs.Bounds);
            }
            //Console.Out.WriteLine("There are {0}", m_relativeTransforms.Count);

        }


    }
     
}
