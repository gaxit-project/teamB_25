using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    /// <summary>
    /// �����Ȃ��悤�ɂ���
    /// </summary>
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}