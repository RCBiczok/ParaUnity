using UnityEngine;
using BlenderMeshReader;
using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;

public class Loader : MonoBehaviour
{

	public GameObject meshNode;
	public Material defaultMaterial;

	//List of lists of UnityMeshes with max. 2^16 vertices per mesh
	private volatile List<List<UnityMesh>> unityMeshes = new List<List<UnityMesh>> ();
	//True if file is loaded
	private bool loaded = false;
	private volatile string Path = "";


	// Use this for initialization
	void Start ()
	{
		LoadFile ("/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/Unity/ParaViewEmbeddedPlayer/Assets/paraview_output.blend");
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (loaded) {
			StartCoroutine ("LoadFileExecute");
			unityMeshes = new List<List<UnityMesh>> ();
			loaded = false;
			Path = "";
		}
	}

	public void LoadFile (string path)
	{
		//Destroy current game objectes attached to mesh node
		for (int i = 0; i < meshNode.transform.childCount; i++) {
			Destroy (meshNode.transform.GetChild (i).gameObject);
		}
		this.Path = path;

		ThreadUtil t = new ThreadUtil (this.LoadFileWorker, this.LoadFileCallback);
		t.Run ();
        
		//Thread thread = new Thread(new ThreadStart(this.LoadFileWorker));
		//thread.Start();
		//LoadFileWorker();
	}

	//Runs in own thread
	private void LoadFileWorker (object sender, DoWorkEventArgs e)
	{
		BlenderFile b = new BlenderFile (Path);
		List<BlenderMesh> blenderMeshes = new List<BlenderMesh> ();
		blenderMeshes = b.readMesh ();
		unityMeshes = new List<List<UnityMesh>> ();
		foreach (BlenderMesh m in blenderMeshes) {
			List<UnityMesh> l = new List<UnityMesh> ();
			l.Add (m.ToUnityMesh ());
			unityMeshes.Add (l);
		}
		//unityMeshes = BlenderFile.createSubmeshesForUnity(blenderMeshes);
		return;
	}


	private void LoadFileCallback (object sender, RunWorkerCompletedEventArgs e)
	{        
		if (e.Cancelled) {
			Debug.Log ("Loading cancelled");
		} else if (e.Error != null) {
			Debug.LogError ("Error while loading the mesh: " + e.Error);
		} else {
			loaded = true;
		}
		return;
	}

	private IEnumerator LoadFileExecute ()
	{
		foreach (List<UnityMesh> um in unityMeshes) {
			foreach (UnityMesh unityMesh in um) {
				//Spawn object
				GameObject objToSpawn = new GameObject (unityMesh.Name);

				objToSpawn.transform.parent = meshNode.transform;

				//Add Components
				objToSpawn.AddComponent<MeshFilter> ();
				objToSpawn.AddComponent<MeshCollider> (); //TODO need to much time --> own thread?? Dont work in Unity!!
				objToSpawn.AddComponent<MeshRenderer> ();
                
				//Add material
				objToSpawn.GetComponent<MeshRenderer> ().material = defaultMaterial;

				objToSpawn.GetComponent<MeshRenderer> ().material.shader = Shader.Find ("Standard (Vertex Color)");
               
				//Create Mesh
				Mesh mesh = new Mesh ();
				mesh.name = unityMesh.Name;

				Debug.Log (unityMesh.ColorList.Length);
				Debug.Log (unityMesh.VertexList.Length);
				Debug.Log (unityMesh.TriangleList.Length);

				mesh.vertices = unityMesh.VertexList;
				mesh.normals = unityMesh.NormalList;
				mesh.triangles = unityMesh.TriangleList;
				//mesh.colors = unityMesh.ColorList;

				mesh.RecalculateNormals ();
				mesh.RecalculateBounds();
				//mesh = PostProcessMesh (mesh);

				objToSpawn.GetComponent<MeshFilter> ().mesh = mesh;

				objToSpawn.transform.localPosition = new Vector3 (0, 0, 0);
				objToSpawn.transform.Rotate (Vector3.right * 180);
				//objToSpawn.transform.localScale = new Vector3(1, 1, 1);

				//mesh.triangles = GameObject.Find ("paraview_output").GetComponent<MeshFilter> ().mesh.triangles;
				DumpMash (GameObject.Find ("paraview_output").GetComponent<MeshFilter> ().mesh, "/Users/rcbiczok/Desktop/importet.txt");
				DumpMash (mesh, "/Users/rcbiczok/Desktop/loaded.txt");

				unityMeshes = new List<List<UnityMesh>> ();
				loaded = false;
				Path = "";

				Camera cam = GameObject.Find ("Camera").GetComponent<Camera> ();
				cam.transform.LookAt (objToSpawn.transform);

				//cam.transform.position = Vector3.Lerp (cam.transform.position, objToSpawn.transform.position, 0.9f);

				yield return null;
			}
            
		}

		yield return null;
	}


