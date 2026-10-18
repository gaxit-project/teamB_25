using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 漏電エフェクトの音と停止処理。起動10秒後にループSEを鳴らし、
/// ブレーカー復旧時に対応するパーティクルとSEを止める。
/// 連携: Breaker(StopElectrical を呼ぶ), AudioManager
/// </summary>
public class Electrical : MonoBehaviour
{
    public string sceneToMuteSE = "Introduction"; // このシーンでは音を鳴らさない

    private void Start()
    {
        Invoke(nameof(PlaySE), 10.0f);
    }

    private void PlaySE()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != sceneToMuteSE && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySELoop("Electric", transform);
        }
    }

    // エリア番号 num に対応する "electricN" という名前のパーティクルを探して停止する
    public void StopElectrical(int num)
    {
        string particleSystemName = "electric" + num;
        GameObject particleObject = GameObject.Find(particleSystemName);

        if (particleObject != null)
        {
            ParticleSystem particle = particleObject.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                particle.gameObject.SetActive(false);

                // ループSEは (SE名, 対象Transform) で管理されるため、再生時と同じ対象で止める
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.DestroySE("Electric", particleObject.transform);
                }
            }
        }
    }
}