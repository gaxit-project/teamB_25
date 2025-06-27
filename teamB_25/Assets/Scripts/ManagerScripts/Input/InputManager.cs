using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        if (SceneManager.GetActiveScene().name == "Main") 
        {
            if (input.Manager.Pause.triggered) pauseManager.Pause();
        }
        
    }
}
