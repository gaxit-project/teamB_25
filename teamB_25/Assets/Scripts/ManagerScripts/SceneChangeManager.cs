using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    public static SceneChangeManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //Sceneをまたいで保持
        }
        else
        {
            Destroy(gameObject); //重複インスタンスを破棄
        }
    }

    public void ChangeScene(string _sceneName) //Sceneを変える
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSELoops();
        }

        if (_sceneName == "End")
        {
            EndScene();
            return;
        }


        SceneManager.LoadScene(_sceneName);
    }

    private IEnumerator DelayedSceneLoad(string sceneName)
    {
        yield return null; // 次のフレームまで待つ
        SceneManager.LoadScene(sceneName);
    }

    public void EndScene() //ゲームを終了
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
