namespace ParaUnity.X3D
{
	using UnityEngine;
	using System;
	using System.Linq;
	using System.Xml.Linq;

	public class X3DTransform : X3DContainer
	{
		public Vector3? Translation { get; private set; }
		public Rotation Rotation { get; private set; }
		public Vector3? Scale { get; private set; }

		public X3DTransform (X3DNode[] nodes,  
			Vector3? translation, 
			Rotation rotatio, 
			Vector3? scale) : base (nodes)
		{
			this.Translation = translation;
			this.Rotation = rotatio;
			this.Scale = scale;
		}

		override public void Convert (GameObject parent) {
			GameObject gameObject = new GameObject (this.GetType().Name);

			gameObject.transform.parent = parent.transform;

			foreach (X3DNode child in this.Children) {
				child.Convert (gameObject);
			}

			if (this.Scale != null) {
				gameObject.transform.localScale = this.Scale.Value;
			}
			if(this.Translation != null) {
				gameObject.transform.Translate (this.Translation.Value);
			}
			if (this.Rotation != null) {
				gameObject.transform.Rotate (this.Rotation.Axis, this.Rotation.Angle * Mathf.Rad2Deg);
			}
		}
	}

	sealed class X3DTransformHandler : X3DContainerHandler
	{
		public static X3DHandler[] DEFAULT_HANDLERS = new X3DHandler[] { 
			new X3DBackgroundHandler (), 
			new X3DViewpointHandler (),
			new X3DDirectionalLightHandler (),
			new X3DShapeHandler ()
		};

		public X3DTransformHandler () : base ("Transform", true, DEFAULT_HANDLERS)
		{
		}

		protected override X3DContainer ParseContainer (X3DNode[] children, XElement elem)
		{
			
			Vector3? translation = ParseVectorAttribute (elem.Attribute ("translation")); 
			Rotation rotatinon = ParseRotationAttribute (elem.Attribute ("rotation")); 
			Vector3? scale = ParseVectorAttribute (elem.Attribute ("scale")); 

			return new X3DTransform (children, translation, rotatinon, scale);
		}
	}
}

