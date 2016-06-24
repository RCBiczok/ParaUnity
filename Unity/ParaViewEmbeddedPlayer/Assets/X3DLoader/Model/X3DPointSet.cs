namespace ParaUnity.X3D
{
	using System;
	using System.Linq;
	using System.Xml.Linq;
	using System.Collections.Generic;
	using UnityEngine;

	public class X3DPointSet : X3DGeometry
	{
		public List<Vector3> Vertices { get; private set; }

		public List<Color> Colors { get; private set; }

		public X3DPointSet (
			List<Vector3> vertices, 
			List<Color> colors)
		{
			this.Vertices = vertices;
			this.Colors = colors;
		}

		override public void Convert (GameObject parent)
		{
			for(int i = 0; i < this.Vertices.Count; i++) {
				GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.parent = parent.transform;
				sphere.transform.position = this.Vertices[i];
				if (this.Colors != null) {
					MeshFilter meshFilter = sphere.GetComponent<MeshFilter> ();
					Mesh mesh = meshFilter.mesh;
					mesh.colors = Enumerable.Repeat (this.Colors [i], mesh.vertices.Length).ToArray ();
					meshFilter.mesh = mesh;

				}
				sphere.GetComponent<MeshRenderer>().material = GameObject.Find ("MaterialPlaceHolder").GetComponent<MeshRenderer> ().material;


				Vector3 scale = sphere.transform.localScale;
				scale.x = 0.1F;
				scale.y = 0.1F; 
				sphere.transform.localScale = scale;


				sphere.transform.localScale = new Vector3(sphere.transform.localScale.x, 0.1F, sphere.transform.localScale.y);
			}
		}
	}

	public class X3DPointSetHandler : X3DGeometryHandler
	{

		public X3DPointSetHandler () : base ("PointSet")
		{
		}

		public override X3DGeometry ParseGeometry (XElement request)
		{
			return new X3DPointSet (
				ParseVertices (request),
				ParseColors (request)
			);
		}
	}
}