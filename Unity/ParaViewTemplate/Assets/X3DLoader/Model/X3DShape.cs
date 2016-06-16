namespace ParaUnity.X3D
{
	using System.Linq;
	using System.Xml.Linq;

	public class X3DShape : X3DContainer
	{
		public X3DShape (X3DNode[] nodes) : base (nodes)
		{
		}
	}

	sealed class X3DShapeHandler : X3DContainerHandler
	{

		private static X3DHandler[] HANDLERS = new X3DHandler[]{new X3DAppearanceHandler(), new X3DIndexedFaceSetHandler()};

		public X3DShapeHandler () : base ("Shape", false, HANDLERS)
		{
		}

		protected override X3DContainer ParseContainer (X3DNode[] nodes, XElement elem)
		{
			return new X3DShape (nodes);
		}
	}
}

