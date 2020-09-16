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

        private static Dictionary<VoxelVertex, Geo.TemplateVertex> GetCubeVerticies(bool AllowSloping)
        {
            return new Dictionary<VoxelVertex, Geo.TemplateVertex>
            {
                { VoxelVertex.FrontTopLeft,     new Geo.TemplateVertex { Position = new Vector3(0.0f, 1.0f, 1.0f), LogicalVertex = VoxelVertex.FrontTopLeft, ApplySlope = AllowSloping } },
                { VoxelVertex.BackTopLeft,      new Geo.TemplateVertex { Position = new Vector3(0.0f, 1.0f, 0.0f), LogicalVertex = VoxelVertex.BackTopLeft, ApplySlope = AllowSloping } },
                { VoxelVertex.FrontTopRight,    new Geo.TemplateVertex { Position = new Vector3(1.0f, 1.0f, 1.0f), LogicalVertex = VoxelVertex.FrontTopRight, ApplySlope = AllowSloping } },
                { VoxelVertex.BackTopRight,     new Geo.TemplateVertex { Position = new Vector3(1.0f, 1.0f, 0.0f), LogicalVertex = VoxelVertex.BackTopRight, ApplySlope = AllowSloping } },
                { VoxelVertex.FrontBottomLeft,  new Geo.TemplateVertex { Position = new Vector3(0.0f, 0.0f, 1.0f), LogicalVertex = VoxelVertex.FrontBottomLeft } },
                { VoxelVertex.BackBottomLeft,   new Geo.TemplateVertex { Position = new Vector3(0.0f, 0.0f, 0.0f), LogicalVertex = VoxelVertex.BackBottomLeft } },
                { VoxelVertex.FrontBottomRight, new Geo.TemplateVertex { Position = new Vector3(1.0f, 0.0f, 1.0f), LogicalVertex = VoxelVertex.FrontBottomRight } },
                { VoxelVertex.BackBottomRight,  new Geo.TemplateVertex { Position = new Vector3(1.0f, 0.0f, 0.0f), LogicalVertex = VoxelVertex.BackBottomRight } }
            };
        }

        public static TemplateSolid MakeCube(bool AllowSloping, TemplateFaceShapes SideFaceShape)
        {
            var verts = GetCubeVerticies(AllowSloping);

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
                            new TemplateEdge { Start = 0, End = 1, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.South },
                            new TemplateEdge { Start = 1, End = 2, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.East },
                            new TemplateEdge { Start = 2, End = 3, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.North },
                            new TemplateEdge { Start = 3, End = 0, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.West }
                        },
                        Corners = new TemplateCorner[]
                        {
                            new TemplateCorner { Vertex = 0, EdgeType = EdgeType.Exterior, EdgeA = 3, EdgeB = 0 },
                            new TemplateCorner { Vertex = 1, EdgeType = EdgeType.Exterior, EdgeA = 0, EdgeB = 1 },
                            new TemplateCorner { Vertex = 2, EdgeType = EdgeType.Exterior, EdgeA = 1, EdgeB = 2 },
                            new TemplateCorner { Vertex = 3, EdgeType = EdgeType.Exterior, EdgeA = 2, EdgeB = 3 }
                        },
                        FaceShape = TemplateFaceShapes.Square
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight]),
                        FaceShape = SideFaceShape
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.South,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft]),
                        FaceShape = SideFaceShape
                    },                    
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.FrontBottomLeft]),
                        FaceShape = TemplateFaceShapes.Square
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft], verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft]),
                        FaceShape = SideFaceShape
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight], verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight]),
                        FaceShape = SideFaceShape
                    }
                }
            };
        }

        public static TemplateSolid MakeLowerSlab()
        {
            var verts = GetCubeVerticies(false);
            foreach (var v in verts)
                v.Value.Position = Vector3.Transform(v.Value.Position, Matrix.CreateScale(1.0f, 0.5f, 1.0f));

            // Todo - pass the face shape to the sides

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
                            new TemplateEdge { Start = 0, End = 1, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.South },
                            new TemplateEdge { Start = 1, End = 2, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.East },
                            new TemplateEdge { Start = 2, End = 3, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.North },
                            new TemplateEdge { Start = 3, End = 0, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.West }
                        },
                        Corners = new TemplateCorner[]
                        {
                            new TemplateCorner { Vertex = 0, EdgeType = EdgeType.Exterior, EdgeA = 3, EdgeB = 0 },
                            new TemplateCorner { Vertex = 1, EdgeType = EdgeType.Exterior, EdgeA = 0, EdgeB = 1 },
                            new TemplateCorner { Vertex = 2, EdgeType = EdgeType.Exterior, EdgeA = 1, EdgeB = 2 },
                            new TemplateCorner { Vertex = 3, EdgeType = EdgeType.Exterior, EdgeA = 2, EdgeB = 3 }
                        },
                        FaceShape = TemplateFaceShapes.Square,
                        CullType = FaceCullType.CannotCull
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.South,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.FrontBottomLeft]),
                        FaceShape = TemplateFaceShapes.Square
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft], verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight], verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    }
                }
            };
        }

        public static TemplateSolid MakeStairs()
        {
            var verts = GetCubeVerticies(false);
            foreach (var v in verts)
                v.Value.Position = Vector3.Transform(v.Value.Position, Matrix.CreateScale(1.0f, 0.5f, 1.0f));

            // Todo - pass the face shape to the sides

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
                            new TemplateEdge { Start = 0, End = 1, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.South },
                            new TemplateEdge { Start = 1, End = 2, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.East },
                            new TemplateEdge { Start = 2, End = 3, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.North },
                            new TemplateEdge { Start = 3, End = 0, EdgeType = EdgeType.Exterior, Orientation = FaceOrientation.West }
                        },
                        Corners = new TemplateCorner[]
                        {
                            new TemplateCorner { Vertex = 0, EdgeType = EdgeType.Exterior, EdgeA = 3, EdgeB = 0 },
                            new TemplateCorner { Vertex = 1, EdgeType = EdgeType.Exterior, EdgeA = 0, EdgeB = 1 },
                            new TemplateCorner { Vertex = 2, EdgeType = EdgeType.Exterior, EdgeA = 1, EdgeB = 2 },
                            new TemplateCorner { Vertex = 3, EdgeType = EdgeType.Exterior, EdgeA = 2, EdgeB = 3 }
                        },
                        FaceShape = TemplateFaceShapes.Square,
                        CullType = FaceCullType.CannotCull
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.South,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.FrontBottomLeft]),
                        FaceShape = TemplateFaceShapes.Square
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft], verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    },
                    new TemplateFace
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight], verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight]),
                        FaceShape = TemplateFaceShapes.LowerSlab
                    }
                }
            };
        }

    }
}