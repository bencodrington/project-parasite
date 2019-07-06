using UnityEngine;

public class AnimationEventHandler: MonoBehaviour
{
    #region [Public Variables]
    
    public AudioClip clip;
    
    #endregion

    #region [Private Variables]
    
    AudioSource source;
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        source = Utility.AddAudioSource(gameObject, clip);
    }
    
    #endregion

    #region [Private Methods]
    
    void PlaySound() {
        source.Play();
        // CLEANUP:
        FindObjectOfType<CameraFollow>().ShakeScreen(0.07f, 0.2f);
    }
    
    #endregion

}
