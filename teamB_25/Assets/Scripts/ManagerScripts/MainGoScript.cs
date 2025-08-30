using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGoScript : MonoBehaviour
{
    void Start()
    {
        Invoke("MainGo", 3f);
    }
    void MainGo()
    {
        SceneManager.LoadScene("Title");
    }
}
