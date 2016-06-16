namespace ParaUnity.X3D
{
	using UnityEngine;
	using System.Linq;
	using System.Xml.Linq;
	using System.Collections.Generic;

	public class X3DBackground :  X3DNode
	{
		public Color? SkyColor { get; private set; }

		public X3DBackground (Color? skyColor)
		{
			this.SkyColor = skyColor;
		}
	}

	sealed class X3DBackgroundHandler : X3DHandler
	{
		public X3DBackgroundHandler() : base("Background") {
		}

		public override X3DNode Parse (XElement elem)
		{
			Color? skyColor = ParseColorAttribute (elem.Attribute ("skyColor"));
			return new X3DBackground(skyColor);
		}
	}
}

