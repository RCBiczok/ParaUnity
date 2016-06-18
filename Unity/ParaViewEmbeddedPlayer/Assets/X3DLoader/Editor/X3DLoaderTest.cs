namespace ParaUnity.X3D
{
	using System;
	using System.Collections.Generic;
	using NUnit.Framework;

	[TestFixture]
	[Category ("X3D Loader")]
	internal class X3DLoaderTest
	{

		private X3DLoader loader = new X3DLoader ();

		[Test]
		public void Test ()
		{
			//string testFile = getTestX2DFile ("simple/Beam_Tet4_NElement=24_0.x3d");
			//string testFile = getTestX2DFile ("liver_colored/liver_colored.x3d");
			string testFile = getTestX2DFile ("paraview_tutorial_data/disk_out_ref/disk_out_ref.x3d");

			X3DScene scene = loader.Load (testFile);
			Console.WriteLine (((X3DMaterial)((X3DAppearance)(((X3DShape)((X3DTransform)((X3DTransform)(scene.Children[3])).Children[4]).Children[0]).Appearance)).Material).Shininess);
			Assert.AreEqual (1, 1);
		}

		private string getTestX2DFile (string testFile)
		{
			return Environment.CurrentDirectory + "/../../../../../TestMaterials/" + testFile;
		}
	}
}
