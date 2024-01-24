using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Csg
{
	public static class Solids
	{
		public static Solid FromRawPrimitive(DwarfCorp.RawPrimitive Prim)
        {
			var polygons = new List<Polygon>();
			for (var x = 0; x < Prim.IndexCount; ++x)
			{
				var poly = new Polygon(new Vertex[] {
					new Vertex(Prim.Vertices[x].Position, Prim.Vertices[x].TextureCoordinate),
					new Vertex(Prim.Vertices[x].Position, Prim.Vertices[x].TextureCoordinate),
					new Vertex(Prim.Vertices[x].Position, Prim.Vertices[x].TextureCoordinate)
				});
				polygons.Add(poly);
            }
			return Solid.FromPolygons(polygons);
        }

		public static Solid Cube(CubeOptions options)
		{
			var c = options.Center;
			var r = options.Radius.Abs(); // negative radii make no sense
			if (r.X == 0.0 || r.Y == 0.0 || r.Z == 0.0)
				return new Solid();
			var result = Solid.FromPolygons(cubeData.Select(info =>
			{
				//var normal = new Vector3D(info[1]);
				//var plane = new Plane(normal, 1);
				var vertices = info[0].Select(i =>
				{
					var pos = new Vector3(
						c.X + r.X * (2 * ((i & 1) != 0 ? 1 : 0) - 1),
							c.Y + r.Y * (2 * ((i & 2) != 0 ? 1 : 0) - 1),
							c.Z + r.Z * (2 * ((i & 4) != 0 ? 1 : 0) - 1));
					return NoTexVertex(pos);
				});
				return new Polygon(vertices.ToList());
			}).ToList());
			return result;
		}

		public static Solid Cube(float size = 1, bool center = false)
		{
			var r = new Vector3(size / 2, size / 2, size / 2);
			var c = center ? new Vector3(0, 0, 0) : r;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(float size, Vector3 center)
		{
			var r = new Vector3(size / 2, size / 2, size / 2);
			var c = center;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(Vector3 size, bool center = false)
		{
			var r = size / 2;
			var c = center ? new Vector3(0, 0, 0) : r;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(Vector3 size, Vector3 center)
		{
			var r = size / 2;
			var c = center;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(float width, float height, float depth, bool center = false)
		{
			var r = new Vector3(width/2, height/2, depth/2);
			var c = center ? new Vector3(0, 0, 0) : r;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Sphere(SphereOptions options)
		{
			var center = options.Center;
			var radius = Math.Abs(options.Radius);
			if (radius == 0.0)
				return new Solid();
			var resolution = options.Resolution;
			var xvector = options.XAxis * radius;
			var yvector = options.YAxis * radius;
			var zvector = options.ZAxis * radius;
			if (resolution < 4) resolution = 4;
			var qresolution = resolution / 4;
			var prevcylinderpoint = new Vector3(0,0,0);
			var polygons = new List<Polygon>();
			for (var slice1 = 0; slice1 <= resolution; slice1++)
			{
				var angle = Math.PI * 2.0 * slice1 / resolution;
				var cylinderpoint = xvector * ((float)Math.Cos(angle)) + (yvector * ((float)Math.Sin(angle)));
				if (slice1 > 0)
				{
					float prevcospitch = 0, prevsinpitch = 0;
					for (var slice2 = 0; slice2 <= qresolution; slice2++)
					{
						var pitch = (float)(0.5 * Math.PI * (double)slice2 / qresolution);
						var cospitch = (float)Math.Cos(pitch);
						var sinpitch = (float)Math.Sin(pitch);
						if (slice2 > 0)
						{
							var vertices = new List<Vertex>();
							vertices.Add(NoTexVertex(center + (prevcylinderpoint * (prevcospitch) - (zvector * (prevsinpitch)))));
							vertices.Add(NoTexVertex(center + (cylinderpoint * (prevcospitch) - (zvector * (prevsinpitch)))));
							if (slice2 < qresolution)
							{
								vertices.Add(NoTexVertex(center + (cylinderpoint * (cospitch) - (zvector * (sinpitch)))));
							}
							vertices.Add(NoTexVertex(center + (prevcylinderpoint * (cospitch) - (zvector * (sinpitch)))));
							polygons.Add(new Polygon(vertices));
							vertices = new List<Vertex>();
							vertices.Add(NoTexVertex(center + (prevcylinderpoint * (prevcospitch) + (zvector * (prevsinpitch)))));
							vertices.Add(NoTexVertex(center + (cylinderpoint * (prevcospitch) + (zvector * (prevsinpitch)))));
							if (slice2 < qresolution)
							{
								vertices.Add(NoTexVertex(center + (cylinderpoint * (cospitch) + (zvector * (sinpitch)))));
							}
							vertices.Add(NoTexVertex(center + (prevcylinderpoint * (cospitch) + (zvector * (sinpitch)))));
							vertices.Reverse();
							polygons.Add(new Polygon(vertices));
						}
						prevcospitch = cospitch;
						prevsinpitch = sinpitch;
					}
				}
				prevcylinderpoint = cylinderpoint;
			}
			var result = Solid.FromPolygons(polygons);
			return result;
		}

		public static Solid Sphere(float r = 1, bool center = true)
		{
			var c = center ? new Vector3(0, 0, 0) : new Vector3(r, r, r);
			return Solids.Sphere(new SphereOptions { Radius = r, Center = c });
		}

		public static Solid Sphere(float r, Vector3 center)
		{
			return Solids.Sphere(new SphereOptions { Radius = r, Center = center });
		}

		public static Solid Cylinder(CylinderOptions options)
		{
			var s = options.Start;
			var e = options.End;
			var r = Math.Abs(options.RadiusStart);
			var rEnd = Math.Abs(options.RadiusEnd);
			var rStart = r;
			var alpha = options.SectorAngle;
			alpha = alpha > 360 ? alpha % 360 : alpha;

			if ((rEnd == 0) && (rStart == 0))
			{
				return new Solid();
			}
			if (s.Equals(e))
			{
				return new Solid();
			}

			var slices = options.Resolution;
			var ray = e - (s);
			var axisZ = Vector3.Normalize(ray);
			var axisX = Vector3.Normalize(axisZ.RandomNonParallelVector());

			var axisY = Vector3.Normalize(Vector3.Cross(axisX, axisZ));
			axisX = Vector3.Normalize(Vector3.Cross(axisZ, axisY));
			var start = NoTexVertex(s);
			var end = NoTexVertex(e);
			var polygons = new List<Polygon>();

			Func<float, float, float, Vertex> point = (stack, slice, radius) =>
			{
				var angle = (float)(slice * Math.PI * alpha / 180);
				var outp = (axisX * (float)(Math.Cos(angle)) + (axisY * (float)(Math.Sin(angle))));
				var pos = s + (ray * (stack)) + (outp * (radius));
				return NoTexVertex(pos);
			};

			if (alpha > 0)
			{
				for (var i = 0; i < slices; i++)
				{
					float t0 = (float)i / slices;
					float t1 = (float)(i + 1) / slices;
					if (rEnd == rStart)
					{
						polygons.Add(new Polygon(start, point(0, t0, rEnd), point(0, t1, rEnd)));
						polygons.Add(new Polygon(point(0, t1, rEnd), point(0, t0, rEnd), point(1, t0, rEnd), point(1, t1, rEnd)));
						polygons.Add(new Polygon(end, point(1, t1, rEnd), point(1, t0, rEnd)));
					}
					else {
						if (rStart > 0)
						{
							polygons.Add(new Polygon(start, point(0, t0, rStart), point(0, t1, rStart)));
							polygons.Add(new Polygon(point(0, t0, rStart), point(1, t0, rEnd), point(0, t1, rStart)));
						}
						if (rEnd > 0)
						{
							polygons.Add(new Polygon(end, point(1, t1, rEnd), point(1, t0, rEnd)));
							polygons.Add(new Polygon(point(1, t0, rEnd), point(1, t1, rEnd), point(0, t1, rStart)));
						}
					}
				}
	            if (alpha < 360) {
	                polygons.Add(new Polygon(start, end, point(0, 0, rStart)));
	                polygons.Add(new Polygon(point(0, 0, rStart), end, point(1, 0, rEnd)));
	                polygons.Add(new Polygon(start, point(0, 1, rStart), end));
	                polygons.Add(new Polygon(point(0, 1, rStart), point(1, 1, rEnd), end));
	            }
			}
			var result = Solid.FromPolygons(polygons);
	        return result;
	    }

		public static Solid Cylinder(float r, float h, bool center = false)
		{
			var start = center ? new Vector3(0, -h / 2, 0) : new Vector3(0, 0, 0);
			var end = center ? new Vector3(0, h / 2, 0) : new Vector3(0, h, 0);
			return Cylinder(new CylinderOptions { Start = start, End = end, RadiusStart = r, RadiusEnd = r, });
		}

		public static Solid Union(params Solid[] csgs)
		{
			if (csgs.Length == 0)
			{
				return new Solid();
			}
			else if (csgs.Length == 1)
			{
				return csgs[0];
			}
			else
			{
				var head = csgs[0];
				var rest = csgs.Skip(1).ToArray();
				return head.Union(rest);
			}
		}

		public static Solid Difference(params Solid[] csgs)
		{
			if (csgs.Length == 0)
			{
				return new Solid();
			}
			else if (csgs.Length == 1)
			{
				return csgs[0];
			}
			else
			{
				var head = csgs[0];
				var rest = csgs.Skip(1).ToArray();
				return head.Substract(rest);
			}
		}

		public static Solid Intersection(params Solid[] csgs)
		{
			if (csgs.Length == 0 || csgs.Length == 1)
			{
				return new Solid();
			}
			else
			{
				var head = csgs[0];
				var rest = csgs.Skip(1).ToArray();
				return head.Intersect(rest);
			}
		}

		static Vertex NoTexVertex (Vector3 pos) => new Vertex (pos, new Vector2 (0, 0));

		static readonly int[][][] cubeData =
			{
				new[] {
					new[] { 0, 4, 6, 2 },
					new[] { -1, 0, 0 }
				},
				new[] {
					new[] {1, 3, 7, 5},
					new[] {+1, 0, 0}
				},
				new[] {
					new[] {0, 1, 5, 4},
					new[] {0, -1, 0},
				},
				new[] {
					new[] {2, 6, 7, 3},
					new[] { 0, +1, 0}
				},
				new[] {
					new[] {0, 2, 3, 1},
					new[] { 0, 0, -1}
				},
				new[] {
					new[] {4, 5, 7, 6},
					new[] { 0, 0, +1}
				}
			};
	}

	public class CubeOptions
	{
		public Vector3 Center;
		public Vector3 Radius = new Vector3(1, 1, 1);
	}

	public class SphereOptions
	{
		public Vector3 XAxis = new Vector3(1, 0, 0);
		public Vector3 YAxis = new Vector3(0, -1, 0);
		public Vector3 ZAxis = new Vector3(0, 0, 1);
		public Vector3 Center;
		public float Radius = 1;
		public int Resolution = Solid.DefaultResolution3D;
	}

	public class CylinderOptions
	{
		public Vector3 Start;
		public Vector3 End;
		public float RadiusStart = 1;
		public float RadiusEnd = 1;
		public float SectorAngle = 360;
		public int Resolution = Solid.DefaultResolution3D;
	}
}

