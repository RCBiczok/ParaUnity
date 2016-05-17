namespace ParaUnity.X3D
{
	using System.Collections.Generic;
	using UnityEngine;

	public class X3DMesh
	{

		public Vector3[] Vertices { get; private set; }

		public Vector3[] Normals { get; private set; }

		public Color[] Colors { get; private set; }

		public int[] Triangles { get; private set; }

		public X3DMesh (Vector3[] vertices, 
		                Vector3[] normals,
		                Color[] colors,
		                int[] triangles)
		{
			this.Vertices = vertices;
			this.Normals = normals;
			this.Colors = colors;
			this.Triangles = triangles;
		}
	}
}

