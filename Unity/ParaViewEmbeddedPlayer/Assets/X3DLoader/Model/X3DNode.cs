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
		//TODO Remove this shit!!
		public static Material __defaultMaterial;

		public static explicit operator GameObject (X3DNode node)
		{
			GameObject parent = new GameObject ("UnityRoot");
			node.Convert (parent);
			return parent;
		}

		virtual public void Convert (GameObject parent)
		{
		}
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

