using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Csg
{
	public class Solid
	{
		static readonly PolygonsPerPlaneKeyComparer polygonsPerPlaneKeyComparer = new PolygonsPerPlaneKeyComparer ();

		public List<Polygon> Polygons;

		public bool IsCanonicalized;
		public bool IsRetesselated;

		public const int DefaultResolution2D = 32;
		public const int DefaultResolution3D = 12;

		BoundingBox cachedBoundingBox;

		public Solid ()
		{
			Polygons = new List<Polygon>();
			IsCanonicalized = true;
			IsRetesselated = true;
		}

		public static Solid FromPolygons(List<Polygon> polygons)
		{
			var csg = new Solid();
			csg.Polygons = polygons;
			csg.IsCanonicalized = false;
			csg.IsRetesselated = false;
			return csg;
		}

		public Solid Union(params Solid[] others)
		{
			var csgs = new List<Solid>();
			csgs.Add(this);
			csgs.AddRange(others);
			var i = 1;
			for (; i < csgs.Count; i += 2)
			{
				var n = csgs[i - 1].UnionSub(csgs[i], false, false);
				csgs.Add(n);
			}
			return csgs[i - 1].Retesselated().Canonicalized();
		}

		Solid UnionSub(Solid csg, bool retesselate, bool canonicalize)
		{
			if (!MayOverlap(csg))
			{
				return UnionForNonIntersecting(csg);
			}
			else {
				var a = new Tree(Bounds, Polygons);
				var b = new Tree(csg.Bounds, csg.Polygons);

				a.ClipTo(b, false);
				b.ClipTo(a);
				b.Invert();
				b.ClipTo(a);
				b.Invert();

				var newpolygons = new List<Polygon>(a.AllPolygons());
				newpolygons.AddRange(b.AllPolygons());
				var result = Solid.FromPolygons(newpolygons);
				if (retesselate) result = result.Retesselated();
				if (canonicalize) result = result.Canonicalized();
				return result;
			}
		}

		Solid UnionForNonIntersecting(Solid csg)
		{
			var newpolygons = new List<Polygon>(Polygons);
			newpolygons.AddRange(csg.Polygons);
			var result = Solid.FromPolygons(newpolygons);
			result.IsCanonicalized = IsCanonicalized && csg.IsCanonicalized;
			result.IsRetesselated = IsRetesselated && csg.IsRetesselated;
			return result;
		}

		public Solid Substract(params Solid[] csgs)
		{
			Solid result = this;
			for (var i = 0; i < csgs.Length; i++)
			{
				var islast = (i == (csgs.Length - 1));
				result = result.SubtractSub(csgs[i], islast, islast);
			}
			return result;
		}

		Solid SubtractSub(Solid csg, bool retesselate, bool canonicalize)
		{
			var a = new Tree(Bounds, Polygons);
			var b = new Tree(csg.Bounds, csg.Polygons);

			a.Invert();
			a.ClipTo(b);
			b.ClipTo(a, true);
			a.AddPolygons(b.AllPolygons());
			a.Invert();

			var result = Solid.FromPolygons(a.AllPolygons());
			if (retesselate) result = result.Retesselated();
			if (canonicalize) result = result.Canonicalized();
			return result;
		}

		public Solid Intersect(params Solid[] csgs)
		{
			var result = this;
			for (var i = 0; i < csgs.Length; i++)
			{
				var islast = (i == (csgs.Length - 1));
				result = result.IntersectSub(csgs[i], islast, islast);
			}
			return result;
		}

		Solid IntersectSub(Solid csg, bool retesselate, bool canonicalize)
		{
			var a = new Tree(Bounds, Polygons);
			var b = new Tree(csg.Bounds, csg.Polygons);

			a.Invert();
			b.ClipTo(a);
			b.Invert();
			a.ClipTo(b);
			b.ClipTo(a);
			a.AddPolygons(b.AllPolygons());
			a.Invert();

			var result = Solid.FromPolygons(a.AllPolygons());
			if (retesselate) result = result.Retesselated();
			if (canonicalize) result = result.Canonicalized();
			return result;
		}

		public Solid Transform(Matrix matrix4x4)
		{
			var ismirror = false;// matrix4x4.IsMirroring;
			var transformedvertices = new Dictionary<int, Vertex>();
			var transformedplanes = new Dictionary<int, Plane>();
			var newpolygons = new List<Polygon>();
			foreach (var p in Polygons)
			{
				Plane newplane;
				var plane = p.Plane;
				var planetag = plane.Tag;
				if (transformedplanes.ContainsKey(planetag)) {
					newplane = transformedplanes[planetag];
				} else {
					newplane = plane.Transform(matrix4x4);
					transformedplanes[planetag] = newplane;
				}
				var newvertices = new List<Vertex>();
				foreach (var v in p.Vertices) {
					Vertex newvertex;
					var vertextag = v.Tag;
					if (transformedvertices.ContainsKey(vertextag)) {
						newvertex = transformedvertices[vertextag];
					} else {
						newvertex = v.Transform(matrix4x4);
						transformedvertices[vertextag] = newvertex;
					}
					newvertices.Add(newvertex);
				}
				if (ismirror) newvertices.Reverse();
				newpolygons.Add(new Polygon(newvertices, p.Shared, newplane));
			}
			var result = Solid.FromPolygons(newpolygons);
			result.IsRetesselated = this.IsRetesselated;
			result.IsCanonicalized = this.IsCanonicalized;
			return result;
		}

		public Solid Translate(Vector3 offset)
		{
			return Transform(Matrix.CreateTranslation(offset));
		}

		public Solid Translate(float x = 0, float y = 0, float z = 0)
		{
			return Transform(Matrix.CreateTranslation(new Vector3(x, y, z)));
		}

		public Solid Scale(Vector3 scale)
		{
			return Transform(Matrix.CreateScale(scale));
		}

		public Solid Scale(float scale)
		{
			return Transform(Matrix.CreateScale(new Vector3(scale, scale, scale)));
		}

		public Solid Scale(float x, float y, float z)
		{
			return Transform(Matrix.CreateScale(new Vector3(x, y, z)));
		}

		Solid Canonicalized()
		{
			if (IsCanonicalized)
			{
				return this;
			}
			else {
				var factory = new FuzzyCsgFactory();
				var result = factory.GetCsg(this);
				result.IsCanonicalized = true;
				result.IsRetesselated = IsRetesselated;
				return result;
			}
		}

		Solid Retesselated()
		{
			if (IsRetesselated)
			{
				return this;
			}
			else {
				var csg = this;
				var polygonsPerPlane = new Dictionary<PolygonsPerPlaneKey, List<Polygon>>(polygonsPerPlaneKeyComparer);
				var isCanonicalized = csg.IsCanonicalized;
				var fuzzyFactory = new FuzzyCsgFactory();
				foreach (var polygon in csg.Polygons)
				{
					var plane = polygon.Plane;
					var shared = polygon.Shared;
					if (!isCanonicalized)
					{
						plane = fuzzyFactory.GetPlane(plane);
						shared = fuzzyFactory.GetPolygonShared(shared);
					}
					var tag = new PolygonsPerPlaneKey { PlaneTag = plane.Tag, SharedTag = shared.Tag };
					List<Polygon> ppp;
					if (polygonsPerPlane.TryGetValue(tag, out ppp))
					{
						ppp.Add(polygon);
					}
					else {
						ppp = new List<Polygon>(1);
						ppp.Add(polygon);
						polygonsPerPlane.Add(tag, ppp);
					}
				}
				var destpolygons = new List<Polygon> ();
				//var retess = new List<PlanePolygons> ();
				foreach (var planetag in polygonsPerPlane)
				{
					var sourcepolygons = planetag.Value;
					if (sourcepolygons.Count < 2)
					{
						destpolygons.AddRange(sourcepolygons);
					}
					else {
						var retesselatedpolygons = new List<Polygon>(sourcepolygons.Count);
						//retess.Add (new PlanePolygons { Source = sourcepolygons, Retesselated = retesselatedpolygons });
						Solid.RetesselateCoplanarPolygons(sourcepolygons, retesselatedpolygons);
						destpolygons.AddRange(retesselatedpolygons);
					}
				}
				//System.Threading.Tasks.Parallel.ForEach (retess, x => {
				//	Solid.RetesselateCoplanarPolygons (x.Source, x.Retesselated);
				//});
				//foreach (var x in retess) {
				//	destpolygons.AddRange (x.Retesselated);
				//}

				var result = Solid.FromPolygons(destpolygons);
				result.IsRetesselated = true;
				return result;
			}
		}

		//struct PlanePolygons
		//{
		//	public List<Polygon> Source;
		//	public List<Polygon> Retesselated;
		//}

		struct PolygonsPerPlaneKey
		{
			public int PlaneTag;
			public int SharedTag;
		}

		class PolygonsPerPlaneKeyComparer : IEqualityComparer<PolygonsPerPlaneKey>
		{
			public bool Equals (PolygonsPerPlaneKey x, PolygonsPerPlaneKey y)
			{
				return x.PlaneTag == y.PlaneTag &&
					   x.SharedTag == y.SharedTag;
			}

			public int GetHashCode (PolygonsPerPlaneKey obj)
			{
				var hashCode = -981392073;
				hashCode = hashCode * -1521134295 + obj.PlaneTag.GetHashCode ();
				hashCode = hashCode * -1521134295 + obj.SharedTag.GetHashCode ();
				return hashCode;
			}
		}

		BoundingBox Bounds
		{
			get
			{
				if (cachedBoundingBox == null)
				{
					var minpoint = new Vector3(0, 0, 0);
					var maxpoint = new Vector3(0, 0, 0);
					var polygons = this.Polygons;
					var numpolygons = polygons.Count;
					for (var i = 0; i < numpolygons; i++)
					{
						var polygon = polygons[i];
						var bounds = polygon.BoundingBox;
						if (i == 0)
						{
							minpoint = bounds.Min;
							maxpoint = bounds.Max;
						}
						else {
							minpoint = minpoint.Min(bounds.Min);
							maxpoint = maxpoint.Max(bounds.Max);
						}
					}
					cachedBoundingBox = new BoundingBox(minpoint, maxpoint);
				}
				return cachedBoundingBox;
			}
		}

		bool MayOverlap(Solid csg)
		{
			if ((this.Polygons.Count == 0) || (csg.Polygons.Count == 0))
			{
				return false;
			}
			else
			{
				var mybounds = Bounds;
				var otherbounds = csg.Bounds;
				if (mybounds.Max.X < otherbounds.Min.X) return false;
				if (mybounds.Min.X > otherbounds.Max.X) return false;
				if (mybounds.Max.Y < otherbounds.Min.Y) return false;
				if (mybounds.Min.Y > otherbounds.Max.Y) return false;
				if (mybounds.Max.Z < otherbounds.Min.Z) return false;
				if (mybounds.Min.Z > otherbounds.Max.Z) return false;
				return true;
			}
		}

		static void RetesselateCoplanarPolygons(List<Polygon> sourcepolygons, List<Polygon> destpolygons)
		{
			var EPS = 1e-5;

			var numpolygons = sourcepolygons.Count;
			if (numpolygons > 0)
			{
				var plane = sourcepolygons[0].Plane;
				var shared = sourcepolygons[0].Shared;
				var orthobasis = new OrthoNormalBasis(plane);
				var polygonvertices2d = new List<List<Vertex2D>>(); // array of array of Vertex2Ds
				var polygontopvertexindexes = new List<int>(); // array of indexes of topmost vertex per polygon
				var topy2polygonindexes = new Dictionary<float, List<int>>();
				var ycoordinatetopolygonindexes = new Dictionary<float, HashSet<int>>();

				//var xcoordinatebins = new Dictionary<double, double>();
				var ycoordinatebins = new Dictionary<float, float>();

				// convert all polygon vertices to 2D
				// Make a list of all encountered y coordinates
				// And build a map of all polygons that have a vertex at a certain y coordinate:
				var ycoordinateBinningFactor = 1.0 / EPS * 10;
				for (var polygonindex = 0; polygonindex < numpolygons; polygonindex++)
				{
					var poly3d = sourcepolygons[polygonindex];
					var vertices2d = new List<Vertex2D> ();
					var numvertices = poly3d.Vertices.Count;
					var minindex = -1;
					if (numvertices > 0)
					{
						float miny = 0, maxy = 0;
						//int maxindex;
						for (var i = 0; i < numvertices; i++)
						{
							var pos2d = orthobasis.To2D(poly3d.Vertices[i].Pos);
							// perform binning of y coordinates: If we have multiple vertices very
							// close to each other, give them the same y coordinate:
							var ycoordinatebin = (float)Math.Floor(pos2d.Y * ycoordinateBinningFactor);
							float newy;
							if (ycoordinatebins.ContainsKey(ycoordinatebin))
							{
								newy = ycoordinatebins[ycoordinatebin];
							}
							else if (ycoordinatebins.ContainsKey(ycoordinatebin + 1))
							{
								newy = ycoordinatebins[ycoordinatebin + 1];
							}
							else if (ycoordinatebins.ContainsKey(ycoordinatebin - 1))
							{
								newy = ycoordinatebins[ycoordinatebin - 1];
							}
							else {
								newy = pos2d.Y;
								ycoordinatebins[ycoordinatebin] = pos2d.Y;
							}
							pos2d = new Vector2(pos2d.X, newy);
							vertices2d.Add(new Vertex2D (pos2d, poly3d.Vertices[i].Tex));
							var y = pos2d.Y;
							if ((i == 0) || (y < miny))
							{
								miny = y;
								minindex = i;
							}
							if ((i == 0) || (y > maxy))
							{
								maxy = y;
								//maxindex = i;
							}
							if (!(ycoordinatetopolygonindexes.ContainsKey(y)))
							{
								ycoordinatetopolygonindexes[y] = new HashSet<int>();
							}
							ycoordinatetopolygonindexes[y].Add(polygonindex);
						}
						if (miny >= maxy)
						{
							// degenerate polygon, all vertices have same y coordinate. Just ignore it from now:
							vertices2d = new List<Vertex2D> ();
							numvertices = 0;
							minindex = -1;
						}
						else {
							if (!(topy2polygonindexes.ContainsKey(miny)))
							{
								topy2polygonindexes[miny] = new List<int>();
							}
							topy2polygonindexes[miny].Add(polygonindex);
						}
					} // if(numvertices > 0)
					  // reverse the vertex order:
					vertices2d.Reverse();
					minindex = numvertices - minindex - 1;
					polygonvertices2d.Add(vertices2d);
					polygontopvertexindexes.Add(minindex);
				}
				var ycoordinates = new List<float>();
				foreach (var ycoordinate in ycoordinatetopolygonindexes) ycoordinates.Add(ycoordinate.Key);
				ycoordinates.Sort();

				// Now we will iterate over all y coordinates, from lowest to highest y coordinate
				// activepolygons: source polygons that are 'active', i.e. intersect with our y coordinate
				//   Is sorted so the polygons are in left to right order
				// Each element in activepolygons has these properties:
				//        polygonindex: the index of the source polygon (i.e. an index into the sourcepolygons
				//                      and polygonvertices2d arrays)
				//        leftvertexindex: the index of the vertex at the left side of the polygon (lowest x)
				//                         that is at or just above the current y coordinate
				//        rightvertexindex: dito at right hand side of polygon
				//        topleft, bottomleft: coordinates of the left side of the polygon crossing the current y coordinate
				//        topright, bottomright: coordinates of the right hand side of the polygon crossing the current y coordinate
				var activepolygons = new List<RetesselateActivePolygon>();
				var prevoutpolygonrow = new List<RetesselateActivePolygon>();
				for (var yindex = 0; yindex < ycoordinates.Count; yindex++)
				{
					var newoutpolygonrow = new List<RetesselateActivePolygon>();
					var ycoordinate = ycoordinates[yindex];
					//var ycoordinate_as_string = ycoordinates + "";

					// update activepolygons for this y coordinate:
					// - Remove any polygons that end at this y coordinate
					// - update leftvertexindex and rightvertexindex (which point to the current vertex index
					//   at the the left and right side of the polygon
					// Iterate over all polygons that have a corner at this y coordinate:
					var polygonindexeswithcorner = ycoordinatetopolygonindexes[ycoordinate];
					for (var activepolygonindex = 0; activepolygonindex < activepolygons.Count; ++activepolygonindex)
					{
						var activepolygon = activepolygons[activepolygonindex];
						var polygonindex = activepolygon.polygonindex;
						if (polygonindexeswithcorner.Contains(polygonindex))
						{
							// this active polygon has a corner at this y coordinate:
							var vertices2d = polygonvertices2d[polygonindex];
							var numvertices = vertices2d.Count;
							var newleftvertexindex = activepolygon.leftvertexindex;
							var newrightvertexindex = activepolygon.rightvertexindex;
							// See if we need to increase leftvertexindex or decrease rightvertexindex:
							while (true)
							{
								var nextleftvertexindex = newleftvertexindex + 1;
								if (nextleftvertexindex >= numvertices) nextleftvertexindex = 0;
								if (vertices2d[nextleftvertexindex].Pos.Y != ycoordinate) break;
								newleftvertexindex = nextleftvertexindex;
							}
							var nextrightvertexindex = newrightvertexindex - 1;
							if (nextrightvertexindex < 0) nextrightvertexindex = numvertices - 1;
							if (vertices2d[nextrightvertexindex].Pos.Y == ycoordinate)
							{
								newrightvertexindex = nextrightvertexindex;
							}
							if ((newleftvertexindex != activepolygon.leftvertexindex) && (newleftvertexindex == newrightvertexindex))
							{
								// We have increased leftvertexindex or decreased rightvertexindex, and now they point to the same vertex
								// This means that this is the bottom point of the polygon. We'll remove it:
								activepolygons.RemoveAt(activepolygonindex);
								--activepolygonindex;
							}
							else {
								activepolygon.leftvertexindex = newleftvertexindex;
								activepolygon.rightvertexindex = newrightvertexindex;
								activepolygon.topleft = vertices2d[newleftvertexindex];
								activepolygon.topright = vertices2d[newrightvertexindex];
								var nextleftvertexindex = newleftvertexindex + 1;
								if (nextleftvertexindex >= numvertices) nextleftvertexindex = 0;
								activepolygon.bottomleft = vertices2d[nextleftvertexindex];
								nextrightvertexindex = newrightvertexindex - 1;
								if (nextrightvertexindex < 0) nextrightvertexindex = numvertices - 1;
								activepolygon.bottomright = vertices2d[nextrightvertexindex];
							}
						} // if polygon has corner here
					} // for activepolygonindex
					float nextycoordinate;
					if (yindex >= ycoordinates.Count - 1)
					{
						// last row, all polygons must be finished here:
						activepolygons = new List<RetesselateActivePolygon>();
						nextycoordinate = 0.0f;
					}
					else // yindex < ycoordinates.length-1
					{
						nextycoordinate = ycoordinates[yindex + 1];
						var middleycoordinate = (float)(0.5 * (ycoordinate + nextycoordinate));
						// update activepolygons by adding any polygons that start here:
						List<int> startingpolygonindexes;
						if (topy2polygonindexes.TryGetValue(ycoordinate, out startingpolygonindexes))
						{
							foreach (var polygonindex in startingpolygonindexes)
							{
								var vertices2d = polygonvertices2d[polygonindex];
								var numvertices = vertices2d.Count;
								var topvertexindex = polygontopvertexindexes[polygonindex];
								// the top of the polygon may be a horizontal line. In that case topvertexindex can point to any point on this line.
								// Find the left and right topmost vertices which have the current y coordinate:
								var topleftvertexindex = topvertexindex;
								while (true)
								{
									var i = topleftvertexindex + 1;
									if (i >= numvertices) i = 0;
									if (vertices2d[i].Pos.Y != ycoordinate) break;
									if (i == topvertexindex) break; // should not happen, but just to prevent endless loops
									topleftvertexindex = i;
								}
								var toprightvertexindex = topvertexindex;
								while (true)
								{
									var i = toprightvertexindex - 1;
									if (i < 0) i = numvertices - 1;
									if (vertices2d[i].Pos.Y != ycoordinate) break;
									if (i == topleftvertexindex) break; // should not happen, but just to prevent endless loops
									toprightvertexindex = i;
								}
								var nextleftvertexindex = topleftvertexindex + 1;
								if (nextleftvertexindex >= numvertices) nextleftvertexindex = 0;
								var nextrightvertexindex = toprightvertexindex - 1;
								if (nextrightvertexindex < 0) nextrightvertexindex = numvertices - 1;
								var newactivepolygon = new RetesselateActivePolygon
								{
									polygonindex = polygonindex,
									leftvertexindex = topleftvertexindex,
									rightvertexindex = toprightvertexindex,
									topleft = vertices2d[topleftvertexindex],
									topright = vertices2d[toprightvertexindex],
									bottomleft = vertices2d[nextleftvertexindex],
									bottomright = vertices2d[nextrightvertexindex],
								};

								InsertSorted(activepolygons, newactivepolygon, (el1, el2) =>
								{
									var x1 = InterpolateBetween2DPointsForY(
										el1.topleft, el1.bottomleft, middleycoordinate);
									var x2 = InterpolateBetween2DPointsForY(
										el2.topleft, el2.bottomleft, middleycoordinate);
									if (x1.Result > x2.Result) return 1;
									if (x1.Result < x2.Result) return -1;
									return 0;
								});
							} // for(var polygonindex in startingpolygonindexes)
						}
					} //  yindex < ycoordinates.length-1
					  //if( (yindex == ycoordinates.length-1) || (nextycoordinate - ycoordinate > EPS) )
					if (true)
					{
						// Now activepolygons is up to date
						// Build the output polygons for the next row in newoutpolygonrow:
						for (var activepolygon_key = 0; activepolygon_key < activepolygons.Count; activepolygon_key++)
						{
							var activepolygon = activepolygons[activepolygon_key];
							var polygonindex = activepolygon.polygonindex;
							var vertices2d = polygonvertices2d[polygonindex];
							var numvertices = vertices2d.Count;

							var x = InterpolateBetween2DPointsForY(activepolygon.topleft, activepolygon.bottomleft, ycoordinate);
							var topleft = new Vertex2D(x.Result, ycoordinate, x.Tex);
							x = InterpolateBetween2DPointsForY(activepolygon.topright, activepolygon.bottomright, ycoordinate);
							var topright = new Vertex2D(x.Result, ycoordinate, x.Tex);
							x = InterpolateBetween2DPointsForY(activepolygon.topleft, activepolygon.bottomleft, nextycoordinate);
							var bottomleft = new Vertex2D(x.Result, nextycoordinate, x.Tex);
							x = InterpolateBetween2DPointsForY(activepolygon.topright, activepolygon.bottomright, nextycoordinate);
							var bottomright = new Vertex2D(x.Result, nextycoordinate, x.Tex);
							var outpolygon = new RetesselateActivePolygon
							{
								topleft = topleft,
								topright = topright,
								bottomleft = bottomleft,
								bottomright = bottomright,
								leftline = Line2D.FromPoints(topleft.Pos, bottomleft.Pos),
								rightline = Line2D.FromPoints(bottomright.Pos, topright.Pos)
							};
							if (newoutpolygonrow.Count > 0)
							{
								var prevoutpolygon = newoutpolygonrow[newoutpolygonrow.Count - 1];
								var d1 = outpolygon.topleft.Pos.DistanceTo(prevoutpolygon.topright.Pos);
								var d2 = outpolygon.bottomleft.Pos.DistanceTo(prevoutpolygon.bottomright.Pos);
								if ((d1 < EPS) && (d2 < EPS))
								{
									// we can join this polygon with the one to the left:
									outpolygon.topleft = prevoutpolygon.topleft;
									outpolygon.leftline = prevoutpolygon.leftline;
									outpolygon.bottomleft = prevoutpolygon.bottomleft;
									newoutpolygonrow.RemoveAt(newoutpolygonrow.Count - 1);
								}
							}
							newoutpolygonrow.Add(outpolygon);
						} // for(activepolygon in activepolygons)
						if (yindex > 0)
						{
							// try to match the new polygons against the previous row:
							var prevcontinuedindexes = new HashSet<int>();
							var matchedindexes = new HashSet<int>();
							for (var i = 0; i < newoutpolygonrow.Count; i++)
							{
								var thispolygon = newoutpolygonrow[i];
								if (thispolygon.leftline != null && thispolygon.rightline != null) {
									for (var ii = 0; ii < prevoutpolygonrow.Count; ii++) {
										if (!matchedindexes.Contains (ii)) // not already processed?
										{
											// We have a match if the sidelines are equal or if the top coordinates
											// are on the sidelines of the previous polygon
											var prevpolygon = prevoutpolygonrow[ii];
											if (prevpolygon.leftline != null && prevpolygon.rightline != null && prevpolygon.bottomleft.Pos.DistanceTo (thispolygon.topleft.Pos) < EPS) {
												if (prevpolygon.bottomright.Pos.DistanceTo (thispolygon.topright.Pos) < EPS) {
													// Yes, the top of this polygon matches the bottom of the previous:
													matchedindexes.Add (ii);
													// Now check if the joined polygon would remain convex:
													var d1 = thispolygon.leftline.Direction.X - prevpolygon.leftline.Direction.X;
													var d2 = thispolygon.rightline.Direction.X - prevpolygon.rightline.Direction.X;
													var leftlinecontinues = Math.Abs (d1) < EPS;
													var rightlinecontinues = Math.Abs (d2) < EPS;
													var leftlineisconvex = leftlinecontinues || (d1 >= 0);
													var rightlineisconvex = rightlinecontinues || (d2 >= 0);
													if (leftlineisconvex && rightlineisconvex) {
														// yes, both sides have convex corners:
														// This polygon will continue the previous polygon
														thispolygon.outpolygon = prevpolygon.outpolygon;
														thispolygon.leftlinecontinues = leftlinecontinues;
														thispolygon.rightlinecontinues = rightlinecontinues;
														prevcontinuedindexes.Add (ii);
													}
													break;
												}
											}
										} // if(!prevcontinuedindexes[ii])
									} // for ii
								}
							} // for i
							for (var ii = 0; ii < prevoutpolygonrow.Count; ii++)
							{
								if (!prevcontinuedindexes.Contains(ii))
								{
									// polygon ends here
									// Finish the polygon with the last point(s):
									var prevpolygon = prevoutpolygonrow[ii];
									if (prevpolygon.outpolygon == null)
										continue;
									prevpolygon.outpolygon.rightpoints.Add(prevpolygon.bottomright);
									if (prevpolygon.bottomright.Pos.DistanceTo(prevpolygon.bottomleft.Pos) > EPS)
									{
										// polygon ends with a horizontal line:
										prevpolygon.outpolygon.leftpoints.Add(prevpolygon.bottomleft);
									}
									// reverse the left half so we get a counterclockwise circle:
									prevpolygon.outpolygon.leftpoints.Reverse();
									var points2d = new List<Vertex2D>(prevpolygon.outpolygon.rightpoints);
									points2d.AddRange(prevpolygon.outpolygon.leftpoints);
									var vertices3d = new List<Vertex>();
									foreach (var point2d in points2d)
									{
										var point3d = orthobasis.To3D(point2d.Pos);
										var vertex3d = new Vertex(point3d, point2d.Tex);
										vertices3d.Add(vertex3d);
									}
									var polygon = new Polygon(vertices3d, shared, plane);
									destpolygons.Add(polygon);
								}
							}
						} // if(yindex > 0)
						for (var i = 0; i < newoutpolygonrow.Count; i++)
						{
							var thispolygon = newoutpolygonrow[i];
							if (thispolygon.outpolygon == null)
							{
								// polygon starts here:
								thispolygon.outpolygon = new RetesselateOutPolygon ();
								thispolygon.outpolygon.leftpoints.Add(thispolygon.topleft);
								if (thispolygon.topleft.Pos.DistanceTo(thispolygon.topright.Pos) > EPS)
								{
									// we have a horizontal line at the top:
									thispolygon.outpolygon.rightpoints.Add(thispolygon.topright);
								}
							}
							else {
								// continuation of a previous row
								if (!thispolygon.leftlinecontinues)
								{
									thispolygon.outpolygon.leftpoints.Add(thispolygon.topleft);
								}
								if (!thispolygon.rightlinecontinues)
								{
									thispolygon.outpolygon.rightpoints.Add(thispolygon.topright);
								}
							}
						}
						prevoutpolygonrow = newoutpolygonrow;
					}
				} // for yindex
			} // if(numpolygons > 0)
		}

		static void InsertSorted<T>(List<T> array, T element, Func<T, T, int> comparefunc)
		{
			var leftbound = 0;
			var rightbound = array.Count;
			while (rightbound > leftbound)
			{
				var testindex = (leftbound + rightbound) / 2;
				var testelement = array[testindex];
				var compareresult = comparefunc(element, testelement);
				if (compareresult > 0) // element > testelement
				{
					leftbound = testindex + 1;
				}
				else {
					rightbound = testindex;
				}
			}
			array.Insert(leftbound, element);
		}

		static Vertex2DInterpolation InterpolateBetween2DPointsForY (Vertex2D vertex1, Vertex2D vertex2, float y)
		{
			var point1 = vertex1.Pos;
			var point2 = vertex2.Pos;
			var f1 = y - point1.Y;
			var f2 = point2.Y - point1.Y;
			if (f2 < 0)
			{
				f1 = -f1;
				f2 = -f2;
			}
			float t;
			if (f1 <= 0)
			{
				t = 0.0f;
			}
			else if (f1 >= f2)
			{
				t = 1.0f;
			}
			else if (f2 < 1e-10)
			{
				t = 0.5f;
			}
			else {
				t = f1 / f2;
			}
			var result = point1.X + t * (point2.X - point1.X);
			return new Vertex2DInterpolation {
				Result = result,
				Tex = vertex1.Tex + (vertex2.Tex - vertex1.Tex) * t,
			};
		}

		struct Vertex2DInterpolation
		{
			public float Result;
			public Vector2 Tex;
		}

		/// <summary>
		/// Used for tesselating co-planar polygons to keep
		/// track of texture coordinates.
		/// </summary>
		struct Vertex2D
		{
			public Vector2 Pos;
			public Vector2 Tex;
			public Vertex2D (Vector2 pos, Vector2 tex)
			{
				Pos = pos;
				Tex = tex;
			}
			public Vertex2D (float x, float y, Vector2 tex)
			{
				Pos = new Vector2 (x, y);
				Tex = tex;
			}
		}

		class RetesselateActivePolygon
		{
			public int polygonindex;
			public int leftvertexindex;
			public int rightvertexindex;
			public Vertex2D topleft;
			public Vertex2D topright;
			public Vertex2D bottomleft;
			public Vertex2D bottomright;
			public Line2D leftline;
			public bool leftlinecontinues;
			public Line2D rightline;
			public bool rightlinecontinues;
			public RetesselateOutPolygon outpolygon;
		}

		class RetesselateOutPolygon
		{
			public readonly List<Vertex2D> leftpoints = new List<Vertex2D> ();
			public readonly List<Vertex2D> rightpoints = new List<Vertex2D> ();
		}

		static int staticTag = 1;
		public static int GetTag()
		{
			return System.Threading.Interlocked.Increment (ref staticTag);
		}
	}

	class FuzzyCsgFactory
	{
		readonly VertexFactory vertexfactory = new VertexFactory (1.0e-5f);
		readonly PlaneFactory planefactory = new PlaneFactory(1.0e-5f);
		readonly Dictionary<string, PolygonShared> polygonsharedfactory = new Dictionary<string, PolygonShared>();

		public PolygonShared GetPolygonShared(PolygonShared sourceshared)
		{
			var hash = sourceshared.Hash;
			PolygonShared result;
			if (polygonsharedfactory.TryGetValue(hash, out result))
			{
				return result;
			}
			else
			{
				polygonsharedfactory.Add(hash, sourceshared);
				return sourceshared;
			}
		}

		public Vertex GetVertex(Vertex sourcevertex)
		{
			var result = vertexfactory.LookupOrCreate(ref sourcevertex);
			return result;
		}

		public Plane GetPlane(Plane sourceplane)
		{
			var result = planefactory.LookupOrCreate(sourceplane);
			return result;
		}

		public Polygon GetPolygon(Polygon sourcepolygon)
		{
			var newplane = GetPlane(sourcepolygon.Plane);
			var newshared = GetPolygonShared(sourcepolygon.Shared);
			var newvertices = new List<Vertex>(sourcepolygon.Vertices);
			for (int i = 0; i < newvertices.Count; i++)
			{
				newvertices[i] = GetVertex(newvertices[i]);
			}
			// two vertices that were originally very close may now have become
			// truly identical (referring to the same CSG.Vertex object).
			// Remove duplicate vertices:
			var newvertices_dedup = new List<Vertex>();
			if (newvertices.Count > 0)
			{
				var prevvertextag = newvertices[newvertices.Count - 1].Tag;
				foreach (var vertex in newvertices) {
					var vertextag = vertex.Tag;
					if (vertextag != prevvertextag)
					{
						newvertices_dedup.Add(vertex);
					}
					prevvertextag = vertextag;
				}
			}
			// If it's degenerate, remove all vertices:
			if (newvertices_dedup.Count < 3)
			{
				newvertices_dedup = new List<Vertex>();
			}
			return new Polygon(newvertices_dedup, newshared, newplane);
		}

		public Solid GetCsg(Solid sourcecsg)
		{
			var newpolygons = new List<Polygon>();
			foreach (var polygon in sourcecsg.Polygons)
			{
				var newpolygon = GetPolygon(polygon);
				if (newpolygon.Vertices.Count >= 3)
				{
					newpolygons.Add(newpolygon);
				}
			}
			return Solid.FromPolygons(newpolygons);
		}
	}

	class VertexFactory
	{
		static readonly KeyComparer keyComparer = new KeyComparer ();
		readonly Dictionary<Key, Vertex> lookuptable = new Dictionary<Key, Vertex> (keyComparer);
		readonly float multiplier;
		public VertexFactory (float tolerance)
		{
			multiplier = 1.0f / tolerance;
		}
		public Vertex LookupOrCreate (ref Vertex vertex)
		{
			var key = new Key {
				X = (int)(vertex.Pos.X * multiplier + 0.5),
				Y = (int)(vertex.Pos.Y * multiplier + 0.5),
				Z = (int)(vertex.Pos.Z * multiplier + 0.5),
				U = (int)(vertex.Tex.X * multiplier + 0.5),
				V = (int)(vertex.Tex.Y * multiplier + 0.5),
			};
			if (lookuptable.TryGetValue (key, out var v))
				return v;
			lookuptable.Add (key, vertex);
			return vertex;
		}
		struct Key
		{
			public int X, Y, Z, U, V;
		}
		class KeyComparer : IEqualityComparer<Key>
		{
			public bool Equals (Key x, Key y)
			{
				return x.X == y.X && x.Y == y.Y && x.Z == y.Z && x.U == y.U && x.V == y.V;
			}

			public int GetHashCode (Key k)
			{
				var hashCode = 1570706993;
				hashCode = hashCode * -1521134295 + k.X.GetHashCode ();
				hashCode = hashCode * -1521134295 + k.Y.GetHashCode ();
				hashCode = hashCode * -1521134295 + k.Z.GetHashCode ();
				hashCode = hashCode * -1521134295 + k.U.GetHashCode ();
				hashCode = hashCode * -1521134295 + k.V.GetHashCode ();
				return hashCode;
			}
		}
	}

	class PlaneFactory
	{
		static readonly KeyComparer keyComparer = new KeyComparer ();
		readonly Dictionary<Key, Plane> lookuptable = new Dictionary<Key, Plane> (keyComparer);
		readonly float multiplier;
		public PlaneFactory (float tolerance)
		{
			multiplier = 1.0f / tolerance;
		}
		public Plane LookupOrCreate (Plane plane)
		{
			var key = new Key {
				X = (int)(plane.Normal.X * multiplier + 0.5),
				Y = (int)(plane.Normal.Y * multiplier + 0.5),
				Z = (int)(plane.Normal.Z * multiplier + 0.5),
				W = (int)(plane.W * multiplier + 0.5),
			};
			if (lookuptable.TryGetValue (key, out var p))
				return p;
			lookuptable.Add (key, plane);
			return plane;
		}
		struct Key
		{
			public int X, Y, Z, W;
		}
		class KeyComparer : IEqualityComparer<Key>
		{
			public bool Equals (Key x, Key y)
			{
				return x.X == y.X && x.Y == y.Y && x.Z == y.Z && x.W == y.W;
			}

			public int GetHashCode (Key k)
			{
				var hashCode = 1570706993;
				hashCode = hashCode * -1521134295 + k.X.GetHashCode ();
				hashCode = hashCode * -1521134295 + k.Y.GetHashCode ();
				hashCode = hashCode * -1521134295 + k.Z.GetHashCode ();
				hashCode = hashCode * -1521134295 + k.W.GetHashCode ();
				return hashCode;
			}
		}
	}
}
