using UnityEngine;

/// <summary>
/// どのシーンから開始しても PauseManager が1つ存在するようにする。
/// </summary>
public class PauseLoad : MonoBehaviour
{
    [SerializeField] GameObject pauseManagerPrefab;

    void Awake()
    {
        if (PauseManager.Instance != null) return;

        if (pauseManagerPrefab == null)
        {
            return;
        }

        Instantiate(pauseManagerPrefab);
    }
}