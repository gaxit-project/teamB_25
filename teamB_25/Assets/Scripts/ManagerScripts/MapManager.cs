using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UIコンポーネント操作に必要

public class MapManager : MonoBehaviour
{
    public Camera miniMapCamera;
    public Transform map;
    public Transform player;
    public RenderTexture miniMapTexture; // 作成済みのRenderTexture
    public RawImage miniMapUI;           // UIに表示するRawImage
    public RawImage fogImage;

    private Transform RedPosition;

    private bool mapLoad;
    private Bounds mapBounds;

    private Texture2D fogTexture;
    private Color32[] fogPixels;
    private int fogResolution = 128;
    void Start()
    {
        mapLoad = false;
        // マーカー作成
        MarkerCreate();
        InitFog();
    }

    void MarkerCreate()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.localScale = new Vector3(3f, 1f, 3f);
        marker.GetComponent<Renderer>().material.color = Color.red;
        RedPosition = marker.transform;
    }
    void InitFog()
    {
        fogTexture = new Texture2D(fogResolution, fogResolution, TextureFormat.ARGB32, false);
        fogPixels = new Color32[fogResolution * fogResolution];

        for (int i = 0; i < fogPixels.Length; i++)
            fogPixels[i] = new Color32(0, 0, 0, 255); // 全面黒で不透明

        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();

        fogImage.texture = fogTexture;
        fogImage.rectTransform.sizeDelta = new Vector2(100, 100); // ミニマップと同じ
    }
    // 子オブジェクトも含めてマップの範囲を取得
    Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("マップにRendererがありません！");
            return new Bounds(root.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    void Update()
    {
        miniPosition();
        MapCamera();
        RevealFog(player.position);
    }
    void MapCamera()
    {
        if (!mapLoad)
        {
            // マップ全体の範囲を取得
            mapBounds = CalculateBounds(map);
            mapLoad = true;
        }
        // カメラの位置と向きをマップの中心に合わせる
        Vector3 center = mapBounds.center;

        //プレイヤーの位置
        //Vector3 center = new Vector3(player.position.x, player.position.y, player.position.z);

        miniMapCamera.transform.position = new Vector3(center.x, 100f, center.z);
        miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 真上から見下ろす

        // カメラの表示範囲を調整
        float size = Mathf.Max(mapBounds.size.x, mapBounds.size.z) * 0.5f;
        miniMapCamera.orthographic = true;
        miniMapCamera.orthographicSize = size;

        // RenderTextureをカメラに設定
        miniMapCamera.targetTexture = miniMapTexture;

        // RawImageにRenderTextureを設定
        miniMapUI.texture = miniMapTexture;
        miniMapUI.rectTransform.sizeDelta = new Vector2(100, 100); // 正方形にする
    }
    // プレイヤーの位置にマーカーを追従させる
    void miniPosition()
    {
        Vector3 offset = new Vector3(0f, 20f, 0f); // 上に浮かせて表示
        RedPosition.position = player.position + offset;
    }

    void RevealFog(Vector3 worldPos)
    {
        float percentX = Mathf.InverseLerp(mapBounds.min.x, mapBounds.max.x, worldPos.x);
        float percentY = Mathf.InverseLerp(mapBounds.min.z, mapBounds.max.z, worldPos.z);

        int centerX = Mathf.RoundToInt(percentX * fogResolution);
        int centerY = Mathf.RoundToInt(percentY * fogResolution);

        int radius = 5;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int px = centerX + x;
                int py = centerY + y;

                if (px >= 0 && px < fogResolution && py >= 0 && py < fogResolution)
                {
                    int index = py * fogResolution + px;
                    fogPixels[index] = new Color32(0, 0, 0, 0); // 透明
                }
            }
        }

        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();
    }
}
