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

	public class Observer : MonoBehaviour
	{

		public GameObject meshNode;
		public Material defaultMaterial;

		TcpListener listener;

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

				string message = getMessage (soc);
				Debug.Log ("File:" + message);
				soc.Disconnect (false);
				string path = message;
				X3DLoader loader = new X3DLoader ();
				List<X3DMesh> meshes = loader.Load (path);

				for (int i = 0; i < meshNode.transform.childCount; i++) {
					Destroy (meshNode.transform.GetChild (i).gameObject);
				}

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

		void OnApplicationQuit ()
		{
			listener.Stop ();
			listener = null;
		}

		private string getMessage (Socket soc)
		{
			byte[] b = new byte[soc.Available];
			int k = soc.Receive (b);
			StringBuilder str = new StringBuilder ();
			for (int i = 0; i < k; i++) {
				str.Append (Convert.ToChar (b [i]));
			}
			return str.ToString ();
		}
	}
}