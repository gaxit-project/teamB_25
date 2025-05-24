using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakerManager : MonoBehaviour
{
    public static bool BreakerOn { get; private set; } = false;
    private GameObject[] exitObjects;

    // Start is called before the first frame update
    void Start()
    {
        exitObjects = GameObject.FindGameObjectsWithTag("Exit");
        
        foreach (GameObject obj in exitObjects)
        {
            obj .SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision other)
    {
        // playerが電力復旧出来たら
        if (other.gameObject.CompareTag("Player"))
        {
            gameObject.GetComponent<Renderer>().material.color = Color.blue;
            BreakerOn = true;
            foreach (GameObject obj in exitObjects)
            {
                obj.SetActive(true);
            }
        }
    }
}
