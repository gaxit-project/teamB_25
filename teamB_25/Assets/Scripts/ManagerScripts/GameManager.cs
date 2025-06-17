using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] SceneChangeManager sceneChangeManager;
    private PlayerBase playerBase;

    // Start is called before the first frame update
    void Start()
    {
        playerBase = GameObject.FindWithTag("Player").GetComponent<PlayerBase>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            // PlayerBaseが存在し、かつ見つかっているときだけゲームオーバー
            if (playerBase != null && !playerBase.IsFounding)
            {
                sceneChangeManager.ChangeScene("GameOver");
            }
            else
            {
                Debug.Log("見つかっていないのでゲームオーバーにならない");
            }
        }

        // 出口にたどりつけたらゲームクリア
        if (other.gameObject.CompareTag("Exit"))
        {
            sceneChangeManager.ChangeScene("GameClear");
        }
    }
}
