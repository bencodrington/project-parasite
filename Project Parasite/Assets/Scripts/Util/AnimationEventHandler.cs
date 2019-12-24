using UnityEngine;

public class AnimationEventHandler: MonoBehaviour
{
    #region [Public Variables]
    
    public AudioClip clip;
    public bool rolloff;
    
    #endregion

    #region [Private Variables]
    
    AudioSource source;
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        // CLEANUP: specific to burst indicator drops
        source = AudioManager.AddAudioSource(gameObject, clip, 1, rolloff, AudioManager.Instance.heartbeatGroup);
    }
    
    #endregion

    #region [Private Methods]
    
    void PlaySound() {
        source.Play();
        // CLEANUP:
        FindObjectOfType<CameraFollow>().ShakeScreen(0.07f, 0.2f, transform.position);
    }
    
    #endregion

}
