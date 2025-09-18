using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの基礎挙動（移動・隠れる・死亡判定）を管理するクラス
/// </summary>
public class PlayerBase : MonoBehaviour
{
    // -------------------------
    // 移動関連
    // -------------------------
    [Header("移動速度設定")]
    [SerializeField] private float walkSpeed = 3f;      // 通常歩行速度
    [SerializeField] private float runSpeed = 6f;       // ダッシュ速度
    [SerializeField] private float tiredSpeed = 1.5f;   // 疲労時の速度
    private float currentSpeed;                         // 実際に適用される速度

    [Header("スタミナ設定")]
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float stamina = 10f;
    [SerializeField] private float staminaRecoveryTime = 4f; // 回復にかかる秒数
    private bool lostStamina = false; // スタミナ切れフラグ

    [Header("カメラ")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera backCamera;
    [SerializeField] private FirstPersonCameraController firstPersonCamera;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI actionText; // Hide/Exitなど
    [SerializeField] private Image hideButton;
    [SerializeField] private Image breakerButton;
    [SerializeField] private Image staminaGauge; // 緑→赤
    [SerializeField] private Image hideTimerImage;
    [SerializeField] private RawImage deathEffectImage;

    [Header("隠れるシステム")]
    [SerializeField] private float defaultHideTime = 5f;
    private float currentHideTime;
    private Transform currentHidePlace = null;
    private Quaternion currentHideRotation;
    private Coroutine hideCoroutine;
    private bool isHiding = false; // 隠れているかどうか

    [Header("死亡演出")]
    [SerializeField] private float deathEffectDuration = 3.0f;
    private bool isDeadProcessing = false;

    // -------------------------
    // 内部管理用
    // -------------------------
    private Rigidbody rb;
    private GameInputs gameInputs;
    private Vector2 moveInput; // 入力値
    private Vector3 velocity = Vector3.zero;

    private bool isRunning = false;
    private bool isPushRun = false;
    private bool isReseting = false;

    // 隠れているかどうか
    public bool IsFounding { get; private set; }

    // 現在走っているかどうか
    public bool IsRunningNow { get; private set; }

    // 現在歩いているかどうか
    public bool IsWalkingNow { get; private set; }

    public static bool countdownActive = false; // ゲーム開始待ちフラグ

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gameInputs = new GameInputs();

        // --- 入力イベント登録 ---
        gameInputs.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        gameInputs.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        gameInputs.Player.Run.started += _ => isPushRun = true;
        gameInputs.Player.Run.canceled += _ => isPushRun = false;

        gameInputs.Player.Tool.started += _ => OnTool(); // 隠れるボタン
        gameInputs.Player.Back.started += _ => TurnAround(); // 反転

        gameInputs.Enable();

        // 初期化
        stamina = maxStamina;
        currentHideTime = defaultHideTime;

        if (deathEffectImage != null) deathEffectImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        gameInputs?.Dispose();
    }

    public virtual void Attack()
    {
        // デフォルトの攻撃処理（何もしないでもOK）
    }

    private void Update()
    {
        if (!countdownActive) return;

        // --- 移動音 ---
        if (isRunning) AudioManager.Instance.PlaySELoop(AudioDefine.PlayerRun, transform);
        else if (IsMoving()) AudioManager.Instance.PlaySELoop(AudioDefine.PlayerWalk, transform);
        else
        {
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun, transform);
        }

