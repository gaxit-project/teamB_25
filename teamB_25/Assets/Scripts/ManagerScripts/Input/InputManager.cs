using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] PauseManager pauseManager;
    ManagerIS input;

    private void Awake()
    {
        input = new ManagerIS();
    }
    private void OnDisable()
    {
        input.Disable();
    }
    private void OnEnable()
    {
        input.Enable();
    }
    private void Update()
    {
        if (input.Manager.Pouse.triggered) pauseManager.Pause();
    }
}
