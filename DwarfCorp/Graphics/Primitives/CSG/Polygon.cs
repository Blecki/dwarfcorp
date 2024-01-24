using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Csg
{
	/// <summary>
	/// Convex polygons comprised of vertices lying on a plane.
	/// Each polygon also has "Shared" data which is any
	/// metadata (usually a material reference) that you need to
	/// share between sets of polygons.
	/// </summary>
	public class Polygon
	{
		public readonly List<Vertex> Vertices;
		public readonly Plane Plane;
		public readonly PolygonShared Shared;

		readonly bool debug = false;

		static readonly PolygonShared defaultShared = new PolygonShared(null);

		BoundingSphere cachedBoundingSphere;
		BoundingBox cachedBoundingBox;

		public Polygon(List<Vertex> vertices, PolygonShared shared = null, Plane plane = null)
		{
			Vertices = vertices;
			Shared = shared ?? defaultShared;
			Plane = plane ?? Plane.FromVector3Ds(vertices[0].Pos, vertices[1].Pos, vertices[2].Pos);
			if (debug)
			{
				//CheckIfConvex();
			}
		}

		public Polygon(params Vertex[] vertices)
			: this(new List<Vertex>(vertices))
		{
		}

		public BoundingSphere BoundingSphere
		{
			get
			{
				if (cachedBoundingSphere == null)
				{
					var box = BoundingBox;
					var middle = (box.Min + box.Max) * 0.5f;
					var radius3 = box.Max - middle;
					var radius = radius3.Length();
					cachedBoundingSphere = new BoundingSphere { Center = middle, Radius = radius };
				}
				return cachedBoundingSphere;
			}
		}

		public BoundingBox BoundingBox
		{
			get
			{
				if (cachedBoundingBox == null)
				{
					Vector3 minpoint, maxpoint;
					var vertices = this.Vertices;
					var numvertices = vertices.Count;
					if (numvertices == 0)
					{
						minpoint = new Vector3(0, 0, 0);
					}
					else {
						minpoint = vertices[0].Pos;
					}
					maxpoint = minpoint;
					for (var i = 1; i < numvertices; i++)
					{
						var point = vertices[i].Pos;
						minpoint = minpoint.Min(point);
						maxpoint = maxpoint.Max(point);
					}
					cachedBoundingBox = new BoundingBox(minpoint, maxpoint);
				}
				return cachedBoundingBox;
			}
		}

		public Polygon Flipped()
		{
			var newvertices = new List<Vertex>(Vertices.Count);
			for (int i = 0; i < Vertices.Count; i++)
			{
				newvertices.Add(Vertices[i].Flipped());
			}
			newvertices.Reverse();
			var newplane = Plane.Flipped();
			return new Polygon(newvertices, Shared, newplane);
		}
	}

	public class PolygonShared
	{
		int tag = 0;
		public int Tag {
			get {
				if (tag == 0) {
					tag = Solid.GetTag ();
				}
				return tag;
			}
		}
		public PolygonShared(object color)
		{			
		}
		public string Hash
		{
			get
			{
				return "null";
			}
		}
	}

	public class Properties
	{
		public readonly Dictionary<string, object> All = new Dictionary<string, object>();
		public Properties Merge(Properties otherproperties)
		{
			var result = new Properties();
			foreach (var x in All)
			{
				result.All.Add(x.Key, x.Value);
			}
			foreach (var x in otherproperties.All)
			{
				result.All[x.Key] = x.Value;
			}
			return result;
		}
		public Properties Transform(Matrix4x4 matrix4x4)
		{
			var result = new Properties();
			foreach (var x in All)
			{
				result.All.Add(x.Key, x.Value);
			}
			return result;
		}
	}
}

