using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


public class Breaker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private bool isCollision = false;
    public bool isActivated = false;
    private GameInputs input;

    private void Awake()
    {
        input = new GameInputs();
    }

    private bool toolTriggered = false;

    private void Update()
    {
        toolTriggered = input.Player.Tool.triggered;
    }


    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }


    private void OnDestroy()
    {
        input?.Dispose();
    }

    void OnCollisionStay(Collision other)
    {
        //Debug.Log("CollisionStay with: " + other.gameObject.name);
        // playerが電力復旧出来たら
        if (!isActivated && other.gameObject.CompareTag("Player"))
        {
            Debug.Log("CollisionStay with: " + other.gameObject.name);
            if (text != null)
            {
                text.gameObject.SetActive(true);
                text.text = "x";
            }
            if (input.Player.Tool.triggered && toolTriggered)
            {
                isActivated = true;
                text.gameObject.SetActive(false);

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
    }

    public void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (text != null) text.gameObject.SetActive(false);
        }
    }
}
