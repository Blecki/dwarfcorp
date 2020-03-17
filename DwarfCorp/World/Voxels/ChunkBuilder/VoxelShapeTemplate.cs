using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Voxels
{
    public class VoxelShapeTemplate
    {
        public List<VoxelFaceTemplate> Faces = new List<VoxelFaceTemplate>();

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

        public static VoxelShapeTemplate MakeCube()
        {
            var verts = GetCubeVerticies();

            return new VoxelShapeTemplate
            {
                Faces = new List<VoxelFaceTemplate>
                {
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.South,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight])
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.North,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft])
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.Top,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontTopLeft], verts[VoxelVertex.BackTopLeft], verts[VoxelVertex.FrontTopRight], verts[VoxelVertex.BackTopRight])
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.Bottom,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.FrontBottomLeft])
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.West,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.BackBottomLeft], verts[VoxelVertex.BackTopLeft], verts[VoxelVertex.FrontBottomLeft], verts[VoxelVertex.FrontTopLeft])
                    },
                    new VoxelFaceTemplate
                    {
                        Orientation = FaceOrientation.East,
                        Mesh = Geo.TemplateMesh.Quad(verts[VoxelVertex.FrontBottomRight], verts[VoxelVertex.FrontTopRight], verts[VoxelVertex.BackBottomRight], verts[VoxelVertex.BackTopRight])
                    }
                }
            };
        }
    }
}