using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorTriggerSwitch2 : MonoBehaviour {
	public GameObject colorTile;
	public static bool colorStatus = false;

	void OnTriggerEnter(Collider collider) {
		//	print(collider.name);
		colorStatus = true;
	}

	void OnTriggerExit(Collider collider) {
		//	print(collider.name);
		colorStatus = false;
	}
}