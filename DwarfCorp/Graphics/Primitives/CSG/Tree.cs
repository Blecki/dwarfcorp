using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Csg
{
	class Tree
	{
		PolygonTreeNode polygonTree;
		Node rootnode;

		public Node RootNode => rootnode;

		public Tree(BoundingBox bbox, List<Polygon> polygons)
		{
			polygonTree = new PolygonTreeNode();
			rootnode = new Node(null);
			if (polygons != null) AddPolygons(polygons);
		}

		public void Invert()
		{
			polygonTree.Invert();
			rootnode.Invert();
		}

		public void ClipTo(Tree tree, bool alsoRemoveCoplanarFront = false)
		{
			rootnode.ClipTo(tree, alsoRemoveCoplanarFront);
		}

		public List<Polygon> AllPolygons()
		{
			var result = new List<Polygon>();
			polygonTree.GetPolygons(result);
			return result;
		}

		public void AddPolygons(List<Polygon> polygons)
		{
			var n = polygons.Count;
			var polygontreenodes = new PolygonTreeNodeList(n);
			for (var i = 0; i < n; i++)
			{
				var p = polygonTree.AddChild(polygons[i]);
				polygontreenodes.Add(p);
			}
			rootnode.AddPolygonTreeNodes(polygontreenodes);
		}
	}

	class Node
	{
		public Plane Plane;
		public Node Front;
		public Node Back;
		public PolygonTreeNodeList PolygonTreeNodes = new PolygonTreeNodeList ();
		public readonly Node Parent;

		public Node(Node parent)
		{
			Parent = parent;
		}

		public void Invert()
		{
			Queue<Node> queue = null;
			Node node = this;
			while (true)
			{
				if (node.Plane != null) node.Plane = node.Plane.Flipped();
				if (node.Front != null) {
					if (queue == null)
						queue = new Queue<Node> ();
					queue.Enqueue (node.Front);
				}
				if (node.Back != null) {
					if (queue == null)
						queue = new Queue<Node> ();
					queue.Enqueue (node.Back);
				}
				var temp = node.Front;
				node.Front = node.Back;
				node.Back = temp;

				if (queue != null && queue.Count > 0)
					node = queue.Dequeue ();
				else
					break;
			}
		}

		public void ClipPolygons(PolygonTreeNodeList clippolygontreenodes, bool alsoRemoveCoplanarFront)
		{
			var args = new Args (node: this, polygonTreeNodes: clippolygontreenodes);
			Stack<Args> stack = null;

			while (true)
			{
				var clippingNode = args.Node;
				var polygontreenodes = args.PolygonTreeNodes;

				if (clippingNode.Plane != null)
				{
					PolygonTreeNodeList backnodes = null;
					PolygonTreeNodeList frontnodes = null;
					var plane = clippingNode.Plane;
					var numpolygontreenodes = polygontreenodes.Count;
					for (var i = 0; i < numpolygontreenodes; i++)
					{
						var polyNode = polygontreenodes[i];
						if (!polyNode.IsRemoved)
						{
							if (alsoRemoveCoplanarFront)
							{
								polyNode.SplitByPlane(plane, ref backnodes, ref backnodes, ref frontnodes, ref backnodes);
							}
							else
							{
								polyNode.SplitByPlane(plane, ref frontnodes, ref backnodes, ref frontnodes, ref backnodes);
							}
						}
					}

					if (clippingNode.Front != null && (frontnodes != null))
					{
						if (stack == null) stack = new Stack<Args>();
						stack.Push(new Args (node: clippingNode.Front, polygonTreeNodes: frontnodes));
					}
					var numbacknodes = backnodes == null ? 0 : backnodes.Count;
					if (clippingNode.Back != null && backnodes != null && (numbacknodes > 0))
					{
						if (stack == null) stack = new Stack<Args>();
						stack.Push(new Args (node: clippingNode.Back, polygonTreeNodes: backnodes));
					}
					else if (backnodes != null) {
						// there's nothing behind this plane. Delete the nodes behind this plane:
						for (var i = 0; i < numbacknodes; i++)
						{
							backnodes[i].Remove();
						}
					}
				}
				if (stack != null && stack.Count > 0) args = stack.Pop();
				else break;
			}
		}

		public void ClipTo(Tree clippingTree, bool alsoRemoveCoplanarFront)
		{
			Node node = this;
			Stack<Node> stack = null;
			while (node != null)
			{
				if (node.PolygonTreeNodes.Count > 0)
				{
					clippingTree.RootNode.ClipPolygons(node.PolygonTreeNodes, alsoRemoveCoplanarFront);
				}
				if (node.Front != null)
				{
					if (stack == null) stack = new Stack<Node>();
					stack.Push(node.Front);
				}
				if (node.Back != null)
				{
					if (stack == null) stack = new Stack<Node>();
					stack.Push(node.Back);
				}
				node = (stack != null && stack.Count > 0) ? stack.Pop() : null;
			}
		}

		public void AddPolygonTreeNodes(PolygonTreeNodeList addpolygontreenodes)
		{
			var args = new Args (node: this, polygonTreeNodes: addpolygontreenodes);
			Stack<Args> stack = null;
			while (true)
			{
				var node = args.Node;
				var polygontreenodes = args.PolygonTreeNodes;

				if (polygontreenodes.Count == 0)
				{
					// Nothing to do
				}
				else {
					var _this = node;
					var _thisPlane = _this.Plane;
					if (_thisPlane == null)
					{
						var bestplane = polygontreenodes[0].GetPolygon().Plane;
						node.Plane = bestplane;
						_thisPlane = bestplane;
					}

					var frontnodes = default (PolygonTreeNodeList);
					var backnodes = default (PolygonTreeNodeList);

					for (int i = 0, n = polygontreenodes.Count; i < n; i++)
					{
						polygontreenodes[i].SplitByPlane(_thisPlane, ref _this.PolygonTreeNodes, ref backnodes, ref frontnodes, ref backnodes);
					}

					if (frontnodes != null && frontnodes.Count > 0)
					{
						if (node.Front == null) node.Front = new Node(node);
						if (stack == null)
							stack = new Stack<Args> ();
						stack.Push(new Args (node: node.Front, polygonTreeNodes: frontnodes));
					}
					if (backnodes != null && backnodes.Count > 0)
					{
						if (node.Back == null) node.Back = new Node(node);
						if (stack == null)
							stack = new Stack<Args> ();
						stack.Push(new Args (node: node.Back, polygonTreeNodes: backnodes));
					}
				}

				if (stack != null && stack.Count > 0) args = stack.Pop();
				else break;
			}
		}
		struct Args
		{
			public Node Node;
			public PolygonTreeNodeList PolygonTreeNodes;

			public Args (Node node, PolygonTreeNodeList polygonTreeNodes)
			{
				Node = node;
				PolygonTreeNodes = polygonTreeNodes;
			}
		}
	}

	class PolygonTreeNode
	{
		PolygonTreeNode parent;
		readonly PolygonTreeNodeList children = new PolygonTreeNodeList ();
		Polygon polygon;
		bool removed;

		public BoundingBox BoundingBox => polygon?.BoundingBox;

		public void AddPolygons(List<Polygon> polygons)
		{
			if (!IsRootNode)
			{
				throw new InvalidOperationException("New polygons can only be added to  root nodes.");
			}
			for (var i = 0; i < polygons.Count; i++)
			{
				AddChild(polygons[i]);
			}
		}

		public void Remove()
		{
			if (!this.removed)
			{
				this.removed = true;

#if DEBUG
				if (this.IsRootNode) throw new InvalidOperationException("Can't remove root node");
				if (this.children.Count > 0) throw new InvalidOperationException("Can't remove nodes with children");
#endif

				// remove ourselves from the parent's children list:
				var parentschildren = this.parent?.children;
				parentschildren?.Remove(this);

				// invalidate the parent's polygon, and of all parents above it:
				this.parent?.RecursivelyInvalidatePolygon();
			}
		}

		public bool IsRemoved => removed;

		public bool IsRootNode => parent == null;

		public void Invert()
		{
			if (!IsRootNode) throw new InvalidOperationException("Only the root nodes are invertable.");
			InvertSub();
		}

		public Polygon GetPolygon()
		{
			if (polygon == null) throw new InvalidOperationException("Node is not associated with a polygon.");
			return this.polygon;
		}

		public void GetPolygons(List<Polygon> result)
		{
			var queue = new Queue<PolygonTreeNodeList>();
			queue.Enqueue(new PolygonTreeNodeList (this));
			while (queue.Count > 0)
			{
				var children = queue.Dequeue();
				var l = children.Count;
				for (int j = 0; j < l; j++)
				{
					var node = children[j];
					if (node.polygon != null)
					{
						result.Add(node.polygon);
					}
					else {
						queue.Enqueue(node.children);
					}
				}
			}
		}

		public void SplitByPlane(Plane plane, ref PolygonTreeNodeList coplanarfrontnodes, ref PolygonTreeNodeList coplanarbacknodes, ref PolygonTreeNodeList frontnodes, ref PolygonTreeNodeList backnodes)
		{
			if (children.Count > 0)
			{
				Queue<PolygonTreeNodeList> queue = null;
				var nodes = children;
				while (true)
				{
					var l = nodes.Count;
					for (int j = 0; j < l; j++)
					{
						var node = nodes[j];
						if (node.children.Count > 0)
						{
							if (queue == null)
								queue = new Queue<PolygonTreeNodeList> (node.children.Count);
							queue.Enqueue(node.children);
						}
						else {
							node.SplitPolygonByPlane(plane, ref coplanarfrontnodes, ref coplanarbacknodes, ref frontnodes, ref backnodes);
						}
					}
					if (queue != null && queue.Count > 0)
						nodes = queue.Dequeue ();
					else
						break;
				}
			}
			else {
				SplitPolygonByPlane(plane, ref coplanarfrontnodes, ref coplanarbacknodes, ref frontnodes, ref backnodes);
			}
		}

		void SplitPolygonByPlane(Plane plane, ref PolygonTreeNodeList coplanarfrontnodes, ref PolygonTreeNodeList coplanarbacknodes, ref PolygonTreeNodeList frontnodes, ref PolygonTreeNodeList backnodes)
		{
			var polygon = this.polygon;
			if (polygon != null)
			{
				var bound = polygon.BoundingSphere;
				var sphereradius = bound.Radius + 1.0e-4;
				var planenormal = plane.Normal;
				var spherecenter = bound.Center;
				var d = Vector3.Dot(planenormal, spherecenter) - plane.W;
				if (d > sphereradius)
				{
					if (frontnodes == null) frontnodes = new PolygonTreeNodeList();
					frontnodes?.Add(this);
				}
				else if (d < -sphereradius)
				{
					if (backnodes == null) backnodes = new PolygonTreeNodeList();
					backnodes?.Add(this);
				}
				else {
					SplitPolygonResult splitresult;
					plane.SplitPolygon(polygon, out splitresult);
					switch (splitresult.Type)
					{
						case 0:
							if (coplanarfrontnodes == null) coplanarfrontnodes = new PolygonTreeNodeList();
							coplanarfrontnodes?.Add(this);
							break;
						case 1:
							if (coplanarbacknodes == null) coplanarbacknodes = new PolygonTreeNodeList();
							coplanarbacknodes?.Add(this);
							break;
						case 2:
							if (frontnodes == null) frontnodes = new PolygonTreeNodeList();
							frontnodes?.Add(this);
							break;
						case 3:
							if (backnodes == null) backnodes = new PolygonTreeNodeList();
							backnodes?.Add(this);
							break;
						default:
							if (splitresult.Front != null)
							{
								var frontnode = AddChild(splitresult.Front);
								if (frontnodes == null) frontnodes = new PolygonTreeNodeList();
								frontnodes?.Add(frontnode);
							}
							if (splitresult.Back != null)
							{
								var backnode = AddChild(splitresult.Back);
								if (backnodes == null) backnodes = new PolygonTreeNodeList();
								backnodes?.Add(backnode);
							}
							break;
					}
				}
			}
		}

		public PolygonTreeNode AddChild(Polygon polygon)
		{
			var newchild = new PolygonTreeNode();
			newchild.parent = this;
			newchild.polygon = polygon;
			children.Add(newchild);
			return newchild;
		}

		void InvertSub()
		{
			var queue = new Queue<PolygonTreeNodeList>();
			queue.Enqueue(new PolygonTreeNodeList (this));
			while (queue.Count > 0)
			{
				var children = queue.Dequeue();
				var l = children.Count;
				for (int j = 0; j < l; j++)
				{
					var node = children[j];
					if (node.polygon != null)
					{
						node.polygon = node.polygon.Flipped();
					}
					queue.Enqueue(node.children);
				}
			}
		}

		void RecursivelyInvalidatePolygon()
		{
			var node = this;
			while (node.polygon != null)
			{
				node.polygon = null;
				if (node.parent != null)
				{
					node = node.parent;
				}
			}
		}
	}

	class PolygonTreeNodeList
	{
		PolygonTreeNode node0;
		List<PolygonTreeNode> nodes;
		public int Count => nodes != null ? nodes.Count : (node0 != null ? 1 : 0);
		public PolygonTreeNodeList (PolygonTreeNode item0)
		{
			this.node0 = item0;
			this.nodes = null;
		}
		public PolygonTreeNodeList (int capacity)
		{
			node0 = null;
			if (capacity > 1) {
				nodes = new List<PolygonTreeNode> (capacity);
			}
			else {
				nodes = null;
			}
		}
		public PolygonTreeNodeList ()
		{
		}
		public PolygonTreeNode this[int index] =>
			nodes != null
			? nodes[index]
			: (node0 ?? throw new ArgumentOutOfRangeException (nameof (index)));
		public void Add (PolygonTreeNode node)
		{
			if (nodes != null) {
				nodes.Add (node);
			}
			else {
				if (node0 == null) {
					node0 = node;
				}
				else {
					nodes = new List<PolygonTreeNode> (2) { node0, node };
					node0 = null;
				}
			}
		}
		public void Remove (PolygonTreeNode node)
		{
			if (nodes != null) {
				nodes.Remove (node);
			}
			else {
				if (ReferenceEquals (node, node0))
					node0 = null;
			}
		}
	}
}

