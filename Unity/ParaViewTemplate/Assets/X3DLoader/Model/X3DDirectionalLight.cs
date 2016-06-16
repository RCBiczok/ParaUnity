namespace ParaUnity.X3D
{
	using System.Xml.Linq;
	using UnityEngine;

	public class X3DDirectionalLight : X3DNode
	{
		public Vector3? Direction { get; private set; }

		public Color? Color { get; private set; }

		public float? Intensity { get; private set; }

		public float? AmbientIntensity { get; private set; }

		public bool? On { get; private set; }

		public X3DDirectionalLight (Vector3? direction,
		                            Color? color, 
		                            float? intensity,
		                            float? ambientIntensity,
		                            bool? on)
		{
			this.Direction = direction;
			this.Color = color;
			this.Intensity = intensity;
			this.AmbientIntensity = ambientIntensity;
			this.On = on;
		}

		override public void Convert (GameObject parent)
		{
			GameObject lightObj = new GameObject (this.GetType().Name);
			lightObj.transform.parent = parent.transform;
			Light light = lightObj.AddComponent<Light> ();
			light.type = LightType.Directional;
			if (this.Direction != null) {
				light.transform.rotation = Quaternion.LookRotation (this.Direction.Value);
			}
			if(this.AmbientIntensity != null) {
				RenderSettings.ambientIntensity = this.AmbientIntensity.Value;
			}
			light.color = this.Color ?? light.color;
			light.intensity = this.Intensity ?? light.intensity;
		}
	}

	sealed class X3DDirectionalLightHandler : X3DHandler
	{
		public X3DDirectionalLightHandler () : base ("DirectionalLight")
		{
		}

		public override X3DNode Parse (XElement elem)
		{
			Vector3? direction = ParseVectorAttribute (elem.Attribute ("direction")); 
			Color? color = ParseColorAttribute (elem.Attribute ("color"));
			float? intensity = ParseFloatAttribute (elem.Attribute ("intensity")); 
			float? ambientIntensity = ParseFloatAttribute (elem.Attribute ("ambientIntensity")); 
			bool? on = ParseBoolAttribute (elem.Attribute ("on"));

			return new X3DDirectionalLight (direction, color, intensity, ambientIntensity, on);
		}
	}
}

