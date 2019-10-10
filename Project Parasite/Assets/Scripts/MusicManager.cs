using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    #region [Public Variables]
    
    public AudioClip introSong;
    public AudioClip menuSong;
    
    #endregion

    #region [Private Variables]
    
    AudioSource audioSource;
    
    #endregion
    
    #region [MonoBehaviour Callbacks]
    
    void Start() {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = menuSong;
        audioSource.loop = true;
        audioSource.volume = 0.1f;
        audioSource.Play();
    }
    
    #endregion
}
