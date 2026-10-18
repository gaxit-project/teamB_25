using System.Collections;
using UnityEngine;

/// <summary>
/// エリアのライト制御。ブレーカー復旧時にライトをちらつかせて点灯させ、最後に一定の明るさで固定する。
/// 呼び出し元: Breaker
/// </summary>
public class AreaLightController : MonoBehaviour
{
    public float intensityMultiplier = 1.5f; // 点灯後のライトの明るさ
    [SerializeField] private float blinkingSpeed = 1.0f; // 点滅スピード
    [SerializeField] private float blinkingTime = 3.0f; // 点滅時間(秒)

    public void IncreaseLight()
    {
        StartCoroutine(BlinkingLight());
    }

    private IEnumerator BlinkingLight()
    {
        Light[] lights = GetComponentsInChildren<Light>();
        float blinkingEndTime = Time.time + blinkingTime;

        // PerlinNoise で明るさを滑らかに変化させ、電気が不安定に復旧する様子を表現する
        while (Time.time < blinkingEndTime)
        {
            foreach (Light light in lights)
            {
                light.intensity = Mathf.PerlinNoise(Time.time * blinkingSpeed, 0) * intensityMultiplier;
            }
            yield return null;
        }

        // 点滅終了後、明るさを固定
        foreach (Light light in lights)
        {
            light.intensity = intensityMultiplier;
        }
    }
}