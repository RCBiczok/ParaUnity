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

		private float rotationSpeed = 4f;
		private float speedFactor = 0.1f;
		private Vector3 target;
		private float initialDistance;

		public Vector3 Target 
			{ 
				get { 
					return target; 
				} 
				set {
					this.target = value;
					this.initialDistance = Vector3.Distance (this.transform.position, target);
					Debug.Log (this.initialDistance);
				}
		}

		private bool leftClickedAndHould;
		private bool rightClickedAndHould;
		private bool middleClickedAndHould;

		// Update is called once per frame
		void Update ()
		{
			if (Input.GetMouseButtonDown ((int)MouseButton.Left)) {
				this.leftClickedAndHould = true;
			}
			if (Input.GetMouseButtonUp ((int)MouseButton.Left)) {
				this.leftClickedAndHould = false;
			}
			if (Input.GetMouseButtonDown ((int)MouseButton.Right)) {
				this.rightClickedAndHould = true;
			}
			if (Input.GetMouseButtonUp ((int)MouseButton.Right)) {
				this.rightClickedAndHould = false;
			}
			if (Input.GetMouseButtonDown ((int)MouseButton.Middle)) {
				this.middleClickedAndHould = true;
			}
			if (Input.GetMouseButtonUp ((int)MouseButton.Middle)) {
				this.middleClickedAndHould = false;
			}

			if (this.leftClickedAndHould) {
				this.transform.RotateAround (Target, Vector3.up, 
					Input.GetAxis("Mouse X") * rotationSpeed);
				this.transform.RotateAround (Target, Vector3.left, 
					Input.GetAxis("Mouse Y") * rotationSpeed);
			}
			if (this.rightClickedAndHould) {
				transform.Translate(Vector3.forward * 
					Input.GetAxis("Mouse Y") * speedFactor * initialDistance);
			}
			if (this.middleClickedAndHould) {
				transform.Translate(Vector3.up * Input.GetAxis("Mouse Y") * speedFactor * initialDistance);
				transform.Translate(Vector3.right * Input.GetAxis("Mouse X") * speedFactor * initialDistance);
			} 

			if (Input.GetAxis("Mouse ScrollWheel") != 0)
			{
				transform.Translate(Vector3.forward * Input.GetAxis("Mouse ScrollWheel") * speedFactor * initialDistance);
			}
		}
	}

}