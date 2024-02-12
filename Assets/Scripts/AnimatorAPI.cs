using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorAPI : MonoBehaviour
{
    private Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void updateSpeed(float speed)
    {
        animator.SetFloat("actualSpeed",speed);
    }

    void Fire()
    {
        animator.SetBool("Fire", true);
    }
    
    void Jump()
    {
        animator.SetBool("Jump", true);
    }
        
}
