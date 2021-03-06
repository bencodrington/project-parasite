﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSoundSet : MonoBehaviour
{
    #region [Public Variables]
    
    public AudioClip[] sounds;
    [Range(0, 1)]
    public float volume;
    public bool rolloff;
    
    #endregion

    #region [Private Variables]
    
    AudioSource audioSource;
    
    #endregion

    #region [Public Methods]
    
    public void PlayRandom() {
        audioSource.clip = sounds[Random.Range(0, sounds.Length - 1)];
        audioSource.Play();
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        audioSource = AudioManager.AddAudioSource(gameObject, null, volume, rolloff, AudioManager.Instance.sfxGroup);
    }
    
    #endregion
}
