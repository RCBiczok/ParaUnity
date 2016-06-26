namespace ParaUnity
{
	using ParaUnity.X3D;
	using UnityEngine;
	using UnityEditor;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Text;
	using System.IO;
	using System.Net;
	using System.Linq;
	using System.Net.Sockets;

	[InitializeOnLoad]
	public class EditorLoader : MonoBehaviour
	{
		private static TcpListener LISTENER;

		static EditorLoader()
		{
			LISTENER = new TcpListener (IPAddress.Loopback, 0);
			LISTENER.Start ();
			int port = ((IPEndPoint)LISTENER.LocalEndpoint).Port;
			Debug.Log ("Port: " + port);
			string editorWorkingDir = Path.GetTempPath () + "/Unity3DPlugin/Editor/" + port;
			Directory.CreateDirectory (editorWorkingDir);

			EditorApplication.update += Update;
		}

		private static void Update ()
		{
			if (LISTENER.Pending ()) {
				Socket soc = LISTENER.AcceptSocket ();
				string importDir = Loader.GetImportDir (soc);
				if (!importDir.Equals ("TEST")) {
					Debug.Log ("Importing from:" + importDir);
					GameObject obj = StripScene(Loader.ImportGameObject(importDir));
					obj.SetActive (true);
					//AssetDatabase.CreateAsset(obj, "Assets/imported.asset");
					//AssetDatabase.SaveAssets();
					soc.Disconnect (false);
				}
			}
		}
			
		private static GameObject StripScene(GameObject scene) {
			List<GameObject> physicalObjects = ExtractPhysicalObjects (scene);
			GameObject imported = new GameObject ("Imported");
			foreach (GameObject obj in physicalObjects) {
				obj.transform.parent = imported.transform;
			}
			GameObject.DestroyImmediate (scene);
			return imported;
		}

		private static List<GameObject> ExtractPhysicalObjects(GameObject obj) {
			if (obj.transform.childCount > 0 &&
				obj.transform.GetChild(0).GetComponent<Renderer> () != null) {
				return new List<GameObject> (){ obj };
			} else {
				List<GameObject> objects = new List<GameObject> ();
				foreach (Transform child in obj.transform) {
					objects.AddRange (ExtractPhysicalObjects(child.gameObject));
				}
				return objects;
			}
		}
	}
}