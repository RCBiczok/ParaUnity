namespace ParaUnity
{
	using UnityEngine;
	using ParaUnity.X3D;
	using System.Collections.Generic;

	public class TestLoader : MonoBehaviour
	{

		public GameObject meshNode;
		public Material defaultMaterial;

		// Use this for initialization
		void Start ()
		{
	
			string path = "/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/TestMaterials/paraview_tutorial_data/disk_out_ref/disk_out_ref.x3d";
			X3DLoader loader = new X3DLoader ();
			List<X3DMesh> meshes = loader.Load (path);

			foreach (X3DMesh unityMesh in meshes) {
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
			}
		}
	}
}