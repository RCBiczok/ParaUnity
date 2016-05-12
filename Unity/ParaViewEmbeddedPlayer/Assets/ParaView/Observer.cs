using UnityEngine;

using System;
using System.Collections;
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
		listener = new TcpListener (IPAddress.Loopback, 51796);
		listener.Start ();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		Debug.Log ("Port: " + port);
	}

	public void Update ()
	{
		if (listener.Pending ()) {
			Socket soc = listener.AcceptSocket ();

			Debug.Log ("Connection accepted from " + soc.RemoteEndPoint);

			string message = getMessage (soc);
			Debug.Log (message);

			GameObject.Find ("Camera").GetComponent<Camera> ().GetComponent<Loader> ().LoadFile (message);
		}
	}

	void OnApplicationQuit ()
	{
		if (listener != null) {
			listener.Stop ();
			listener = null;
		}
	}

	private string getMessage (Socket soc)
	{
		byte[] b = new byte[255];
		int k = soc.Receive (b);
		StringBuilder str = new StringBuilder ();
		for (int i = 0; i < k; i++) {
			str.Append (Convert.ToChar (b [i]));
		}
		return str.ToString ();
	}
}