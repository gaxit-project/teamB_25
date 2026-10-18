using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ミニマップと探索済みフォグの管理。
/// 真上から見下ろすカメラの映像(RenderTexture)をミニマップとして表示し、その上に黒いフォグ用の
/// テクスチャを重ねる。プレイヤーが通った周囲のピクセルを透明にして「探索済み」を表現する。
/// 連携: BreakerManager(脱出扉の開放時に GoalMarkerCreate を呼ぶ)
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    public Camera miniMapCamera;
    public Transform map;
    public Transform player;
    public RenderTexture miniMapTexture; // 作成済みのRenderTexture
    public RawImage miniMapUI;           // UIに表示するRawImage
    public RawImage fogImage;

    private Transform playerMarker;
    private bool mapLoad;
    private Bounds mapBounds;

    // フォグ: マップ全体を fogResolution x fogResolution のピクセルに対応させる(x = ワールドX, y = ワールドZ)
    private Texture2D fogTexture;
    private Color32[] fogPixels;
    private int fogResolution = 128;
    private const int FogRevealRadius = 7; // プレイヤー周囲で透明にする半径(ピクセル)
    private Vector2Int lastRevealCell = new Vector2Int(-1, -1);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        mapLoad = false;
        MarkerCreate();
        InitFog();
    }

    void Update()
    {
        if (!mapLoad) SetupMapCamera();
        UpdatePlayerMarker();
        RevealFog(player.position);
    }

    // ゴール位置にマーカーを置き、その周辺のフォグを晴らして場所を示す
    public void GoalMarkerCreate(Vector3 goalPosition)
    {
        GameObject goalMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goalMarker.transform.localScale = new Vector3(10f, 1f, 10f);
        goalMarker.transform.position = goalPosition;

        Renderer markerRenderer = goalMarker.GetComponent<Renderer>();
        markerRenderer.material.color = Color.white;
        RevealArea(markerRenderer.bounds);
    }

    // ワールド座標の範囲を、フォグテクスチャのピクセル範囲に変換して透明にする
    public void RevealArea(Bounds areaBounds)
    {
        int minX = Mathf.RoundToInt(Mathf.InverseLerp(mapBounds.min.x, mapBounds.max.x, areaBounds.min.x) * fogResolution);
        int maxX = Mathf.RoundToInt(Mathf.InverseLerp(mapBounds.min.x, mapBounds.max.x, areaBounds.max.x) * fogResolution);
        int minY = Mathf.RoundToInt(Mathf.InverseLerp(mapBounds.min.z, mapBounds.max.z, areaBounds.min.z) * fogResolution);
        int maxY = Mathf.RoundToInt(Mathf.InverseLerp(mapBounds.min.z, mapBounds.max.z, areaBounds.max.z) * fogResolution);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                if (x >= 0 && x < fogResolution && y >= 0 && y < fogResolution)
                {
                    fogPixels[y * fogResolution + x] = new Color32(0, 0, 0, 0);
                }
            }
        }

        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();
    }

    // プレイヤー位置を示すミニマップ用マーカー(赤い立方体)
    void MarkerCreate()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.localScale = new Vector3(5f, 1f, 5f);
        marker.GetComponent<Renderer>().material.color = Color.red;
        playerMarker = marker.transform;
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
        fogImage.rectTransform.sizeDelta = new Vector2(100, 100); // ミニマップと同じ大きさ
    }

    // 子オブジェクトも含めてマップの範囲を取得
    Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("MapManager: map has no Renderer.");
            return new Bounds(root.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    // マップ全体の範囲を一度だけ取得し、その中心の真上から見下ろすようカメラを設定する
    void SetupMapCamera()
    {
        mapBounds = CalculateBounds(map);
        mapLoad = true;

        Vector3 center = mapBounds.center;
        miniMapCamera.transform.position = new Vector3(center.x, 100f, center.z);
        miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        miniMapCamera.orthographic = true;
        miniMapCamera.orthographicSize = Mathf.Max(mapBounds.size.x, mapBounds.size.z) * 0.5f;

        miniMapCamera.targetTexture = miniMapTexture;
        miniMapUI.texture = miniMapTexture;
        miniMapUI.rectTransform.sizeDelta = new Vector2(100, 100); // 正方形にする
    }

    // プレイヤーの位置にマーカーを追従させる
    void UpdatePlayerMarker()
    {
        Vector3 offset = new Vector3(0f, 20f, 0f); // 上に浮かせて表示
        playerMarker.position = player.position + offset;
    }

    // プレイヤー周囲のフォグを晴らす。同じセルにいる間はテクスチャを更新しない
    void RevealFog(Vector3 worldPos)
    {
        float percentX = Mathf.InverseLerp(mapBounds.min.x, mapBounds.max.x, worldPos.x);
        float percentY = Mathf.InverseLerp(mapBounds.min.z, mapBounds.max.z, worldPos.z);

        int centerX = Mathf.RoundToInt(percentX * fogResolution);
        int centerY = Mathf.RoundToInt(percentY * fogResolution);

        if (centerX == lastRevealCell.x && centerY == lastRevealCell.y) return;
        lastRevealCell = new Vector2Int(centerX, centerY);

        for (int y = -FogRevealRadius; y <= FogRevealRadius; y++)
        {
            for (int x = -FogRevealRadius; x <= FogRevealRadius; x++)
            {
                int px = centerX + x;
                int py = centerY + y;

                if (px >= 0 && px < fogResolution && py >= 0 && py < fogResolution)
                {
                    fogPixels[py * fogResolution + px] = new Color32(0, 0, 0, 0);
                }
            }
        }

        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();
    }
}