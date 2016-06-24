namespace ParaUnity.X3D
{
	using UnityEngine;
	using System;
	using System.Globalization;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml.Linq;

	public class Rotation
	{
		public float Angle { get; private set; }

		public Vector3 Axis { get; private set; }

		public Rotation (float angle, Vector3 axis)
		{
			this.Angle = angle;
			this.Axis = axis;
		}
	}

	/// <summary>
	/// Root class for objects parsed from X3DFiles 
	/// </summary>
	public abstract class X3DNode
	{
		public static explicit operator GameObject (X3DNode node)
		{
			GameObject parent = new GameObject ("UnityRoot");
			//parent.SetActive (false);
			node.Convert (parent);
			return parent;
		}

		virtual public void Convert (GameObject parent)
		{
		}
	}

	/// <summary>
	/// Root class for the actual geometry (meshes etc.)
	/// </summary>
	public abstract class X3DGeometry : X3DNode
	{
	}

	/// <summary>
	/// X3D object that can contain other nodes
	/// </summary>
	public abstract class X3DContainer : X3DNode
	{
		public X3DNode[] Children { get; private set; }

		public X3DContainer (X3DNode[] children)
		{
			this.Children = children;
		}

		override public void Convert (GameObject parent)
		{
			GameObject gameObject = new GameObject (this.GetType().Name);

			gameObject.transform.parent = parent.transform;

			foreach (X3DNode child in this.Children) {
				child.Convert (gameObject);
			}
		}
	}

	/// <summary>
	/// Marker interface for xml-processing handlers
	/// </summary>
	public abstract class X3DHandler
	{
		public string TargetNodeName { get; private set; }

		public abstract X3DNode Parse (XElement elem);

		protected X3DHandler (string targetNodeName)
		{
			this.TargetNodeName = targetNodeName;
		}

		protected Rotation ParseRotationAttribute (XAttribute attr)
		{
			if (attr != null) {
				string[] splits = attr.Value.Split (' ');
				if (splits.Length != 4) {
					throw new InvalidOperationException ("Vector3 must consist of 3 components");
				}
				return new Rotation (ParseFloat (splits [0]), new Vector3 (ParseFloat (splits [1]),
					ParseFloat (splits [2]), ParseFloat (splits [3])));
			}
			return null;
		}

		protected Vector3? ParseVectorAttribute (XAttribute attr)
		{
			if (attr != null) {
				string[] splits = attr.Value.Split (' ');
				if (splits.Length != 3) {
					throw new InvalidOperationException ("Vector3 must consist of 3 components");
				}
				return new Vector3 (ParseFloat (splits [0]), ParseFloat (splits [1]), ParseFloat (splits [2]));
			}
			return null;
		}

		protected Color? ParseColorAttribute (XAttribute attr)
		{
			if (attr != null) {
				string[] splits = attr.Value.Split (' ');
				if (splits.Length != 3) {
					throw new InvalidOperationException ("Color must consist of 3 components");
				}
				return new Color (ParseFloat (splits [0]), ParseFloat (splits [1]), ParseFloat (splits [2]));
			}
			return null;
		}

		protected bool? ParseBoolAttribute (XAttribute attr)
		{
			if (attr != null) {
				return attr.Value.ToUpper () == "true" ? true : false;
			}
			return null;
		}

		protected float? ParseFloatAttribute (XAttribute attr)
		{
			if (attr != null) {
				return float.Parse (attr.Value, CultureInfo.InvariantCulture);
			}
			return null;
		}

		protected float ParseFloat (string floatString)
		{
			return float.Parse (floatString, CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// Abstract class for handling geometry nodes
	/// </summary>
	public abstract class X3DGeometryHandler : X3DHandler
	{
		protected X3DGeometryHandler (string targetNodeName) : base (targetNodeName)
		{
		}

		public override X3DNode Parse (XElement elem)
		{
			return ParseGeometry (elem);
		}

		public abstract X3DGeometry ParseGeometry (XElement elem);

		protected List<List<int>> ParseCoordIndex (XElement request)
		{
			string coordIndex = (string)request.Attribute ("coordIndex");
			string[] facesString = coordIndex.Split (new[] { "-1" }, StringSplitOptions.None);
			List<List<int>> faces = new List<List<int>> (facesString.Length - 1);
			for (int i = 0; i < faces.Capacity; i++) {
				string[] faceString = facesString [i].Trim ().Split (' ');
				List<int> face = new List<int> (faceString.Length);
				for (int j = 0; j < face.Capacity; j++) {
					face.Add (Int32.Parse (faceString [j]));
				}
				faces.Add (face);
			}
			return faces;
		}

		protected List<Vector3> ParseVertices (XElement request)
		{
			return ParseVertexBased (request.Element ("Coordinate"), null, "point");
		}

		protected List<Vector3> ParseNormals (XElement request)
		{
			return ParseVertexBased (request.Element ("Normal"), 
				(string)request.Attribute ("normalIndex"), "vector");
		}

		protected List<Color> ParseColors (XElement request)
		{
			return ParseListOfTuples (request.Element ("Color"), 
				(string)request.Attribute ("colorIndex"), "color", (colorChannels) => {
					return new Color (ParseFloat (colorChannels [0]),
						ParseFloat (colorChannels [1]),
						ParseFloat (colorChannels [2]), 1);
				});
		}

		protected List<Vector3> ParseVertexBased (XElement elem, string indexString, string subAttr)
		{
			return ParseListOfTuples (elem, indexString, subAttr, (vertexCoords) => {
				return new Vector3 (ParseFloat (vertexCoords [0]),
					ParseFloat (vertexCoords [1]),
					ParseFloat (vertexCoords [2]));
			});
		}

		private delegate T TupleConsumer<T> (string[] tuple);

		private List<T> ParseListOfTuples <T> (XElement elem, string indexString, string subAttr, TupleConsumer<T> consumer)
		{
			if (elem == null || elem.Attribute (subAttr) == null) {
				return null;
			}
			string[] verticesString = ((string)elem.Attribute (subAttr)).Trim ().Split (',');
			List<T> vertices = new List<T> (verticesString.Length);
			for (int i = 0; i < vertices.Capacity; i++) {
				if (verticesString [i].Trim () == "") {
					continue;
				}
				vertices.Add (consumer (verticesString [i].Trim ().Split (' ')));
			}
			if (indexString != null) {
				string[] indexParts = indexString.Trim ().Split (' ');
				List<int> index = new List<int> (indexParts.Length);
				for (int i = 0; i < index.Capacity; i++) {
					index.Add (Int32.Parse (indexParts [i]));
				}
				List<T> orderedVertices = new List<T> (vertices);
				for (int i = 0; i < vertices.Count; i++) {
					orderedVertices [index [i]] = vertices [i];
				}
				return orderedVertices;
			}
			return vertices;
		}
	}

	/// <summary>
	/// Abstract class for handling nestable nodes
	/// </summary>
	public abstract class X3DContainerHandler : X3DHandler
	{
		private X3DHandler[] Handlers { get; set; }

		protected X3DContainerHandler (string targetNodeName, bool nestable, X3DHandler[] handlers) : base (targetNodeName)
		{
			if (nestable) {
				this.Handlers = handlers.Concat (new X3DHandler[]{ this }).ToArray ();
			} else {
				this.Handlers = handlers;
			}
		}

		public override X3DNode Parse (XElement elem)
		{
			Dictionary<string, X3DHandler> handlerDict = Handlers.ToDictionary (h => h.TargetNodeName, h => h);

			return ParseContainer (elem.Elements ().
				Where (e => handlerDict.ContainsKey (e.Name.ToString ())).
				Select (e => handlerDict [e.Name.ToString ()].Parse (e)).ToArray (), elem);
		}

		protected abstract X3DContainer ParseContainer (X3DNode[] nodes, XElement elem);

	}
}

