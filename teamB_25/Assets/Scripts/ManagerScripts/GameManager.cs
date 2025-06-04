using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] SceneChangeManager sceneChangeManager;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    
    void OnCollisionEnter(Collision other)
    {
        //恐竜と接触したらゲームオーバー
        if (other.gameObject.CompareTag("Enemy"))
        {
            sceneChangeManager.ChangeScene("GameOver");
        }

        // 出口にたどりつけたらゲームクリア
        if(other.gameObject.CompareTag("Exit"))
        {
            sceneChangeManager.ChangeScene("GameClear");
        }
    }
}
