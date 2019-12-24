using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurstIndicator : MonoBehaviour
{
    #region [Public Variables]
    
    public AudioClip burstChargeSound;
    public AudioClip burstReadySound;
    
    #endregion

    #region [Private Variables]

    Color START_COLOUR  = new Color(.15f, 0f, 0f, .85f);
    Color END_COLOUR    = new Color(.75f, 0f, 0f, 1f);

    SpriteRenderer spriteRenderer;
    SpriteMask spriteMask;
    Animator animator;
    AudioSource burstChargeAudioSource;
    AudioSource burstReadyAudioSource;

    float timeToFill = 0.25f;
    float timeElapsed = 0f;
    bool isFilling = false;
    bool isFull = false;
    
    #endregion

    #region [Public Methods]

    public void SetTimeToFill(float newTimeToFill) {
        timeToFill = newTimeToFill;
    }

    
    public void StartFilling() {
        timeElapsed = 0f;
        isFilling = true;
        burstChargeAudioSource.Play();
    }

    public void StopFilling() {
        isFilling = false;
        float progress = Mathf.Min(timeElapsed / timeToFill, 1);
        FindObjectOfType<CameraFollow>().ShakeScreen(Mathf.Lerp(0, .25f, progress), 0.2f, transform.position);
        burstChargeAudioSource.Stop();
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteMask = GetComponentInChildren<SpriteMask>();
        animator = GetComponentInChildren<Animator>();
        // Hide at start
        spriteRenderer.color = Color.clear;
        burstChargeAudioSource = AudioManager.AddAudioSource(gameObject, burstChargeSound);
        burstReadyAudioSource = AudioManager.AddAudioSource(gameObject, burstReadySound);
    }

    void Update() {
        if (isFilling) {
            timeElapsed += Time.deltaTime;
            float progress = Mathf.Min(timeElapsed / timeToFill, 1);
            spriteRenderer.color = Color.Lerp(START_COLOUR, END_COLOUR, progress);
            spriteMask.alphaCutoff = 1 - progress;
            if (timeElapsed >= timeToFill) {
                animator.SetBool("isActive", true);
                if (!isFull) {
                    OnReadyToBurst();
                }
                isFull = true;
            } else {
                FindObjectOfType<CameraFollow>().ShakeScreen(Mathf.Lerp(0, 0.03f, progress), 0.1f, transform.position);
            }
        } else {
            isFull = false;
            spriteRenderer.color = Color.clear;
            spriteMask.alphaCutoff = 0;
                animator.SetBool("isActive", false);
        }
    }
    
    #endregion

    #region [Private Methods]
    
    void OnReadyToBurst() {
        burstReadyAudioSource.Play();
    }
    
    #endregion

}