	private Mesh PostProcessMesh (Mesh mesh)
	{
		Mesh filtredMesh = FilterUnusedVectors (mesh);
		int[] newTriangles = filtredMesh.triangles;
		List<Vector3> newVecs = new List<Vector3> (filtredMesh.vertices);
		List<Vector3> newNormals = new List<Vector3> (filtredMesh.normals);
		List<Color> newColors = new List<Color> (filtredMesh.colors);

		for (int vertexId = 0; vertexId < filtredMesh.vertices.Length; vertexId++) {
			int firstTriangleId = -1;
			for (int triangleIdx = 0; triangleIdx < newTriangles.Length; triangleIdx++) {
				if (newTriangles [triangleIdx] == vertexId) {
					if (firstTriangleId == -1) {
						firstTriangleId = triangleIdx / 3;
					} else if (firstTriangleId != triangleIdx / 3) {
						//if (AngleOf (newVecs, newTriangles, vertexId, firstTriangleId, triangleIdx / 3) > 0.001) {
							newTriangles [triangleIdx] = newVecs.Count;
							newVecs.Add (filtredMesh.vertices [vertexId]);
							if (vertexId < filtredMesh.normals.Length) {
								newNormals.Add (filtredMesh.normals [vertexId]);
							}
							if (vertexId < filtredMesh.colors.Length) {
								newColors.Add (filtredMesh.colors [vertexId]);
							}
						//}
					}
				}
			}
		}
			
		Mesh newMesh = new Mesh ();
		newMesh.vertices = newVecs.ToArray ();
		newMesh.normals = newNormals.ToArray ();
		newMesh.colors = newColors.ToArray ();
		newMesh.triangles = newTriangles;
		//newMesh.Optimize ();
		//newMesh.RecalculateBounds ();
		newMesh.RecalculateNormals ();
		return newMesh;
	}

	private float AngleOf (List<Vector3> vectors, int[] triangles, int vertexId, int triangle1Id, int triangle2Id)
	{

		Debug.Log ("(" + triangles [3 * triangle1Id]
		+ "," + triangles [3 * triangle1Id + 1]
		+ "," + triangles [3 * triangle1Id + 2]
		+ "):(" + triangles [3 * triangle2Id]
		+ "," + triangles [3 * triangle2Id + 1]
		+ "," + triangles [3 * triangle2Id + 2] + ")");
	
		Vector3 p1 = vectors [vertexId];
		Vector3 p2;
		Vector3 p3;
		if (triangles [3 * triangle1Id] == vertexId) {
			p2 = vectors [triangles [3 * triangle1Id + 1]];
			p3 = vectors [triangles [3 * triangle1Id + 2]];
		} else if (triangles [3 * triangle1Id + 1] == vertexId) {
			p2 = vectors [triangles [3 * triangle1Id]];
			p3 = vectors [triangles [3 * triangle1Id + 2]];
		} else {
			p2 = vectors [triangles [3 * triangle1Id]];
			p3 = vectors [triangles [3 * triangle1Id + 1]];
		}
		Vector3 p4;
		if (triangles [3 * triangle2Id] != triangles [3 * triangle1Id]
		    && triangles [3 * triangle2Id] != triangles [3 * triangle1Id + 1]
		    && triangles [3 * triangle2Id] != triangles [3 * triangle1Id + 2]) {
			p4 = vectors [triangles [3 * triangle2Id]];
		} else if (triangles [3 * triangle2Id + 1] != triangles [3 * triangle1Id]
		           && triangles [3 * triangle2Id + 1] != triangles [3 * triangle1Id + 1]
		           && triangles [3 * triangle2Id + 1] != triangles [3 * triangle1Id + 2]) {
			p4 = vectors [triangles [3 * triangle2Id + 1]];
		} else {
			p4 = vectors [triangles [3 * triangle2Id + 2]];
		}
			
		Debug.Log (p1 + ":" + p2 + ":" + p3 + ":" + p4 + Math.Abs(Vector3.Dot (Vector3.Cross (p3 - p1, p2 - p1).normalized, 
			Vector3.Cross (p4 - p1, p2 - p1).normalized)));

		return Math.Abs(Vector3.Dot (Vector3.Cross (p3 - p1, p2 - p1).normalized, 
			Vector3.Cross (p4 - p1, p2 - p1).normalized));
	}

	private Mesh FilterUnusedVectors (Mesh mesh)
	{
		Vector3[] oldVecs = mesh.vertices;
		Vector3[] oldNormals = mesh.normals;
		Color[] oldColors = mesh.colors;
		int[] newTriangles = mesh.triangles;
		ArrayList newVecs = new ArrayList (oldVecs.Length);
		ArrayList newNormals = new ArrayList (oldNormals.Length);
		ArrayList newColors = new ArrayList (oldColors.Length);

		bool[] usedVecs = new bool[oldVecs.Length];
		foreach (int i in newTriangles) {
			usedVecs [i] = true;
		}

		for (int i = 0; i < oldVecs.Length; i++) {
			if (usedVecs [i]) {
				newVecs.Add (oldVecs [i]);
				if (i < oldNormals.Length) {
					newNormals.Add (oldNormals [i]);
				}
				if (i < oldColors.Length) {
					newColors.Add (oldColors [i]);
				}
			} else {
				for (int j = 0; j < newTriangles.Length; j++) {
					if (i < newTriangles [j]) {
						newTriangles [j]--;
					}
				}
			}
		}
		Mesh newMesh = new Mesh ();
		newMesh.vertices = (Vector3[])newVecs.ToArray (typeof(Vector3));
		newMesh.normals = (Vector3[])newNormals.ToArray (typeof(Vector3));
		newMesh.colors = (Color[])newColors.ToArray (typeof(Color));
		newMesh.triangles = newTriangles;
		return newMesh;
	}

	private void DumpMash (Mesh mesh, string outFile)
	{
		using (System.IO.StreamWriter file = 
			       new System.IO.StreamWriter (outFile)) {
			file.WriteLine ("Vertices:" + mesh.vertices.Length);
			/*foreach (Vector3 v in mesh.vertices) {
				file.WriteLine (v);
			}*/
			file.WriteLine ("Normals:" + mesh.normals.Length);
			/*foreach (Vector3 n in mesh.normals) {
				file.WriteLine (n);
			}*/
			file.WriteLine ("Triangles:" + mesh.triangles.Length);
			/*foreach (int n in mesh.triangles) {
				file.WriteLine (n);
			}*/
		}
	}
}
