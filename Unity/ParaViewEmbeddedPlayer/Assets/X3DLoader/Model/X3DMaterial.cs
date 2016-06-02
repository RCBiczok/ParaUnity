using System;

namespace ParaUnity.X3D
{
	using UnityEngine;
	using System.Xml.Linq;

	public class X3DMaterial : X3DNode
	{
		public float? AmbientIntensity { get; private set; }

		public Color? EmissiveColor { get; private set; }

		public Color? DiffuseColor { get; private set; }

		public Color? SpecularColor { get; private set; }

		public float? Shininess { get; private set; }

		public float? Transparency { get; private set; }

		public X3DMaterial (float? ambientIntensity,  
		                    Color? emissiveColor,  
		                    Color? diffuseColor,
							Color? specularColor,
		                    float? shininess,
		                    float? transparency)
		{
			this.AmbientIntensity = ambientIntensity;
			this.EmissiveColor = emissiveColor;
			this.DiffuseColor = diffuseColor;
			this.SpecularColor = specularColor;
			this.Shininess = shininess;
			this.Transparency = transparency;
		}
	}

	sealed class X3DMaterialHandler : X3DHandler
	{
		public X3DMaterialHandler () : base ("Material")
		{
		}

		public override X3DNode Parse (XElement elem)
		{
			float? ambientIntensity = ParseFloatAttribute (elem.Attribute ("ambientIntensity")); 
			Color? emissiveColor = ParseColorAttribute (elem.Attribute ("emissiveColor")); 
			Color? diffuseColor = ParseColorAttribute (elem.Attribute ("diffuseColor"));   
			Color? specularColor = ParseColorAttribute (elem.Attribute ("specularColor"));   
			float? shininess = ParseFloatAttribute (elem.Attribute ("shininess"));
			float? transparency = ParseFloatAttribute (elem.Attribute ("transparency"));

			return new X3DMaterial (ambientIntensity, emissiveColor, diffuseColor, specularColor, shininess, transparency);
		}
	}
}

