using System.Collections;
using UnityEngine;

/// <summary>
/// 脱出扉の開閉。扉のローカル方向へスライドさせて開く。
/// 呼び出し元: BreakerManager(全ブレーカー復旧時)
/// </summary>
public class DoorOpener : MonoBehaviour
{
    public Vector3 slideOffset = new Vector3(0f, 2f, 0f); // 扉のローカル方向への移動量（例：上にスライド）
    public float slideDuration = 1.5f;
    private bool isOpened = false;

    public void OpenDoor()
    {
        if (isOpened) return;

        isOpened = true;
        StartCoroutine(SlideDoor());
    }

    // 開扉と同時に演出カメラで Time.timeScale = 0 になるため、unscaledDeltaTime で進める
    private IEnumerator SlideDoor()
    {
        float time = 0f;
        Vector3 startPos = transform.position;
        Vector3 target = startPos + transform.TransformDirection(slideOffset);

        while (time < slideDuration)
        {
            transform.position = Vector3.Lerp(startPos, target, time / slideDuration);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.position = target;
    }
}