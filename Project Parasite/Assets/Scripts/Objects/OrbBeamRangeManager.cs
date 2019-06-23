using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbBeamRangeManager : RangeIndicator {

	#region [Public Variables]

	// The orb that was most recently placed
	public Orb MostRecentOrb {
		get { return _mostRecentOrb; }
		set {
			_mostRecentOrb = value;
			SetOriginTransform(value.transform);
		}
	}

	// Set to false externally when there are no orbs remaining to be placed
	public bool shouldShowMarkers = true;
	
	#endregion

    protected override int IndicatorCount => 6;

	#region [Private Variables]

	// The maximum distance from the most recent orb that another can be placed
	//	to connect them
	const float ORB_BEAM_RANGE = 9f;
	Orb _mostRecentOrb;
	
	#endregion

	#region [Public Methods]
	
	public bool isInRange(Vector2 ofPosition) {
		if (MostRecentOrb == null) {
			return false;
		}
		return Vector2.Distance(MostRecentOrb.transform.position, ofPosition) <= ORB_BEAM_RANGE;
	}
	
	#endregion

    protected override bool ShouldShowIndicators() {
        if (shouldShowMarkers && isInRange(Utility.GetMousePos())) {
			SetTargetPosition(Utility.GetMousePos());
			return true;
		}
		return false;
    }

}
