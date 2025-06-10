using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ClearScreen : MonoBehaviour
{
    public Image myImage;
    private float alpha;
    private void Start()
    {
        alpha = 1.0f;
    }
    private void Update()
    {
        if (alpha < 0.2f)
        {
            Destroy(myImage);
        }
        else
        {
            ChangeImage();
        }
        
    }

    public void ChangeImage()
    {
        alpha -= 0.001f;
        Color color = myImage.color;
        color.a = alpha;
        myImage.color = color;
        
    }
}
