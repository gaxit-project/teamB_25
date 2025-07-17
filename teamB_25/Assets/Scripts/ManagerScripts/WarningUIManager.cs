using System.Collections;
using UnityEngine;

public class WarningUIManager : MonoBehaviour
{
    [SerializeField] private GameObject warningUI;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        if (warningUI != null)
        {
            canvasGroup = warningUI.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                Debug.LogError("CanvasGroup が見つかりません。warningUI に CanvasGroup をアタッチしてください。");
                return;
            }

            canvasGroup.alpha = 0f;
            warningUI.SetActive(false);
        }
    }

    public void ShowWarning()
    {
        FadeIn(3f);
    }

    public void HideWarning()
    {
        FadeOut(0.5f);
    }

    public void FadeIn(float duration)
    {
        if (warningUI == null || canvasGroup == null) return;

        warningUI.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1f, duration));
    }

    public void FadeOut(float duration)
    {
        if (warningUI == null || canvasGroup == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutAndDisable(duration));
    }

    private IEnumerator FadeOutAndDisable(float duration)
    {
        yield return FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0f, duration);
        warningUI.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        cg.alpha = endAlpha;
    }
}
