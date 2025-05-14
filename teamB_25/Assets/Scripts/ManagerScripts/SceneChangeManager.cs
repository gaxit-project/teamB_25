using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    [SerializeField]private string _sceneName; //移動先のScene名
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

    public void ChangeScene() //Sceneを変える
    {
        SceneManager.LoadScene(_sceneName);
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
