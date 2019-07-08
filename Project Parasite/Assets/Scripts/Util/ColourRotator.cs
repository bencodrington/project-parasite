using System.Collections.Generic;
using UnityEngine;

public class ColourRotator
{
    #region [Private Variables]
    Color currentColour;
	
	Dictionary<Color, Color> nextColour = new Dictionary<Color, Color>();
    
    #endregion

    #region [Public Methods]
    
    public ColourRotator(Color[] colours) {
		currentColour = colours[0];
		for (int i = 0; i < colours.Length - 1; i++) {
			nextColour.Add(colours[i], colours[i+1]);
		}
		nextColour.Add(colours[colours.Length - 1], colours [0]);
    }

    public Color GetNextColour() {
        nextColour.TryGetValue(currentColour, out currentColour);
        return currentColour;
    }
    
    #endregion

}
