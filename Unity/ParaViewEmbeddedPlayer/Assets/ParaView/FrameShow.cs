using UnityEngine;

using System;
using System.Collections;

namespace ParaUnity
{

	public class FrameShow : MonoBehaviour
	{
		int currentFrame = 0;

		int delay = 0;

		const int DELAY_COUNT = 1;

		void Update () {
			if (delay >= DELAY_COUNT) {
				delay = 0;
				foreach (Transform child in transform) {
					child.gameObject.SetActive (false);
				}
				transform.GetChild (currentFrame).gameObject.SetActive (true);
				currentFrame = (currentFrame + 1) % transform.childCount;
			} else {
				delay++;
			}
		}
	}

}