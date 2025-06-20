using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public Canvas PauseCanvas;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PauseCanvas.enabled = false;
    }

    public void ChangeScene(string _sceneName)
    {
        Time.timeScale = 1.0f; // 時間を戻す
        PauseCanvas.gameObject.SetActive(false); // UIを消す
        AudioManager.Instance.ResumeAudio(); // Audio再開
        if (_sceneName == "End")
        {
            Application.Quit();
        }
        SceneChangeManager.Instance.ChangeScene(_sceneName);
    }
    public void Pause()
    {
        Time.timeScale = (Time.timeScale == 0f) ? 1f : 0f;
        if (Time.timeScale == 0f)
        {
            PauseNow();
        }
        else
        {
            AudioManager.Instance.ResumeAudio();
            PauseCanvas.enabled = false;
        }
    }

    private void PauseNow()
    {
        AudioManager.Instance.StopAllSELoops();
        AudioManager.Instance.PauseAudio();
        PauseCanvas.enabled = true;

    }
}
