namespace ParaView
{

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

		public GameObject meshNode;
		public UnityEngine.Material defaultMaterial;

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
				Debug.Log ("Import dir:" + importDir);
				soc.Disconnect (false);
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