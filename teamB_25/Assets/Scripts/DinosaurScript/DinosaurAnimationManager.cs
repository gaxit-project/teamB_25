using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinosaurAnimationManager : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayWalk()
    {
        animator.Play("Walk");
    }

    public void PlayRun()
    {
        animator.Play("Run");
    }

    public void PlayRoar()
    {
        animator.Play("Roar");
    }

    public void PlayLeap()
    {
        // Yé≤180ìxâÒì]Åiåªç›ÇÃX,ZÇÕï€éùÅj
        Vector3 currentEulerAngles = transform.eulerAngles;
        transform.eulerAngles = new Vector3(currentEulerAngles.x, 180f, currentEulerAngles.z);

        animator.Play("Leap");
    }

    public void PlaySniff()
    {
        animator.Play("Sniff");
    }

    public void PlayIdle()
    {
        animator.Play("Idle");
    }
}
