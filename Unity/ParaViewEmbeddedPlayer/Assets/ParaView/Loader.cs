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

	public class Loader
	{

		private static X3DLoader LOADER = new X3DLoader ();

		public static string GetImportDir (Socket soc)
		{
			byte[] b = new byte[soc.Available];
			int k = soc.Receive (b);
			StringBuilder str = new StringBuilder ();
			for (int i = 0; i < k; i++) {
				str.Append (Convert.ToChar (b [i]));
			}
			return str.ToString ();
		}

		public static GameObject ImportGameObject(string file)
		{
			List<GameObject> frames = ImportFrames(file);
			MergeFrames (frames);
			for (int i = 1; i < frames.Count; i++) {
				GameObject.Destroy (frames[i]);
			}
			return frames [0];
		}

		private static List<GameObject> ImportFrames(string file)
		{
			FileAttributes attr = File.GetAttributes(file);

			if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
				DirectoryInfo d = new DirectoryInfo(file);
				return d.GetFiles("*.x3d").OrderBy(x => Int32.Parse(Regex.Match(x.Name, @"\d+").Value)).
					Select(frameFile => (GameObject)LOADER.Load (file +"/" +frameFile.Name)).ToList();
			} else {
				GameObject ob = (GameObject)LOADER.Load (file);
				return new List<GameObject> () {ob};
			}
		}

		private static GameObject MergeFrames(List<GameObject> frames) {
			if (frames [0].transform.childCount > 0 &&
				frames [0].transform.GetChild(0).GetComponent<Renderer> () != null) {
				GameObject frameContainer = new GameObject ("FramedObject");
				frameContainer.transform.parent = frames [0].transform.parent;
				frames [0].transform.parent = frameContainer.transform;
				for (int i = 1; i < frames.Count; i++) {
					frames[i].transform.parent = frameContainer.transform;
					frames [i].SetActive (false);
				}
				frameContainer.AddComponent<FrameShow> ();
			} else {
				for (int i = 0; i < frames [0].transform.childCount; i++) {
					MergeFrames (frames.Select (obj => obj.transform.GetChild (i).gameObject).ToList ());
				}
			}
			return frames [0];
		}
	}
}