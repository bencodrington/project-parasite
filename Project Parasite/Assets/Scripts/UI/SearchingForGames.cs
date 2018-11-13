using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchingForGames : MonoBehaviour {

	public string textString;
	public Text text;
	public float secondsBetween;
	public int maxPeriods;
	int periodCount;
	
	void Start() {
		periodCount = 0;
		StartCoroutine(AnimatePeriods());
	}

	IEnumerator AnimatePeriods() {
		string periods;
		while (true) {
			periods = "";
			periodCount++;
			if (periodCount > maxPeriods) {
				periodCount = 0;
			}
			for (int i = 0; i < periodCount; i++) {
				periods = periods + ".";
			}
			text.text = textString + periods;
			yield return new WaitForSeconds(secondsBetween);
		}
	}
}
