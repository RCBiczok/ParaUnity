namespace ParaUnity.X3D
{
	using System;
	using System.Linq;
	using System.Xml.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityVC;

	public class X3DIndexedLineSet : X3DGeometry
	{
		public bool ColorPerVertex { get; private set; }

		public List<Vector3> Vertices { get; private set; }

		public List<Color> Colors { get; private set; }

		public List<List<int>> LineSets { get; private set; }

		public X3DIndexedLineSet (
		    bool colorPerVertex,
		    List<Vector3> vertices, 
		    List<Color> colors,
			List<List<int>> lineSets)
		{
			this.ColorPerVertex = colorPerVertex
			&& (colors == null || colors.Count == vertices.Count);
			this.Vertices = vertices;
			this.Colors = colors;
			this.LineSets = lineSets;
		}

		override public void Convert (GameObject parent)
		{
			for(int lineSetIdx = 0; lineSetIdx < this.LineSets.Count; lineSetIdx++) {
				for (int i = 0; i <  this.LineSets[lineSetIdx].Count - 1; i++) {
					GameObject obj = new GameObject ("Line_" + (i + lineSetIdx));
					obj.transform.parent = parent.transform;

					LineRenderer lineRenderer = obj.AddComponent<LineRenderer> ();
					Material material = new Material(Shader.Find("Standard (Vertex Color)"));
					Util.SetMaterialKeywords(material, WorkflowMode.Specular);
					lineRenderer.GetComponent<MeshRenderer> ().material = material;

					if (this.Colors != null) {
						if (this.ColorPerVertex) {
							lineRenderer.SetColors (this.Colors [this.LineSets[lineSetIdx] [i]], 
								this.Colors [this.LineSets[lineSetIdx] [i + 1]]);
						} else {
							lineRenderer.SetColors (this.Colors [lineSetIdx], 
								this.Colors [lineSetIdx]);
						}
					}
					lineRenderer.SetWidth (0.025F, 0.025F);
				
					lineRenderer.SetPositions (new Vector3[]{this.Vertices[this.LineSets[lineSetIdx][i]],this.Vertices[this.LineSets[lineSetIdx][i+1]]});
				}
			}
		}
	}

	public class X3DIndexedLineSetHandler : X3DGeometryHandler
	{

		public X3DIndexedLineSetHandler () : base ("IndexedLineSet")
		{
		}

		public override X3DGeometry ParseGeometry (XElement request)
		{
			return new X3DIndexedLineSet (
				(bool)request.Attribute ("colorPerVertex"),
				ParseVertices (request),
				ParseColors (request), 
				ParseCoordIndex (request)
			);
		}
	}
}