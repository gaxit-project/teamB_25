using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions; // Regexを使うために必要

public class Breaker : MonoBehaviour
{
    private bool isCollision = false;
    public bool isActivated = false;
    public string sceneToMuteSE = "Introduction";

    // Mainシーンのみ音を出す
    private void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName != sceneToMuteSE)
        {
            AudioManager.Instance.PlaySELoop("BrokenBreaker", transform);
        }
    }

    public void bootBreaker()
    {
        AudioManager.Instance.DestroySE("BrokenBreaker");
        AudioManager.Instance.PlaySE("BreakerOn", transform.position);
        isActivated = true;

        // ブレーカー名からエリア番号を取得
        string breakerName = gameObject.name;
        string number = Regex.Replace(breakerName, "[^0-9]", ""); // System.Text.RegularExpressions.Regex を using で追加済み

        // 対応するライト親オブジェクトを探す
        GameObject lightParent = GameObject.Find("light" + number);
        if (lightParent != null)
        {
            AreaLightController alc = lightParent.GetComponent<AreaLightController>();
            if (alc != null)
            {
                alc.IncreaseLight(); // 明るくする
            }
        }

        // アウトラインの色を変更
        Outline outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.OutlineColor = Color.green;
        }

        AudioManager.Instance.PlaySE("ReBreaker", transform.position);

        BreakerManager.Instance.ActivateBreaker();
    }
}