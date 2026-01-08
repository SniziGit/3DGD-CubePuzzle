using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoColorSwitcher1 : MonoBehaviour {
	private bool state = false;

	void Start() {
		StartCoroutine(ChangeColor());
	}

	IEnumerator ChangeColor() {
		while(true) {
			yield return new WaitForSeconds(1.5f); // Delay for 1.5 seconds
			if(state) {
				GreenColor();
				state = false;
			}
			else {
				OrangeColor();
				state = true;
			}
		}
	}

	void GreenColor() {
		GetComponent<Renderer>().material.color = new Color(0.847f, 1, 0, 1);
	}

	void OrangeColor() {
		GetComponent<Renderer>().material.color = new Color(1, 0.816f, 0, 1);
	}
}
