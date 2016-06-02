using UnityEngine;

using System;
using System.Collections;

namespace ParaUnity
{

	public enum MouseButton {
		Left = 0,
		Right = 1,
		Middle = 2,
	}

	public class CameraMovement : MonoBehaviour
	{

		private float speed = 4F;

		public Vector3 Target { get; set; }

		private bool clickAndHould = false;

		// Update is called once per frame
		void Update ()
		{
			if (Input.GetMouseButtonDown ((int)MouseButton.Left)) {
				this.clickAndHould = true;
			}
			if (Input.GetMouseButtonUp ((int)MouseButton.Left)) {
				this.clickAndHould = false;
			}
			if (this.clickAndHould) {
				Debug.Log (Input.GetAxis ("Mouse X"));
				Debug.Log (Input.GetAxis ("Mouse Y"));
				this.transform.RotateAround (Target, Vector3.up, Input.GetAxis("Mouse X")*speed);
			}
		}
	}

}