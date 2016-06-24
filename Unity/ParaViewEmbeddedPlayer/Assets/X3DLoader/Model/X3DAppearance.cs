using System;

namespace ParaUnity.X3D
{
	using UnityEngine;
	using System.Linq;
	using System.Xml.Linq;

	public class X3DAppearance : X3DNode
	{
		public X3DMaterial Material { get; private set; }

		public X3DAppearance (X3DMaterial material)
		{
			this.Material = material;
		}

		override public void Convert (GameObject parent)
		{
			foreach (Transform child in parent.transform) {
				this.Material.Convert (child.gameObject);
			}
		}
	}

	sealed class X3DAppearanceHandler : X3DHandler
	{

		private X3DMaterialHandler materialHander = new X3DMaterialHandler();

		public X3DAppearanceHandler() : base("Appearance") {
		}

		public override X3DNode Parse (XElement elem)
		{
			return new X3DAppearance((X3DMaterial)materialHander.Parse (elem.Element("Material")));
		}
	}
}

