using System.Collections;
using UnityEngine;

public class AreaLightController : MonoBehaviour
{
    [SerializeField] public float intensityMultiplier = 1.5f; //ライトの明るさ調整
    [SerializeField] private float blinkingSpeed = 1.0f; //点滅スピード
    [SerializeField] private float blinkingTime = 3.0f; //点滅時間


    public void IncreaseLight()
    {
        StartCoroutine(BlinkingLight());
    }

    private IEnumerator BlinkingLight()
    {
        float blinkingEndTime = Time.time + blinkingTime;

        // 3秒間、点滅を繰り返す
        while (Time.time < blinkingEndTime)
        {
            foreach (Light light in GetComponentsInChildren<Light>())
            {
                light.intensity = Mathf.PerlinNoise(Time.time * blinkingSpeed, 0) * intensityMultiplier;
            }
            yield return null; // 1フレーム待機
        }

        // 点滅終了後、明るさを固定
        foreach (Light light in GetComponentsInChildren<Light>())
        {
            light.intensity = intensityMultiplier;
        }
    }
}