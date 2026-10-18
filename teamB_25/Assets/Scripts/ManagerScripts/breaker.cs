using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ブレーカー1台分の復旧処理。
/// オブジェクト名の数字をエリア番号として使い、対応する UI アイコン("breakerIconN")と
/// ライト("lightN")をシーン内から探して更新する。
/// 連携: Electrical, AreaLightController, BreakerManager, Outline
/// </summary>
public class Breaker : MonoBehaviour
{
    public bool isActivated = false;
    public Electrical electricalScript;
    public GameObject[] electricalObjects; // 復旧時に非表示にするオブジェクト

    // エリア1〜4のアイコン色(復旧後)
    private static readonly Color[] iconColors =
    {
        new Color(1.0f, 1.0f, 0f),
        new Color(0f, 1.0f, 0f),
        new Color(1.0f, 0f, 0.8f),
        new Color(0f, 1.0f, 0.9f),
    };

    public void bootBreaker()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("BreakerOn", transform.position);
        }
        isActivated = true;

        // ブレーカー名の数字からエリア番号を取得
        string number = Regex.Replace(gameObject.name, "[^0-9]", "");
        if (!int.TryParse(number, out int num))
        {
            return;
        }

        electricalScript.StopElectrical(num);
        foreach (GameObject electricalObject in electricalObjects)
        {
            if (electricalObject != null)
            {
                electricalObject.SetActive(false);
            }
        }

        // 対応するエリアのアイコンを色付けする
        if (num >= 1 && num <= iconColors.Length)
        {
            GameObject icon = GameObject.Find("breakerIcon" + number);
            if (icon != null && icon.TryGetComponent(out Image breakerIconImage))
            {
                breakerIconImage.color = iconColors[num - 1];
            }
        }

        // 対応するライト親オブジェクトを探して明るくする
        GameObject lightParent = GameObject.Find("light" + number);
        if (lightParent != null)
        {
            AreaLightController alc = lightParent.GetComponent<AreaLightController>();
            if (alc != null)
            {
                alc.IncreaseLight();
            }
        }

        Outline outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.OutlineColor = Color.green;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("ReBreaker", transform.position);
        }

        BreakerManager.Instance.ActivateBreaker();
    }
}