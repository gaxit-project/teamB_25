using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class Breaker : MonoBehaviour
{
    
    private bool isCollision = false;
    public bool isActivated = false;
    

    public void bootBreaker()
    {
        isActivated = true;

        // ブレーカー名からエリア番号を取得
        string breakerName = gameObject.name;
        string number = System.Text.RegularExpressions.Regex.Replace(breakerName, "[^0-9]", "");

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

        //GetComponent<Renderer>().material.color = Color.green; // 色を変える
        BreakerManager.Instance.ActivateBreaker();
    }
}
