using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseLoad : MonoBehaviour
{
    [SerializeField] GameObject pauseManagerPrefab;
    [SerializeField] CanvasGroup otherCG;
    void Awake()
    {
        if (PauseManager.Instance == null)
        {
            GameObject pMP = Instantiate(pauseManagerPrefab);
            PauseManager manager = pMP.GetComponent<PauseManager>();
           // manager.SetOtherCG(otherCG);
        }
    }
}
