namespace ParaUnity
{
	using ParaUnity.X3D;
	using UnityEngine;
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;

	public class Loader : MonoBehaviour
	{

		private static X3DLoader LOADER = new X3DLoader ();

		public GameObject meshNode;

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

				for (int i = 0; i < meshNode.transform.childCount; i++) {
					Destroy (meshNode.transform.GetChild (i).gameObject);
				}

				string importDir = GetImportDir (soc);
				Debug.Log ("Import dir:" + importDir);
				soc.Disconnect (false);
				ImportMesh(importDir, meshNode);
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

		public static GameObject[] ImportMesh(string file, GameObject meshNode)
		{
			FileAttributes attr = File.GetAttributes(file);

			if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
				DirectoryInfo d = new DirectoryInfo(file);
				return d.GetFiles("*.x3d").Select(frameFile => (GameObject)LOADER.Load (file +"/" +frameFile.Name)).ToArray();
			} else {
				GameObject ob = (GameObject)LOADER.Load (file);
				return new GameObject[]{ob};
			}
		}
	}
}