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