        // --- 隠れている場合は移動音停止 ---
        if (isHiding)
        {
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun, transform);
        }

        // --- UI更新 ---
        if (staminaGauge != null)
        {
            staminaGauge.fillAmount = stamina / maxStamina;
            staminaGauge.color = lostStamina ? Color.red : Color.green;
        }

        // --- 入力状態や移動速度から判定してフラグを更新 ---

        // 例: ロッカーに隠れたら true にする
        if (isHiding)
            IsFounding = true;
        else
            IsFounding = false;

        // 例: スタミナシステムと連動して走り状態を判定
        if (isRunning)
        {
            IsRunningNow = true;
            IsWalkingNow = false;
        }
        else
        {
            IsRunningNow = false;
            IsWalkingNow = true;
        }
    }


    private void FixedUpdate()
    {
        if (!countdownActive) return;

        ChangeSpeed(); // スタミナ・速度更新

        if (IsMoving())
        {
            // カメラの向きに合わせた移動
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
            rb.velocity = moveDir * currentSpeed;
        }
        else
        {
            // 停止時は慣性を減速
            Vector3 targetVel = new Vector3(0, rb.velocity.y, 0);
            rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVel, ref velocity, 0.1f);
        }
    }

    // ==================================================
    // 入力アクション
    // ==================================================

    private void TurnAround()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySE("Turn", transform.position);

        mainCamera.transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
    }

    private void OnTool()
    {
        if (isReseting) return;

        // 隠れ場所がある & 今隠れていないなら入る
        if (currentHidePlace != null && !isHiding)
        {
            EnterHide();
        }
        // 既に隠れている場合は解除
        else if (isHiding)
        {
            ExitHide();
            StartCoroutine(ResetTime());
        }
    }

    // ==================================================
    // 隠れる処理
    // ==================================================
    private void EnterHide()
    {
        isHiding = true;
        currentHideTime = defaultHideTime;

        // サウンド
        AudioManager.Instance.PlaySE("OpenLocker", transform.position);
        AudioManager.Instance.PlaySELoop("HeartBeat", transform);

        // 位置と向き修正
        Vector3 pos = currentHidePlace.position;
        transform.position = new Vector3(pos.x, transform.position.y, pos.z);
        transform.rotation = Quaternion.Euler(0, currentHideRotation.eulerAngles.y, 0);

        rb.velocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // UI
        if (actionText != null) actionText.text = "Exit";
        if (hideTimerImage != null)
        {
            hideTimerImage.gameObject.SetActive(true);
            hideTimerImage.fillAmount = 1f;
        }

        // タイマー開始（既存があれば止める）
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideCountdown());
    }

    private void ExitHide()
    {
        isHiding = false;

        // サウンド
        AudioManager.Instance.PlaySE("CloseLocker", transform.position);
        AudioManager.Instance.DestroySE("HeartBeat", transform);

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (actionText != null) actionText.text = "Hide";
        if (hideTimerImage != null) hideTimerImage.gameObject.SetActive(false);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    private IEnumerator HideCountdown()
    {
        while (currentHideTime > 0f)
        {
            currentHideTime -= Time.deltaTime;
            if (hideTimerImage != null)
                hideTimerImage.fillAmount = currentHideTime / defaultHideTime;
            yield return null;
        }
        // 時間切れで自動解除
        if (isHiding) ExitHide();
    }

    private IEnumerator ResetTime()
    {
        isReseting = true;
        yield return new WaitForSeconds(1.5f);
        isReseting = false;
    }

    // ==================================================
    // 死亡処理
    // ==================================================
    private void OnCollisionStay(Collision collision)
    {
        if (isDeadProcessing) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (isHiding)
            {
                StartCoroutine(PlayDeathEffect());
            }
            else
            {
                ChangeSceneImmediately();
            }
        }
    }

    private IEnumerator PlayDeathEffect()
    {
        isDeadProcessing = true;

        if (deathEffectImage != null)
            deathEffectImage.gameObject.SetActive(true);

        AudioManager.Instance?.PlaySE("Rouring", transform.position);

        yield return new WaitForSeconds(deathEffectDuration);

        ChangeSceneImmediately();
    }

    private void ChangeSceneImmediately()
    {
        isDeadProcessing = true;
        if (deathEffectImage != null)
            deathEffectImage.gameObject.SetActive(false);

        // 実際はSceneManager.LoadSceneでも可
        Debug.Log("シーン遷移：DeadScene");
    }

    // ==================================================
    // ユーティリティ
    // ==================================================
    private void ChangeSpeed()
    {
        if (!lostStamina && stamina > 0 && isPushRun && moveInput.y > 0)
        {
            currentSpeed = runSpeed;
            isRunning = true;
            stamina -= Time.deltaTime;

            if (stamina <= 0)
            {
                stamina = 0;
                lostStamina = true;
            }
        }
        else if (lostStamina && stamina < maxStamina)
        {
            currentSpeed = tiredSpeed;
            isRunning = false;
            stamina += (maxStamina / 6f) * Time.deltaTime;

            if (stamina >= maxStamina)
            {
                stamina = maxStamina;
                lostStamina = false;
            }
        }
        else
        {
            currentSpeed = walkSpeed;
            isRunning = false;
            stamina += (maxStamina / staminaRecoveryTime) * Time.deltaTime;
            if (stamina > maxStamina) stamina = maxStamina;
        }
    }

    private bool IsMoving() => moveInput.sqrMagnitude > 0.01f;
}
