namespace ParaUnity
{
	using UnityEngine;
	using X3D;
    using System;
	using System.Collections.Generic;

	public class TestLoader : MonoBehaviour
	{

		public GameObject meshNode;
		public Material defaultMaterial;
		private GameObject[] frames;
		private int idx = 0;

		// Use this for initialization
		void Start ()
		{
			//string path = getTestX2DFile("paraview_tutorial_data/disk_out_ref/disk_out_ref.x3d");
			//string path = getTestX2DFile("simple/Beam_Tet4_NElement=24_0.x3d");
			//string path = getTestX2DFile ("liver_colored/liver_colored.x3d");
			string path = getTestX2DFile ("one_beam_cropped/one_beam_cropped.x3d");
			//string path = getTestX2DFile ("liver_colored/animation");
			frames = Loader.ImportMesh (path, meshNode);
			frames [0].SetActive (true);
		}

		/*public void Update ()
		{
			if (frames != null) {
				frames [idx % frames.Length].transform.parent = null;
				frames [idx % frames.Length].SetActive (false);
				frames [(idx + 1) % frames.Length].transform.parent = meshNode.transform;
				frames [(idx + 1) % frames.Length].SetActive (true);
				idx++;
				if (idx == frames.Length) {
					idx = 0;
				}
				Debug.Log (idx);
			}
		}*/

        private string getTestX2DFile(string testFile)
        {
            return Environment.CurrentDirectory + "/../../TestMaterials/" + testFile;
        }
    }
}