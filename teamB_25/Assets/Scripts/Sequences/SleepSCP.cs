using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ゲームオーバー演出。カメラが沈んで横倒しになり、暗転したらGameOverシーンへ遷移する。
/// </summary>
public class SleepSCP : MonoBehaviour
{
    public Transform Camera;
    public Image fadeImage;

    // 暗転にかかる秒数。フレームレートに依存しないよう時間基準にしている
    [SerializeField] private float fadeDuration = 33f;

    private void Start()
    {
        if (Camera == null || fadeImage == null)
        {
            return;
        }

        StartCoroutine(SleepSequence());
    }

    // ① カメラがゆっくり沈む → ② 1.5秒の間 → ③ 横倒しになりながら暗転開始
    private IEnumerator SleepSequence()
    {
        while (Camera.position.y > 1.4f)
        {
            Camera.position += new Vector3(0, -1f, 1f) * Time.deltaTime;
            Camera.rotation = Quaternion.Lerp(Camera.rotation, Quaternion.Euler(10, 0, 0), Time.deltaTime * 2f);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);
        StartCoroutine(FadeToBlack());

        while (Camera.position.y > 0.3f)
        {
            Camera.position += new Vector3(0, -0.5f, 0.8f) * Time.deltaTime;
            Camera.rotation = Quaternion.Lerp(Camera.rotation, Quaternion.Euler(0, 90, 90), Time.deltaTime * 1f);
            yield return null;
        }
    }

    // 暗転が完了したら GameOver シーンへ遷移する
    private IEnumerator FadeToBlack()
    {
        Color color = fadeImage.color;
        float alpha = 0f;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime / fadeDuration;
            color.a = Mathf.Min(alpha, 1f);
            fadeImage.color = color;
            yield return null;
        }

        SceneManager.LoadScene("GameOver");
    }
}