namespace ParaUnity.X3D
{
	using UnityEngine;
	using System;
	using System.Linq;
	using System.Xml.Linq;

	public class X3DViewpoint :  X3DNode
	{
		public float? FieldOfView { get; private set; }

		public Vector3? Position { get; private set; }

		public Rotation Orientation { get; private set; }

		public Vector3? CenterOfRotation { get; private set; }

		public X3DViewpoint (float? fieldOfView, Vector3? position, 
		                     Rotation orientation, Vector3? centerOfRotation)
		{
			this.FieldOfView = fieldOfView;
			this.Position = position;
			this.Orientation = orientation;
			this.CenterOfRotation = centerOfRotation;
		}

		override public void Convert (GameObject parent) {
			GameObject camObj = new GameObject (this.GetType().Name);
			camObj.transform.parent = parent.transform;
			Camera cam = camObj.AddComponent<Camera> ();
			cam.clearFlags = CameraClearFlags.Color;
			if (this.FieldOfView != null) {
				cam.fieldOfView = this.FieldOfView.Value * Mathf.Rad2Deg;
			}
			if( this.Position != null) {
				camObj.transform.position = this.Position.Value;
			}
				
			//TODO: Not sure if this is the best way to adjust the camera focus
			camObj.transform.LookAt(this.CenterOfRotation.Value);
			/*if (this.Orientation != null) {
				if (this.CenterOfRotation != null) {
					camObj.transform.RotateAround(this.CenterOfRotation.Value, 
						this.Orientation.Axis, this.Orientation.Angle * Mathf.Rad2Deg);
				} 
				else  {
					camObj.transform.Rotate (Orientation.Axis, Orientation.Angle * Mathf.Rad2Deg);
				} 
			}*/

			CameraMovement m = camObj.AddComponent<CameraMovement> ();
			if (this.CenterOfRotation != null) {
				m.Target = this.CenterOfRotation.Value;
			}

		}
	}

	sealed class X3DViewpointHandler : X3DHandler
	{
		public X3DViewpointHandler () : base ("Viewpoint")
		{
		}

		public override X3DNode Parse (XElement elem)
		{
			float? fieldOfView = ParseFloatAttribute (elem.Attribute ("fieldOfView"));
			Vector3? position = ParseVectorAttribute(elem.Attribute("position"));
			Rotation orientation = ParseRotationAttribute (elem.Attribute ("orientation"));
			Vector3? centerOfRotation = ParseVectorAttribute(elem.Attribute("centerOfRotation"));

			return new X3DViewpoint (fieldOfView, position, orientation, centerOfRotation);
		}
	}
}
