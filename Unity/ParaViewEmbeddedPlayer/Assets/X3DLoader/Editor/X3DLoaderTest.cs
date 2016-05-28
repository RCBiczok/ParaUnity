namespace ParaUnity.X3D
{
	using System;
	using NUnit.Framework;

	[TestFixture]
	[Category ("X3D Loader")]
	internal class X3DLoaderTest
	{

		private X3DLoader loader = new X3DLoader ();

		[Test]
		public void TransferFunds ()
		{
			//string testFile = getTestX2DFile ("simple/Beam_Tet4_NElement=24_0.x3d");
			//string testFile = getTestX2DFile ("liver_colored/liver_colored.x3d");
			string testFile = getTestX2DFile ("paraview_tutorial_data/disk_out_ref/disk_out_ref.x3d");

			loader.Load(testFile);
			Assert.AreEqual (1, 1);
		}

		private string getTestX2DFile (string testFile)
		{
			return Environment.CurrentDirectory + "/../../../../../TestMaterials/" + testFile;
		}
	}
}
