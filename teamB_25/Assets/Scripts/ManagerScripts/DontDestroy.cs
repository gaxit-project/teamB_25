using UnityEngine;

/// <summary>
/// シーンをまたいで残すオブジェクト用。2つ目以降が生成された場合は破棄して、常に1つだけ存在させる。
/// </summary>
public class DontDestroy : MonoBehaviour
{
    public static DontDestroy Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}