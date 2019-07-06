using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    #region [Public Variables]
    
    public AudioClip hoverSound;
    public AudioClip clickSound;

    #endregion

    #region [Private Variables]
    
    AudioSource hoverAudioSource;
    AudioSource clickAudioSource;

    #endregion

    #region [Public Methods]

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverAudioSource.Play();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickAudioSource.Play();
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        hoverAudioSource = Utility.AddAudioSource(gameObject, hoverSound, 0.25f);
        clickAudioSource = Utility.AddAudioSource(gameObject, clickSound, 0.75f);
    }
    
    #endregion

}
