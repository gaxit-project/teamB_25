using UnityEngine;

/// <summary>
/// 出口への到達を検知して GameClear シーンへ遷移させる。
/// 連携: SceneChangeManager
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] SceneChangeManager sceneChangeManager;

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Exit"))
        {
            sceneChangeManager.ChangeScene("GameClear");
        }
    }
}