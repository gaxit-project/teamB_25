using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.SceneManagement;

public class IntroductionCamera : MonoBehaviour
{
    [Tooltip("切り替え順に並べたカメラをここに登録してください")]
    public Camera[] cameras;

    public float switchInterval = 5f;

    private int currentIndex = 0;
    private float timer = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if(cameras == null || cameras.Length == 0)
        {
            //Debug.LogError("camersd 配列が空です！エディターで登録してください。");
            enabled = false; // このスクリプトを無効化
            return;
        }

        // 最初のカメラだけONにする
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = (i == 0);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("joystick button 7"))
        {
            SceneChangeManager.Instance.ChangeScene("Main");
        }


        timer += Time.deltaTime;
        if(timer >= switchInterval)
        {
            // 現在のカメラをOFF
            cameras[currentIndex].enabled = false;

            // 次のカメラに切り替え(ループ)
            currentIndex = (currentIndex + 1) % cameras.Length;
            cameras[currentIndex].enabled = true;

            timer = 0f;

            // 一周終わったらシーン移動
            if (currentIndex == 0)
            {
                SceneChangeManager.Instance.ChangeScene("Main");
            }
        }
    }
}
