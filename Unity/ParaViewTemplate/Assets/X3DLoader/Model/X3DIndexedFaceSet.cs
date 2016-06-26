namespace ParaUnity.X3D
{
	using System;
	using System.Linq;
	using System.Xml.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityVC;

	public class X3DIndexedFaceSet : X3DGeometry
	{
		private const int MAX_VERTEX_PER_MESH_COUNT = 65000;

		public bool Solid { get; private set; }

		public bool NormalPerVertex { get; private set; }

		public bool ColorPerVertex { get; private set; }

		public List<Vector3> Vertices { get; private set; }

		public List<Vector3> Normals { get; private set; }

		public List<Color> Colors { get; private set; }

		public List<List<int>> Faces { get; private set; }

		public X3DIndexedFaceSet (bool solid,
							   bool normalPerVertex,
		                       bool colorPerVertex,
		                       List<Vector3> vertices, 
		                       List<Vector3> normals,
		                       List<Color> colors,
		                       List<List<int>> faces)
		{
			this.Solid = solid;
			this.NormalPerVertex = normalPerVertex
			&& (normals == null || normals.Count == vertices.Count);
			this.ColorPerVertex = colorPerVertex
			&& (colors == null || colors.Count == vertices.Count);
			this.Vertices = vertices;
			this.Normals = normals;
			this.Colors = colors;
			this.Faces = faces;
		}
			
		override public void Convert (GameObject parent) 
		{
			X3DIndexedFaceSet preparedFaceSet = this.ToTriangulatedFaceSet ().
				ToVertexOrientedFaceSet ();

			AddFace (parent, preparedFaceSet, true);
			if(!this.Solid) {
				AddFace (parent, preparedFaceSet, false);
			}
		}

		private void AddFace(GameObject parent, X3DIndexedFaceSet preparedFaceSet, bool frontFace) {

			List<Vector3> vertices = null;
			int?[] vertexMapping = null;
			List<Vector3> normals = null;
			List<Color> colors = null;
			List<int> triangles = null;

			int partCount = 0;

			for (int i = 0; i < preparedFaceSet.Faces.Count; i++) {
				if (vertices != null && vertices.Count + 3 > MAX_VERTEX_PER_MESH_COUNT) {
					AddPart (parent, vertices, normals, colors, triangles, (frontFace ? "Front_" : "Back_") + partCount);
					partCount++;
					vertices = null;
				}
				if (vertices == null) {
					vertices = new List<Vector3> (preparedFaceSet.Vertices.Count);
					vertexMapping = new int?[preparedFaceSet.Vertices.Count];
					if (preparedFaceSet.Normals != null) {
						normals = new List<Vector3> (preparedFaceSet.Normals.Count);
					}
					if (preparedFaceSet.Colors != null) {
						colors = new List<Color> (preparedFaceSet.Colors.Count);
					}
					triangles = new List<int>(preparedFaceSet.Faces.Count * 3);
				}
					
				int vertexA;
				int vertexB;
				int vertexC;

				if (frontFace) {
					vertexA = preparedFaceSet.Faces [i] [0];
					vertexB = preparedFaceSet.Faces [i] [1];
					vertexC = preparedFaceSet.Faces [i] [2];
				} else {
					vertexA = preparedFaceSet.Faces [i] [2];
					vertexB = preparedFaceSet.Faces [i] [1];
					vertexC = preparedFaceSet.Faces [i] [0];
				}

				foreach (int vertexId in new int[]{vertexA, vertexB, vertexC}) {
					if (vertexMapping [vertexId] == null) {
						vertexMapping [vertexId] = vertices.Count;
						vertices.Add (preparedFaceSet.Vertices[vertexId]);
						if (preparedFaceSet.Normals != null) {
							normals.Add (preparedFaceSet.Normals[vertexId]);
						}
						if (preparedFaceSet.Colors != null) {
							colors.Add (preparedFaceSet.Colors[vertexId]);
						}
					}
					triangles.Add (vertexMapping [vertexId].Value);
				}
			}
			AddPart (parent, vertices, normals, colors, triangles, (frontFace ? "Front_" : "Back_") + partCount);
		}

		private void AddPart(GameObject parent,
			List<Vector3> vertices,
			List<Vector3> normals,
			List<Color> colors,
			List<int> triangles,
			string label) {

			GameObject part = new GameObject (label);
			part.transform.parent = parent.transform;

			Mesh mesh = new Mesh ();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			if (normals != null) {
				mesh.normals = normals.ToArray();
			}
			if (colors != null) {
				mesh.colors = colors.ToArray();
			}
				
			mesh.RecalculateBounds ();
			mesh.RecalculateNormals ();
				
			part.AddComponent<MeshFilter> ();
			part.GetComponent<MeshFilter> ().mesh = mesh;

			part.AddComponent<MeshRenderer> ();
			//part.GetComponent<MeshRenderer> ().material = GameObject.Find ("MaterialPlaceHolder").GetComponent<MeshRenderer> ().sharedMaterial;
			Material material = new Material(Shader.Find("Standard (Vertex Color)"));
			Util.SetMaterialKeywords(material, WorkflowMode.Specular);
			part.GetComponent<MeshRenderer> ().material = material;
		}

		private X3DIndexedFaceSet ToTriangulatedFaceSet ()
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

				if (this.Faces [faceIdx].Count == 3) {
					triangles.AddLast (this.Faces [faceIdx]);
					if (!this.NormalPerVertex && this.Normals != null) {
						newNormals.AddLast (this.Normals [faceIdx]);
					}
					if (!this.ColorPerVertex && this.Colors != null) {
						newColors.AddLast (this.Colors [faceIdx]);
					}
				} else {
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
						if (!this.NormalPerVertex && this.Normals != null) {
							newNormals.AddLast (this.Normals [faceIdx]);
						}
						if (!this.ColorPerVertex && this.Colors != null) {
							newColors.AddLast (this.Colors [faceIdx]);
						}
					}
				}
			}
				
			List<Vector3> normals = this.Normals;
			if (!this.NormalPerVertex && this.Normals != null) {
				normals = new List<Vector3> (newNormals);
			}
			List<Color> colors = this.Colors;
			if (!this.ColorPerVertex && this.Colors != null) {
				colors = new List<Color> (newColors);
			}

			return new X3DIndexedFaceSet (this.Solid,
				this.NormalPerVertex, 
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
		private X3DIndexedFaceSet ToVertexOrientedFaceSet ()
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

			return new X3DIndexedFaceSet (this.Solid, true, true, vertices, normals, colors, faces);
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
	}

	public class X3DIndexedFaceSetHandler : X3DGeometryHandler
	{

		public X3DIndexedFaceSetHandler() : base("IndexedFaceSet") {
		}

		public override X3DGeometry ParseGeometry (XElement request)
		{
			return new X3DIndexedFaceSet (
				(bool)request.Attribute ("solid"),
				(bool)request.Attribute ("normalPerVertex"),
				(bool)request.Attribute ("colorPerVertex"),
				ParseVertices (request),
				ParseNormals (request),
				ParseColors (request), 
				ParseCoordIndex (request)
			);
		}
	}
}

