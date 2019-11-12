using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HunterAnimationSounds : MonoBehaviour
{

    #region [Public Variables]
    
    public AudioClip jumpSound;
    public AudioClip wallClingSound;
    public AudioClip wallSlipSound;
    
    #endregion

    #region [Private Variables]
    
    AudioSource jumpAudioSource;
    AudioSource wallClingAudioSource;
    AudioSource wallSlipAudioSource;
    
    #endregion

    #region [Public Methods]
    
    public void PlayJumpSound() {
        jumpAudioSource.Play();
    }
    
    public void PlayWallClingSound() {
        wallClingAudioSource.Play();
    }
    
    public void PlayWallSlipSound() {
        wallSlipAudioSource.Play();
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        jumpAudioSource = AudioManager.AddAudioSource(gameObject, jumpSound, .5f, true, AudioManager.Instance.sfxGroup);
        wallClingAudioSource = AudioManager.AddAudioSource(gameObject, wallClingSound, 1, true, AudioManager.Instance.sfxGroup);
        wallSlipAudioSource = AudioManager.AddAudioSource(gameObject, wallSlipSound, 1, true, AudioManager.Instance.sfxGroup);
    }
    
    #endregion

}
