using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Electrical : MonoBehaviour
{
    public string sceneToMuteSE = "Introduction";

    //Mainシーンのみ音を出す
    private void Start()
    {
        Invoke("PlaySE", 10.0f); // 10秒後に鳴らす
    }

    private void PlaySE()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != sceneToMuteSE)
        {
            AudioManager.Instance.PlaySELoop("Electric", transform);
        }
    }

    // メソッドの引数としてnumを受け取るように変更
    public void StopElectrical(int num)
    {
        // numを使用して名前でパーティクルシステムを探す
        // 例: "Breaker_1" に対応するパーティクルシステムが "ElectricalEffect1" の場合
        string particleSystemName = "electric" + num;
        GameObject particleObject = GameObject.Find(particleSystemName);

        if (particleObject != null)
        {
            ParticleSystem particle = particleObject.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                particle.gameObject.SetActive(false);

                // 必要であれば音を停止
                AudioManager.Instance.DestroySE("Electric", particleObject.transform);
            }
        }
        else
        {
            Debug.LogWarning("パーティクルシステムが見つかりません: " + particleSystemName);
        }
    }
}