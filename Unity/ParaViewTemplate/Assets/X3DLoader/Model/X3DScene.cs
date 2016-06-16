using System;

namespace ParaUnity.X3D
{
	using UnityEngine;
	using System.Linq;
	using System.Xml.Linq;

	public class X3DScene : X3DContainer
	{
		public X3DScene (X3DNode[] nodes) : base (nodes)
		{
		}
			
		override public void Convert (GameObject parent) {
			base.Convert (parent);
			foreach (X3DBackground b in this.Children.Where(node => node is X3DBackground)) {
				foreach (Camera cam in parent.GetComponentsInChildren<Camera>()) {
					cam.backgroundColor = b.SkyColor ?? cam.backgroundColor;
				}
			}
		}
	}

	sealed class X3DSceneHandler : X3DContainerHandler
	{

		public X3DSceneHandler () : base ("Scene", false, 
			X3DTransformHandler.DEFAULT_HANDLERS.Concat (new X3DHandler[] {new X3DTransformHandler ()}).ToArray ())
		{
		}

		protected override X3DContainer ParseContainer (X3DNode[] nodes, XElement elem)
		{
			return new X3DScene (nodes);
		}
	}
}

