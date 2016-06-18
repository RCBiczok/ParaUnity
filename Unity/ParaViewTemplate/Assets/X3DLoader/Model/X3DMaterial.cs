namespace ParaUnity.X3D
{
	using System;
	using System.Xml.Linq;
	using UnityEngine;
	using UnityVC;

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

		override public void Convert (GameObject obj)
		{
			obj.AddComponent<MeshRenderer> ();
			obj.GetComponent<MeshRenderer> ().material = GameObject.Find ("MaterialPlaceHolder").GetComponent<MeshRenderer> ().material;
			//obj.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Standard (Vertex Color)"));
			//Util.SetMaterialKeywords(obj.GetComponent<MeshRenderer> ().material, WorkflowMode.Specular);
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
