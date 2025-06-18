using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SleepSCP : MonoBehaviour
{
    public Transform Camera;
    public Image fadeImage;

    private void Start()
    {
        StartCoroutine(SleepSequence());
    }

    private IEnumerator SleepSequence()
    {
        while (Camera.position.y > 1.4f)
        {
            Camera.position += new Vector3(0, -0.005f, 0.005f);
            Camera.rotation = Quaternion.Lerp(Camera.rotation, Quaternion.Euler(10, 0, 0), Time.deltaTime * 2f);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);
        StartCoroutine(FadeToBlack());
        while (Camera.position.y > 0.3f)
        {
            Camera.position += new Vector3(0, -0.001f, 0.002f);
            Camera.rotation = Quaternion.Lerp(Camera.rotation, Quaternion.Euler(0, 90, 90), Time.deltaTime * 1f);
            yield return null;
        }
    }

    private IEnumerator FadeToBlack()
    {
        Color color = fadeImage.color;
        float alpha = 0f;

        while (alpha <= 1f)
        {
            alpha += 0.0005f;
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }
        SceneManager.LoadScene("GameOver");
    }
}
