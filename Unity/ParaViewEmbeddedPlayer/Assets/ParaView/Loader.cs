using UnityEngine;
using ParaUnity.X3D;
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
	private volatile List<X3DMesh> unityMeshes = new List<X3DMesh> ();
	//True if file is loaded
	private bool loaded = false;
	private volatile string Path = "";


	// Use this for initialization
	void Start ()
	{
		//LoadFile ("/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/TestMaterials/simple/Beam_Tet4_NElement=24_0.x3d");
		LoadFile ("/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/TestMaterials/liver_colored/liver_colored.x3d");
		//LoadFile ("/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/TestMaterials/paraview_tutorial_data/disk_out_ref/disk_out_ref.x3d");
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (loaded) {
			StartCoroutine ("LoadFileExecute");
			unityMeshes = new List<X3DMesh> ();
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

		ThreadUtil2 t = new ThreadUtil2 (this.LoadFileWorker, this.LoadFileCallback);
		t.Run ();
        
		//Thread thread = new Thread(new ThreadStart(this.LoadFileWorker));
		//thread.Start();
		//LoadFileWorker();
	}

	//Runs in own thread
	private void LoadFileWorker (object sender, DoWorkEventArgs e)
	{
		X3DLoader loader = new X3DLoader ();
		unityMeshes = loader.Load (Path);
		return;
	}
		
	private void LoadFileCallback (object sender, RunWorkerCompletedEventArgs e)
	{        
		BackgroundWorker worker = sender as BackgroundWorker;
		if (e.Cancelled) {
			Debug.Log ("Loading cancelled");
		} else if (e.Error != null) {
			Debug.LogException (e.Error);
		} else {
			loaded = true;
		}
		return;
	}

	private IEnumerator LoadFileExecute ()
	{
		foreach (X3DMesh unityMesh in unityMeshes) {
			//Spawn object
			//TODO
			GameObject objToSpawn = new GameObject ("TODO");

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
			//mesh.name = unityMesh.Name;
			mesh.vertices = unityMesh.Vertices;
			mesh.triangles = unityMesh.Triangles;
			if (unityMesh.Normals != null) {
				mesh.normals = unityMesh.Normals;
			}
			if (unityMesh.Colors != null) {
				mesh.colors = unityMesh.Colors;
			}

			objToSpawn.GetComponent<MeshFilter> ().mesh = mesh;
			objToSpawn.GetComponent<MeshCollider> ().sharedMesh = mesh; //TODO Reduce mesh??

			objToSpawn.transform.localPosition = new Vector3 (0, 0, 0);
			//objToSpawn.transform.localScale = new Vector3(1, 1, 1);

			unityMeshes = new List<X3DMesh> ();
			loaded = false;
			Path = "";

			yield return null;
		}

		yield return null;
	}

}
