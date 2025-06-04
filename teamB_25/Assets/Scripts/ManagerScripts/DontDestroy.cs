using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    /// <summary>
    /// è¡Ç¶Ç»Ç¢ÇÊÇ§Ç…Ç∑ÇÈ
    /// </summary>
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}