using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions; // Regexを使うために必要

public class Breaker : MonoBehaviour
{
    private bool isCollision = false;
    public bool isActivated = false;
    public Electrical electricalScript;
    public GameObject[] electricalObjects; // ここで設定されたオブジェクトが消える

    public void bootBreaker()
    {
        AudioManager.Instance.PlaySE("BreakerOn", transform.position);
        isActivated = true;

        // ブレーカー名からエリア番号を取得
        string breakerName = gameObject.name;
        string number = Regex.Replace(breakerName, "[^0-9]", ""); // System.Text.RegularExpressions.Regex を using で追加済み
        int num = int.Parse(number);
        Image breakerIconImage;


        electricalScript.StopElectrical(num); //電気エフェクト消す処理
        foreach (GameObject electricalObject in electricalObjects)
        {
            if (electricalObject != null)
            {
                electricalObject.SetActive(false);
            }
        }

        switch (number)
        {
            case "1":
                breakerIconImage = GameObject.Find("breakerIcon" + number).GetComponent<Image>();
                breakerIconImage.color = new Color(1.0f, 1.0f, 0f);
                break;

            case "2":
                breakerIconImage = GameObject.Find("breakerIcon" + number).GetComponent<Image>();
                breakerIconImage.color = new Color(0f, 1.0f, 0f);
                break;

            case "3":
                breakerIconImage = GameObject.Find("breakerIcon" + number).GetComponent<Image>();
                breakerIconImage.color = new Color(1.0f, 0f, 0.8f);
                break;

            case "4":
                breakerIconImage = GameObject.Find("breakerIcon" + number).GetComponent<Image>();
                breakerIconImage.color = new Color(0f, 1.0f, 0.9f);
                break;
        }

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