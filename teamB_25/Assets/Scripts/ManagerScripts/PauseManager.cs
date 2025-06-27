using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    public Canvas PauseCanvas;
    [SerializeField] Button focusButton;
    [SerializeField] CanvasGroup PauseCG;
    [SerializeField] CanvasGroup otherCG;

    public bool CountDown;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public void SetOtherCG(CanvasGroup cg)
    {
        otherCG = cg;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PauseCanvas.enabled = false;
    }
    public void Start()
    {
        
        if (otherCG != null)
        {
            otherCG.interactable = true;
        }
        if (SceneManager.GetActiveScene().name == "Title")
        {

        }
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
            if (!CountDown)
            {
                PauseNow();
            }
            
        }
        else
        {
            PauseCG.interactable = false;
            AudioManager.Instance.ResumeAudio();
            PauseCanvas.enabled = false;
            if (otherCG != null)
            {
                otherCG.interactable = true;
            }
            
        }
    }

    private void PauseNow()
    {
        AudioManager.Instance.StopAllSELoops();
        AudioManager.Instance.PauseAudio();
        PauseCanvas.enabled = true;
        PauseCG.interactable = true;
        if (otherCG != null)
        {
            otherCG.interactable = false;
        }
        focusButton.Select();
    }
}
