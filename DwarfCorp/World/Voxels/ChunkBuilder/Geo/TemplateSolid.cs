using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Voxels.Geo
{
    public class TemplateSolid
    {
        public List<TemplateFace> Faces = new List<TemplateFace>();

        private static Dictionary<VoxelVertex, Geo.TemplateVertex> GetCubeVerticies()
        {
            return new Dictionary<VoxelVertex, Geo.TemplateVertex>
            {
                { VoxelVertex.FrontTopLeft,     new Geo.TemplateVertex { Position = new Vector3(0.0f, 1.0f, 1.0f), LogicalVertex = VoxelVertex.FrontTopLeft, ApplySlope = true } },
                { VoxelVertex.BackTopLeft,      new Geo.TemplateVertex { Position = new Vector3(0.0f, 1.0f, 0.0f), LogicalVertex = VoxelVertex.BackTopLeft, ApplySlope = true } },
                { VoxelVertex.FrontTopRight,    new Geo.TemplateVertex { Position = new Vector3(1.0f, 1.0f, 1.0f), LogicalVertex = VoxelVertex.FrontTopRight, ApplySlope = true } },
                { VoxelVertex.BackTopRight,     new Geo.TemplateVertex { Position = new Vector3(1.0f, 1.0f, 0.0f), LogicalVertex = VoxelVertex.BackTopRight, ApplySlope = true } },
                { VoxelVertex.FrontBottomLeft,  new Geo.TemplateVertex { Position = new Vector3(0.0f, 0.0f, 1.0f), LogicalVertex = VoxelVertex.FrontBottomLeft } },
                { VoxelVertex.BackBottomLeft,   new Geo.TemplateVertex { Position = new Vector3(0.0f, 0.0f, 0.0f), LogicalVertex = VoxelVertex.BackBottomLeft } },
                { VoxelVertex.FrontBottomRight, new Geo.TemplateVertex { Position = new Vector3(1.0f, 0.0f, 1.0f), LogicalVertex = VoxelVertex.FrontBottomRight } },
                { VoxelVertex.BackBottomRight,  new Geo.TemplateVertex { Position = new Vector3(1.0f, 0.0f, 0.0f), LogicalVertex = VoxelVertex.BackBottomRight } }
            };
        }

        public static TemplateSolid MakeCube()
        {
            var verts = GetCubeVerticies();

            return new TemplateSolid
            {
                Faces = new List<TemplateFace>
                {
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.Top,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontTopLeft], verts[VoxelVertex.BackTopLeft], verts[VoxelVertex.FrontTopRight], verts[VoxelVertex.BackTopRight]),
                        Edges = new TemplateEdge[]
                        {
                            new TemplateEdge { Start = 0, End = 1, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.North },
                            new TemplateEdge { Start = 1, End = 2, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.East },
                            new TemplateEdge { Start = 2, End = 3, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.South },
                            new TemplateEdge { Start = 3, End = 0, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.West }
                        }
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.South,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight])
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft])
                    },                    
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.FrontBottomLeft])
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft], verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft])
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight], verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight])
                    }
                }
            };
        }
    }
}