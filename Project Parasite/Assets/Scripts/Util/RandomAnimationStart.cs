using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimationStart : MonoBehaviour
{
    
    #region [MonoBehaviour Callbacks]
    
    void Start() {
        Animator animator = GetComponent<Animator>();
        // Skip to a random point in the animation
        animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, Random.Range(0f, 1f));
    }
    
    #endregion

}
