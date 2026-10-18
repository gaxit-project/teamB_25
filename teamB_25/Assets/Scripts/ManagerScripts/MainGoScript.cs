using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 3•bŒã‚É Title ƒV[ƒ“‚Ö©“®‘JˆÚ‚·‚éB
/// </summary>
public class MainGoScript : MonoBehaviour
{
    void Start()
    {
        Invoke(nameof(LoadTitle), 3f);
    }

    void LoadTitle()
    {
        SceneManager.LoadScene("Title");
    }
}