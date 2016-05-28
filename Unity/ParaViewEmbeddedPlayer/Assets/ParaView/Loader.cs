namespace ParaUnity
{
	using UnityEngine;
	using ParaUnity.X3D;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;

	public class Loader : MonoBehaviour
	{

		private static X3DLoader LOADER = new X3DLoader ();

		public GameObject meshNode;
		public Material defaultMaterial;

		private TcpListener listener;

		public void Start ()
		{
			listener = new TcpListener (IPAddress.Loopback, 0);
			listener.Start ();
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			Debug.Log ("Port: " + port);

			string embeddedPlayerPath = Path.GetTempPath () + "/Unity3DPlugin/Embedded/"
			                            + System.Diagnostics.Process.GetCurrentProcess ().Id;
			Directory.CreateDirectory (embeddedPlayerPath);

			string portFile = embeddedPlayerPath + "/port" + port;
			using (File.Create (portFile)) {
			}
			;
		}

		public void Update ()
		{
			if (listener.Pending ()) {
				Socket soc = listener.AcceptSocket ();

				string importDir = GetImportDir (soc);
				Debug.Log ("Import dir:" + importDir);
				soc.Disconnect (false);
				ImportFrom(importDir, meshNode, defaultMaterial);
			}
		}

		void OnApplicationQuit ()
		{
			listener.Stop ();
			listener = null;
		}

		private string GetImportDir (Socket soc)
		{
			byte[] b = new byte[soc.Available];
			int k = soc.Receive (b);
			StringBuilder str = new StringBuilder ();
			for (int i = 0; i < k; i++) {
				str.Append (Convert.ToChar (b [i]));
			}
			return str.ToString ();
		}

		public static void ImportFrom(string importDir, GameObject meshNode, Material defaultMaterial) {
			ImportMesh(importDir+"/frame_0.x3d", meshNode, defaultMaterial);
		}

		public static void ImportMesh(string file, GameObject meshNode, Material defaultMaterial)
		{
			List<X3DMesh> meshes = LOADER.Load (file);

			for (int i = 0; i < meshNode.transform.childCount; i++) {
				Destroy (meshNode.transform.GetChild (i).gameObject);
			}

			foreach (X3DMesh unityMesh in meshes) {
				//Spawn object
				GameObject objToSpawn = new GameObject ();

				objToSpawn.transform.parent = meshNode.transform;

				//Add Components
				objToSpawn.AddComponent<MeshFilter> ();
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

				objToSpawn.transform.localPosition = new Vector3 (0, 0, 0);
			}
		}
	}
}