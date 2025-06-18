using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class Dead : MonoBehaviour
{
    [System.Serializable]
    public class Slash
    {
        public RawImage image;
        public Vector2 position; 
        public float rotationZ;  
    }

    public Slash[] slashes; 
    public float delayBetweenSlashes = 0.2f;
    public float fadeDuration = 0.1f;

    private void Start()
    {
        StartCoroutine(PlaySlashes());
    }

    IEnumerator PlaySlashes()
    {
        foreach (var slash in slashes)
        {
            yield return StartCoroutine(FadeInOut(slash));
            yield return new WaitForSeconds(delayBetweenSlashes);
        }
        SceneManager.LoadScene("GameOver");
    }

    IEnumerator FadeInOut(Slash slash)
    {
        // 位置と回転を設定
        RectTransform rt = slash.image.rectTransform;
        rt.anchoredPosition = slash.position;
        rt.localRotation = Quaternion.Euler(0, 0, slash.rotationZ);

        // 表示＆フェード処理
        Color c = slash.image.color;
        c.a = 0f;
        slash.image.color = c;
        slash.image.gameObject.SetActive(true);

        // Fade In
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeDuration);
            slash.image.color = c;
            yield return null;
        }

        // Fade Out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1, 0, t / fadeDuration);
            slash.image.color = c;
            yield return null;
        }

        slash.image.gameObject.SetActive(false);
    }
}
