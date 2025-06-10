using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaLightController : MonoBehaviour
{
    public float intensityMultiplier = 1.5f;

   public void IncreaseLight()
    {
        foreach (Light light in GetComponentsInChildren<Light>())
        {
            light.intensity *= intensityMultiplier;
        }
    }
}
