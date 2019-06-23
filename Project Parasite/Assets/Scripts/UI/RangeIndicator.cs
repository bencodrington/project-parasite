using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RangeIndicator : MonoBehaviour
{
    #region [Public Variables]
    
    public GameObject indicatorPrefab;
    
    #endregion

	// Number of indicators to display
    protected abstract int IndicatorCount { get; }

    #region [Private Variables]
    // The transform from which the range is measured
    //  represents the start of the line of indicators
    Transform originTransform;
    // The end of the line of indicators
    Vector2 targetPosition;
	GameObject[] indicators;
    
    #endregion

    #region [Public Methods]
    
    public void Start() {
        // Spawn indicators
        indicators = new GameObject[IndicatorCount];
        for (int i = 0; i < IndicatorCount; i++) {
            indicators[i] = Instantiate(indicatorPrefab);
            indicators[i].transform.SetParent(transform);
        }
        HideIndicators();
    }

    public void SetOriginTransform(Transform _originTransform) {
        originTransform = _originTransform;
    }

    public void SetTargetPosition(Vector2 position) {
        targetPosition = position;
    }
    
    #endregion

    protected abstract bool ShouldShowIndicators();

    #region [MonoBehaviour Callbacks]
    
    void Update() {
        if (ShouldShowIndicators()) {
            ShowIndicators();
            PositionMarkers();
        } else {
            HideIndicators();
        }
    }
    
    #endregion

	void ShowIndicators() {
		// Return early if the first indicator is already active
		if (indicators == null || indicators[0].gameObject.activeInHierarchy) { return; }
		foreach(GameObject indicator in indicators) {
			indicator.gameObject.SetActive(true);
		}
	}

	void HideIndicators() {
		// Return early if the first marker is already inactive
		if (indicators == null || !indicators[0].gameObject.activeInHierarchy) { return; }
		foreach(GameObject indicator in indicators) {
			indicator.gameObject.SetActive(false);
		}
	}

	void PositionMarkers() {
		GameObject indicator;
		Vector2 start = originTransform.position;
		Vector2 end = targetPosition;
		for (int i = 0; i < IndicatorCount; i++) {
			indicator = indicators[i];
			// Include an imaginary indicator on either end of the calculations
			// 	to replace the first and last ones which will be hidden by the
			// 	origin and target, respectively
			// Otherwise it would be (float) i / (indicatorCount)
			float percentageToTargetPosition = (i + 1f) / (IndicatorCount + 1);
			indicator.transform.position = Vector2.Lerp(start, end, percentageToTargetPosition);
		}
	}
}
