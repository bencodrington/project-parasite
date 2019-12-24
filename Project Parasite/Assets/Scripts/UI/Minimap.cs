using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{

    #region [Public Variables]

    public Image minimapImage;
    // The dot that represents the player's location
    public RectTransform dotTransform;
    // The coordinates of the main level, in world units
    public Vector2 worldBottomLeftCorner;
    public Vector2 worldTopRightCorner;
    
    #endregion

    #region [Private Variables]
    
    Transform target;
    Vector2 worldSize;
    
    #endregion

    #region [Public Methods]
    
    public void SetTarget(Transform target) {
        this.target = target;
    }

    public void RemoveTarget() {
        target = null;
        HideMap();
    }

    public void Activate() {
        ShowMap();
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Start() {
        if (worldTopRightCorner.x <= worldBottomLeftCorner.x || worldTopRightCorner.y <= worldBottomLeftCorner.y) {
            Debug.Log("Minimap:Start(): " + worldTopRightCorner + " isn't greater than " + worldBottomLeftCorner); 
        }
        // Store the size of the main ship map in world units
        worldSize = worldTopRightCorner - worldBottomLeftCorner;
        HideMap();
    }
    
    void Update() {
        if (target != null && minimapImage.IsActive()) {
            UpdateDot();
        }
    }
    
    #endregion

    #region [Private Methods]
    
    void UpdateDot() {
        // Set dot's position on map based on the player's position in the level
        Vector2 distFromBottomLeft = (Vector2)target.position - worldBottomLeftCorner;
        Vector2 normalizedPosition = distFromBottomLeft / worldSize;
        dotTransform.anchorMin = normalizedPosition;
        dotTransform.anchorMax = normalizedPosition;
    }

    void HideMap() {
        minimapImage.gameObject.SetActive(false);
        dotTransform.gameObject.SetActive(false);
    }

    void ShowMap() {
        minimapImage.gameObject.SetActive(true);
        dotTransform.gameObject.SetActive(true);
    }
    
    #endregion

}
