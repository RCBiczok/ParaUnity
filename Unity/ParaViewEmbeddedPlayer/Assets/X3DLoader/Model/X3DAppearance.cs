using System;

namespace ParaUnity.X3D
{
	using System.Linq;
	using System.Xml.Linq;

	public class X3DAppearance : X3DContainer
	{
		public X3DAppearance (X3DNode[] nodes) : base (nodes)
		{
		}
	}

	sealed class X3DAppearanceHandler : X3DContainerHandler
	{

		private static X3DHandler[] HANDLERS = new X3DHandler[]{new X3DMaterialHandler(), new X3DIndexedFaceSetHandler()};

		public X3DAppearanceHandler () : base ("Appearance", false, HANDLERS)
		{
		}

		protected override X3DContainer ParseContainer (X3DNode[] nodes, XElement elem)
		{
			return new X3DAppearance (nodes);
		}
	}
}

