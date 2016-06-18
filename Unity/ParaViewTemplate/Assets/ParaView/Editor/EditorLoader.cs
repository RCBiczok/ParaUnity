namespace ParaView
{
	using ParaUnity.X3D;
	using UnityEngine;
	using UnityEditor;
	using System.Collections;
	using System;
	using System.Text;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;

	[InitializeOnLoad]
	public class EditorLoader : MonoBehaviour
	{
		private static X3DLoader LOADER = new X3DLoader ();
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
				string importDir = GetImportDir (soc);
				if (!importDir.Equals ("TEST")) {
					Debug.Log ("Import dir:" + importDir);
					GameObject obj = (GameObject)LOADER.Load (importDir);
					obj.SetActive (true);
					UnityEngine.Object prefab = PrefabUtility.CreateEmptyPrefab("Assets/imported.prefab");
					PrefabUtility.ReplacePrefab(obj, prefab, ReplacePrefabOptions.ConnectToPrefab);
					soc.Disconnect (false);
				}
			}
		}
			
		private static string GetImportDir (Socket soc)
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