using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] SceneChangeManager sceneChangeManager;
    private PlayerBase playerBase;
    private Dinosaur_Base dinosaur_Base;

    // Start is called before the first frame update
    void Start()
    {
        playerBase = GameObject.FindWithTag("Player").GetComponent<PlayerBase>();
        dinosaur_Base = GameObject.FindWithTag("Enemy").GetComponent<Dinosaur_Base>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    
    void OnCollisionEnter(Collision other)
    {
        // 出口にたどりつけたらゲームクリア
        if (other.gameObject.CompareTag("Exit"))
        {
            sceneChangeManager.ChangeScene("GameClear");
        }
    }
}
