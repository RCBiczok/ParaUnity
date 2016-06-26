namespace ParaUnity
{
	using UnityEngine;
	using X3D;
    using System;
	using System.Collections.Generic;

	public class TestLoader : MonoBehaviour
	{

		// Use this for initialization
		void Start ()
		{
			//string path = getTestX2DFile("paraview_tutorial_data/disk_out_ref/disk_out_ref.x3d");
			//string path = getTestX2DFile("simple/Beam_Tet4_NElement=24_0.x3d");
			//string path = getTestX2DFile ("liver_colored/liver_colored.x3d");
			//string path = getTestX2DFile ("one_beam_cropped/one_beam_cropped.x3d");
			//string path = getTestX2DFile ("on_beam_colored/on_beam_colored.x3d");
			//string path = getTestX2DFile ("beam_colored/beam_colored_2.x3d");
			//string path = getTestX2DFile ("paraview_tutorial_data/disk_out_ref/disk_out_ref_wireframe.x3d");
			//string path = getTestX2DFile ("advanced/1.x3d");
			//string path = getTestX2DFile ("paraview_tutorial_data/disk_out_ref/disk_out_ref_outline.x3d");
			//string path = getTestX2DFile ("paraview_tutorial_data/disk_out_ref/disk_out_ref_surface_with_edges.x3d");
			//string path = getTestX2DFile ("paraview_tutorial_data/disk_out_ref/disk_out_ref_points.x3d");
			string path = getTestX2DFile ("liver_colored/animation");
			GameObject obj = Loader.ImportGameObject (path);
			obj.SetActive (true);
		}

        private string getTestX2DFile(string testFile)
        {
            return Environment.CurrentDirectory + "/../../TestMaterials/" + testFile;
        }
    }
}