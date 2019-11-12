using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour {
    
    #region [Public Variables]
    
    public AudioMixerGroup master;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ungrouped;
    public AudioMixerGroup sidechainGroup;
    public AudioMixerGroup heartbeatGroup;
    public AudioMixerGroup uiGroup;
    public static AudioManager Instance {get; private set;}
    
    #endregion

    #region [Public Methods]
    
	public static AudioSource AddAudioSource(GameObject gameObject, AudioClip clip = null, float volume = 1f, bool rollOff = false, AudioMixerGroup group = null) {
		AudioSource newSource;
		newSource = gameObject.AddComponent<AudioSource>();
		newSource.clip = clip;
		newSource.volume = volume;
		newSource.playOnAwake = false;
		if (rollOff) {
			newSource.rolloffMode = AudioRolloffMode.Linear;
			newSource.minDistance = 3;
			newSource.maxDistance = 15;
			newSource.spatialBlend = 1; // Necessary for volume rolloff to take effect
		}
		newSource.outputAudioMixerGroup = group == null ? Instance.ungrouped : group;
		return newSource;
	}
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        if (Instance == null) {
            Instance = MatchManager.Instance.GetComponent<AudioManager>();
        } else {
            Debug.LogError("AudioManager.Start(): Shouldn't be instantiating a second instance of a singleton class.");
        }
    }
    
    #endregion
}
