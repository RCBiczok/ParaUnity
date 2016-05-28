namespace ParaUnity.X3D
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Xml.Linq;
	using System.Collections.Generic;
	using UnityEngine;

	internal class IndexedFaceSet
	{

		public bool NormalPerVertex { get; private set; }

		public bool ColorPerVertex { get; private set; }

		public List<Vector3> Vertices { get; private set; }

		public List<Vector3> Normals { get; private set; }

		public List<Color> Colors { get; private set; }

		public List<List<int>> Faces { get; private set; }

		public IndexedFaceSet (bool normalPerVertex,
		                       bool colorPerVertex,
		                       List<Vector3> vertices, 
		                       List<Vector3> normals,
		                       List<Color> colors,
		                       List<List<int>> faces)
		{
			this.NormalPerVertex = normalPerVertex
			&& (normals == null || normals.Count == vertices.Count);
			this.ColorPerVertex = colorPerVertex
			&& (colors == null || colors.Count == vertices.Count);
			this.Vertices = vertices;
			this.Normals = normals;
			this.Colors = colors;
			this.Faces = faces;
		}

		public static explicit operator X3DMesh (IndexedFaceSet faceSet)
		{
			IndexedFaceSet preparedFaceSet = faceSet.ToTriangulatedFaceSet ().
				ToVertexOrientedFaceSet ();

			int[] triangles = new int[preparedFaceSet.Faces.Count * 3];
			for (int i = 0; i < preparedFaceSet.Faces.Count; i++) {
				triangles [i * 3] = preparedFaceSet.Faces [i] [0];
				triangles [i * 3 + 1] = preparedFaceSet.Faces [i] [1];
				triangles [i * 3 + 2] = preparedFaceSet.Faces [i] [2];
			}
			Vector3[] normals = null;
			if (preparedFaceSet.Normals != null) {
				normals = preparedFaceSet.Normals.ToArray ();
			}
			Color[] colors = null;
			if (preparedFaceSet.Colors != null) {
				colors = preparedFaceSet.Colors.ToArray ();
			}
				
			return new X3DMesh (preparedFaceSet.Vertices.ToArray (), 
				normals, 
				colors, 
				triangles);
		}

		private IndexedFaceSet ToTriangulatedFaceSet ()
		{
			LinkedList<List<int>> triangles = new LinkedList<List<int>> ();
			LinkedList<Vector3> newNormals = new LinkedList<Vector3> ();
			LinkedList<Color> newColors = new LinkedList<Color> ();

			for (int faceIdx = 0; faceIdx < this.Faces.Count; faceIdx++) {

				//X3D standard requires that the faces are planar and do not have "holes"

				Vector2[] prohjectedVertices = ProjectorVerticesOf (this.Faces [faceIdx], 
					                               (vec) => new Vector2 (vec.x, vec.y));
				if (HasDuplicates (prohjectedVertices)) {
					prohjectedVertices = ProjectorVerticesOf (this.Faces [faceIdx], 
						(vec) => new Vector2 (vec.y, vec.z));
				}

				Triangulator tr = new Triangulator (prohjectedVertices);
				int[] indices = tr.Triangulate ();
					
				for (int i = 0; i < indices.Length / 3; i++) {
					List<int> triangle = new List<int> ();

					if (IsFacingInRightDirection (
						    this.Vertices [this.Faces [faceIdx] [0]],
						    this.Vertices [this.Faces [faceIdx] [1]],
						    this.Vertices [this.Faces [faceIdx] [2]],
						    this.Vertices [this.Faces [faceIdx] [indices [3 * i]]], 
						    this.Vertices [this.Faces [faceIdx] [indices [3 * i + 1]]], 
						    this.Vertices [this.Faces [faceIdx] [indices [3 * i + 2]]])) {
						triangle.Add (this.Faces [faceIdx] [indices [3 * i]]);
						triangle.Add (this.Faces [faceIdx] [indices [3 * i + 1]]);
						triangle.Add (this.Faces [faceIdx] [indices [3 * i + 2]]);
					} else {
						triangle.Add (this.Faces [faceIdx] [indices [3 * i + 2]]);
						triangle.Add (this.Faces [faceIdx] [indices [3 * i + 1]]);
						triangle.Add (this.Faces [faceIdx] [indices [3 * i]]);
					}

					triangles.AddLast (triangle);
					if (!this.NormalPerVertex) {
						newNormals.AddLast (this.Normals [faceIdx]);
					}
					if (!this.ColorPerVertex) {
						newColors.AddLast (this.Colors [faceIdx]);
					}
				}
			}
				
			List<Vector3> normals = this.Normals;
			if (!this.NormalPerVertex) {
				normals = new List<Vector3> (newNormals);
			}
			List<Color> colors = this.Colors;
			if (!this.ColorPerVertex) {
				colors = new List<Color> (newColors);
			}

			return new IndexedFaceSet (this.NormalPerVertex, 
				this.ColorPerVertex, 
				this.Vertices, 
				normals, 
				colors, 
				new List<List<int>> (triangles));
		}

		/// <summary>
		/// Returns a face set whose colors and normals are all associated with vertices instead of faces.
		/// It will duplicate colors and normals if necessary.
		/// </summary>
		/// <returns>The face set where the condition normalPerVertex=colorPerVertex=true holds.</returns>
		private IndexedFaceSet ToVertexOrientedFaceSet ()
		{
			if (this.ColorPerVertex && this.NormalPerVertex
			    || (!this.ColorPerVertex && this.Colors == null)
			    || (!this.NormalPerVertex && this.Normals == null)) {
				return this;
			}

			List<List<int>> faces = new List<List<int>> (this.Faces.Count);
			foreach (List<int> face in this.Faces) {
				faces.Add (new List<int> (face));
			}

			List<Vector3> vertices = this.Vertices;
			bool[] seenVertices = new bool[this.Vertices.Count];
			List<Vector3> normals;
			if (this.NormalPerVertex) {
				normals = this.Normals;
			} else {
				normals = new List<Vector3> (new Vector3[this.Vertices.Count]);
			}
			List<Color> colors;
			if (this.ColorPerVertex) {
				colors = this.Colors;
			} else {
				colors = new List<Color> (new Color[this.Vertices.Count]);
			}
				
			LinkedList<Vector3> addedVertices = new LinkedList<Vector3> ();
			LinkedList<Vector3> addedNormals = new LinkedList<Vector3> ();
			LinkedList<Color> addedColors = new LinkedList<Color> ();
			for (int faceIdx = 0; faceIdx < faces.Count; faceIdx++) {
				for (int nodeIdx = 0; nodeIdx < faces [faceIdx].Count; nodeIdx++) {
					if (!seenVertices [faces [faceIdx] [nodeIdx]]) {
						seenVertices [faces [faceIdx] [nodeIdx]] = true;
						if (!this.NormalPerVertex && this.Normals != null) {
							normals [faces [faceIdx] [nodeIdx]] = this.Normals [faceIdx];
						}
						if (!this.ColorPerVertex && this.Colors != null) {
							colors [faces [faceIdx] [nodeIdx]] = this.Colors [faceIdx];
						}
					} else {
						addedVertices.AddLast (this.Vertices [faces [faceIdx] [nodeIdx]]);
						if (this.Normals != null) {
							if (this.NormalPerVertex) {
								addedNormals.AddLast (this.Normals [faces [faceIdx] [nodeIdx]]);
							} else {
								addedNormals.AddLast (this.Normals [faceIdx]);
							} 
						}
						if (this.Colors != null) {
							if (this.ColorPerVertex) {
								addedColors.AddLast (this.Colors [faces [faceIdx] [nodeIdx]]);
							} else {
								addedColors.AddLast (this.Colors [faceIdx]);
							}
						}
						faces [faceIdx] [nodeIdx] = vertices.Count + addedVertices.Count - 1;
					}
				}
			}

			vertices.AddRange (addedVertices);
			if (normals != null) {
				normals.AddRange (addedNormals);
			} 
			if (colors != null) {
				colors.AddRange (addedColors);
			}

			return new IndexedFaceSet (true, true, vertices, normals, colors, faces);
		}

		private bool IsFacingInRightDirection (Vector3 aOld, Vector3 bOld, Vector3 cOld,
		                                       Vector3 aNew, Vector3 bNew, Vector3 cNew)
		{

			Vector3 surfaceNormalOld = Vector3.Cross (bOld - aOld, cOld - aOld);
			Vector3 surfaceNormalNew = Vector3.Cross (bNew - aNew, cNew - aNew);

			return Vector3.Dot (surfaceNormalOld, surfaceNormalNew) > 0;
		}

		private bool HasDuplicates (Vector2[] vertices)
		{
			var duplicateKeys = vertices.GroupBy (x => x)
				.Where (group => group.Count () > 1)
				.Select (group => group.Key);
			return duplicateKeys.Count () != 0;
		}

		private delegate Vector2 Projector (Vector3 vec);

		private Vector2[] ProjectorVerticesOf (List<int> face, Projector projector)
		{
			LinkedList<Vector2> projectedVectors = new LinkedList<Vector2> ();
			foreach (int vertexId in face) {
				projectedVectors.AddLast (projector (this.Vertices [vertexId]));
			}
			return new List<Vector2> (projectedVectors).ToArray ();
		}

		public void Dump ()
		{
			Console.WriteLine ("Color per vertex:" + this.ColorPerVertex);
			Console.WriteLine ("Normal per vertex:" + this.NormalPerVertex);
			Console.Write ("Vertices (" + this.Vertices.Count + "):");
			foreach (Vector3 vec in this.Vertices) {
				Console.Write (vec + ",");
			}
			Console.WriteLine ();
			if (this.Normals != null) {
				Console.Write ("Normals (" + this.Normals.Count + "):");
				foreach (Vector3 vec in this.Normals) {
					Console.Write (vec + ",");
				}
				Console.WriteLine ();
			}
			if (this.Colors != null) {
				Console.Write ("Colors (" + this.Colors.Count + "):");
				foreach (Color col in this.Colors) {
					Console.Write (col + ",");
				}
				Console.WriteLine ();
			}
			Console.Write ("Faces (" + this.Faces.Count + "):");
			foreach (List<int> face in this.Faces) {
				Console.Write ("(");
				foreach (int idx  in face) {
					Console.Write (idx);
					Console.Write (",");
				}
				Console.Write ("),");
			}
			Console.WriteLine ();
		}

	}

	internal class IndexedFaceSetHandler
	{

		public X3DMesh Parse (XNamespace df, XElement request)
		{
			return (X3DMesh)new IndexedFaceSet (
				(bool)request.Attribute (df + "normalPerVertex"),
				(bool)request.Attribute (df + "colorPerVertex"),
				ParseVertices (df, request),
				ParseNormals (df, request),
				ParseColors (df, request), 
				ParseFaces (df, request)
			);

		}

		private List<List<int>>  ParseFaces (XNamespace df, XElement request)
		{
			string coordIndex = (string)request.Attribute (df + "coordIndex");
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

		private List<Vector3> ParseVertices (XNamespace df, XElement request)
		{
			return ParseVertexBased (request.Element (df + "Coordinate"), null, "point");
		}

		private List<Vector3> ParseNormals (XNamespace df, XElement request)
		{
			return ParseVertexBased (request.Element (df + "Normal"), 
				(string)request.Attribute (df + "normalIndex"), "vector");
		}

		private List<Color> ParseColors (XNamespace df, XElement request)
		{
			return ParseListOfTuples (request.Element (df + "Color"), 
				(string)request.Attribute (df + "colorIndex"), "color", (colorChannels) => {
				return new Color (ParseFloat (colorChannels [0]),
					ParseFloat (colorChannels [1]),
					ParseFloat (colorChannels [2]), 1);
			});
		}

		private List<Vector3> ParseVertexBased (XElement elem, string indexString, string subAttr)
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

		private float ParseFloat (string floatString)
		{
			return float.Parse (floatString, CultureInfo.InvariantCulture);
		}
	}

}

