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
	using System.Text.RegularExpressions;

	public class PlayerLoader : MonoBehaviour
	{

		private GameObject meshNode;

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

				Destroy (meshNode);

				string importDir = Loader.GetImportDir (soc);
				Debug.Log ("Importing from:" + importDir);
				soc.Disconnect (false);
				meshNode = Loader.ImportGameObject(importDir);
				meshNode.SetActive (true);
			}
		}

		void OnApplicationQuit ()
		{
			listener.Stop ();
			listener = null;
		}
	}
}