using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移の窓口。遷移前にループSEを止める。"End" が指定された場合はゲームを終了する。
/// DontDestroyOnLoad は使わず、シーンごとに配置する。
/// </summary>
public class SceneChangeManager : MonoBehaviour
{
    public static SceneChangeManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 重複インスタンスを破棄
        }
    }

    public void ChangeScene(string _sceneName)
    {
        // ループSEの管理情報は DontDestroyOnLoad の AudioManager に残るため、遷移前に整理する
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

    // ゲームを終了する(エディタ上では再生を停止する)
    public void EndScene()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}