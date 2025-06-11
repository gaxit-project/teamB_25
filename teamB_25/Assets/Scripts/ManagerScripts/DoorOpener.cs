using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    public Vector3 slideOffset = new Vector3(0f, 2f, 0f); // 移動量（例：上にスライド）
    public float slideDuration = 1.5f;
    private Vector3 originalPos;
    private bool isOpened = false;

    // Start is called before the first frame update
    void Start()
    {
        originalPos = transform.position;
    }

    public void OpenDoor()
    {
        if (!isOpened)
        {
            isOpened = true;
            StartCoroutine(SlideDoor());
        }
    }

    private IEnumerator SlideDoor()
    {
        float time = 0f;
        originalPos = transform.position;

        Vector3 localOffset = transform.TransformDirection(slideOffset);
        Vector3 target = originalPos + localOffset;

        while (time < slideDuration)
        {
            transform.position = Vector3.Lerp(originalPos, target, time / slideDuration);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.position = target;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
