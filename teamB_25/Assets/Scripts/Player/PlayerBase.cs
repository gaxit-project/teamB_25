using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// プレイヤー本体。移動・ダッシュ(スタミナ)・ロッカーに隠れる・ブレーカー起動・敵との接触による死亡を担当する。
/// 入力は Input System(GameInputs)で受け取る。
/// 恐竜AI(Dinosaur_Base)は IsFounding / IsRunningNow / IsWalkingNow / LookedHiding を参照して、
/// プレイヤーの発見・警戒を判定する。
/// 連携: Dinosaur_Base, FirstPersonCameraController(隠れ解除時の向き設定), Breaker,
///       SceneChangeManager, AudioManager
/// </summary>
public class PlayerBase : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float tiredSpeed; // スタミナ切れ後、回復するまでの速度
    [SerializeField] private float currentSpeed;
    public float stopTime = 0f; // 入力をやめてから止まるまでの時間(SmoothDamp)
    [SerializeField] private Camera mainCamera;
    [SerializeField] SceneChangeManager sceneChangeManager;
    [SerializeField] private float stamina = 10f;
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private TextMeshProUGUI text; // 操作ガイド(Hide / Boot / Exit)
    [SerializeField] private Image hideButton;
    [SerializeField] private Image breakerButton;
    [SerializeField] private Image image; // スタミナゲージ
    [SerializeField] private Image hideTimeImage; // 隠れていられる残り時間のゲージ
    [SerializeField] private Image hideview; // 隠れている間の画面
    [SerializeField] private float defaultHideTime = 5f;
    [SerializeField] private FirstPersonCameraController firstPersonCamera;

    private const float NormalRecoverTime = 4f; // スタミナが満タンになるまでの秒数(通常時)
    private const float TiredRecoverTime = 6f;  // 同(スタミナ切れ後)

    private float maxHideTime = 10f;
    private float currentHideTime = 10f;

    private Rigidbody rb;
    private GameInputs gameInputs;
    private Vector2 moveInputValue;
    private Vector3 velocity = Vector3.zero; // SmoothDamp 用
    private Vector3 preHidePosition;
    private Transform currentHidePlace = null;
    private Quaternion currentHidePlaceRotation;
    private Collider currentHideCollider; // 隠れる場所のCollider
    private Breaker breaker; // 今いる範囲のブレーカー(範囲外では null)
    private Coroutine hideCoroutine;
    private float resetTime = 1.5f;

    public RawImage deathEffectImage;   // Inspectorに割り当て(恐竜の動画)
    public float effectDuration = 3.0f; // 死亡演出の再生時間
    private bool isDeadProcessing = false; // 死亡処理が二重に走らないように制御

    private Dinosaur_Base dinosaur_Base;

    // 隠れている間 true。恐竜AIが参照する
    private bool isFounding = false;
    public bool IsFounding => isFounding;

    // 恐竜AIが、プレイヤーの移動状態(走る / 歩く)を判定するために参照する
    public bool IsRunningNow => IsRunning();
    public bool IsWalkingNow => IsMoving() && !IsRunning();

    private bool isRunning = false;
    private bool isPushRun = false;
    private bool lostStamina = false; // スタミナ切れ。満タンになるまで走れない
    private bool isTurning = false;   // 後ろを向いている
    private bool isReseting = false;  // 隠れ解除直後の操作待ち

    // 隠れるところを恐竜に見られていたか。恐竜AIが参照する
    private bool lookedHiding = false;
    public bool LookedHiding => lookedHiding;

    public int Hp = 0;

    public static bool countdownActive = false; // StartTimerのカウントダウン終了で true になる

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        image.color = Color.green;

        gameInputs = new GameInputs();

        gameInputs.Player.Move.started += OnMove;
        gameInputs.Player.Move.performed += OnMove;
        gameInputs.Player.Move.canceled += OnMove;

        gameInputs.Player.Run.started += ctx => isPushRun = true;
        gameInputs.Player.Run.canceled += ctx => isPushRun = false;

        // Tool ボタンは「隠れる / 出る」(started)と「ブレーカー起動」(performed)の両方に使う
        gameInputs.Player.Tool.started += OnToolStarted;
        gameInputs.Player.Tool.performed += OnTool;

        gameInputs.Player.Back.performed += OnBack;

        gameInputs.Enable();
    }

    private void OnDestroy()
    {
        gameInputs?.Dispose();
    }

    void Start()
    {
        GameObject enemy = GameObject.FindWithTag("Enemy");
        if (enemy != null)
        {
            dinosaur_Base = enemy.GetComponent<Dinosaur_Base>();
        }

        countdownActive = false;

        if (deathEffectImage != null)
        {
            deathEffectImage.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!countdownActive || AudioManager.Instance == null) return;

        UpdateFootstepSE();
    }

    // 移動状態に応じて足音(歩き / 走り)を切り替える。隠れている間は鳴らさない
    private void UpdateFootstepSE()
    {
        if (isFounding)
        {
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun, transform);
            return;
        }

        if (IsRunning())
        {
            AudioManager.Instance.PlaySELoop(AudioDefine.PlayerRun, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk, transform);
        }
        else if (IsMoving())
        {
            AudioManager.Instance.PlaySELoop(AudioDefine.PlayerWalk, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun, transform);
        }
        else
        {
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun, transform);
        }
    }

    private void FixedUpdate()
    {
        if (!countdownActive) return;

        ChangeSpeed();
        image.fillAmount = stamina / maxStamina;
        image.color = lostStamina ? Color.red : Color.green;

        if (moveInputValue.sqrMagnitude > 0.01f)
        {
            // カメラの向きを基準に、水平方向へ移動する
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = camForward * moveInputValue.y + camRight * moveInputValue.x;
            rb.velocity = moveDirection * currentSpeed;
        }
        else
        {
            // 入力がないときは水平方向だけ減速させる(落下速度は維持)
            Vector3 targetVelocity = new Vector3(0, rb.velocity.y, 0);
            rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref velocity, stopTime);
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInputValue = context.ReadValue<Vector2>();
    }

    // 後ろを向く(カメラを180度回転)。もう一度押すと元に戻る
    private void OnBack(InputAction.CallbackContext context)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("Turn", transform.position);
        }
        mainCamera.transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
        isTurning = !isTurning;
    }

    // Tool ボタン(started): 隠れ場所の前なら隠れる、隠れ中なら出る
    private void OnToolStarted(InputAction.CallbackContext context)
    {
        if (currentHidePlace == null || isReseting) return;

        if (!isFounding)
        {
            // 後ろを向いたまま隠れると向きが反転したままになるため、先に正面へ戻す
            if (isTurning)
            {
                mainCamera.transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
                isTurning = false;
            }

            EnterHide();
            maxHideTime = defaultHideTime;
            currentHideTime = defaultHideTime;
            hideTimeImage.fillAmount = 1f;

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            hideCoroutine = StartCoroutine(HideCountdown());

            // 隠れた時点で、恐竜に見られていたかを記録する
            lookedHiding = Dinosaur_Base.dinosLookingAtPlayer.Count > 0;
        }
        else
        {
            lookedHiding = false;

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
            CancelHide();
            StartCoroutine(ResetTime());
        }
    }

    // Tool ボタン(performed): 範囲内のブレーカーを起動する
    private void OnTool(InputAction.CallbackContext callback)
    {
        if (breaker == null) return;

        if (!breaker.isActivated)
        {
            breakerButton.gameObject.SetActive(false);
            text.gameObject.SetActive(false);
            breaker.bootBreaker();
        }
    }

    // 敵との接触判定。隠れていて、かつ見られていない場合だけセーフ
    private void OnCollisionStay(Collision collision)
    {
        if (isDeadProcessing) return;
        if (!collision.gameObject.CompareTag("Enemy")) return;
        if (isFounding && !lookedHiding) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        if (isFounding)
        {
            // ロッカー中に見つかって接触: 演出付きで遷移
            StartCoroutine(PlayDeathEffectAndChangeScene());
        }
        else
        {
            // 隠れていない状態で接触: 即遷移
            ChangeSceneImmediately();
        }
    }

    private IEnumerator PlayDeathEffectAndChangeScene()
    {
        isDeadProcessing = true;

        if (deathEffectImage != null)
        {
            deathEffectImage.gameObject.SetActive(true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.DestroySE("HeartBeat", transform);
            AudioManager.Instance.DestroySE("Dash", transform);
            AudioManager.Instance.DestroySE("Walk", transform);
            AudioManager.Instance.PlaySE("Rouring", transform.position);
        }

        yield return new WaitForSeconds(effectDuration);

        ChangeSceneImmediately();
    }

    private void ChangeSceneImmediately()
    {
        isDeadProcessing = true;

        if (deathEffectImage != null)
        {
            deathEffectImage.gameObject.SetActive(false); // シーン遷移前に非表示へ戻す
        }

        sceneChangeManager.ChangeScene("DeadScene");
    }

    // 隠れ場所 / ブレーカーの範囲に入っている間、対象を記録して操作ガイドを出す。
    // トリガーは子オブジェクトで、対象の情報は親オブジェクトに付いている前提
    private void OnTriggerStay(Collider other)
    {
        if (isFounding) return;

        Transform parent = other.transform.parent;
        if (parent == null) return;

        if (other.CompareTag("HidePlace"))
        {
            Collider parentCollider = parent.GetComponent<Collider>();
            if (parentCollider == null)
            {
                return;
            }

            currentHidePlace = parentCollider.transform;
            currentHidePlaceRotation = parentCollider.transform.rotation;
            currentHideCollider = parentCollider;

            if (text != null)
            {
                if (isReseting) return;

                text.gameObject.SetActive(true);
                hideButton.gameObject.SetActive(true);
                text.text = "Hide";
            }
        }
        else if (other.CompareTag("Breaker"))
        {
            breaker = parent.GetComponent<Breaker>();

            if (breaker != null && !breaker.isActivated && breakerButton != null)
            {
                breakerButton.gameObject.SetActive(true);
                text.gameObject.SetActive(true);
                text.text = "Boot";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HidePlace"))
        {
            if (!isFounding)
            {
                currentHidePlace = null; // 隠れ場所の情報をリセット

                if (text != null)
                {
                    if (isReseting) return;

                    hideButton.gameObject.SetActive(false);
                    text.gameObject.SetActive(false);
                }
            }
        }

        if (other.CompareTag("Breaker"))
        {
            if (!isFounding && breaker != null && !breaker.isActivated && text != null)
            {
                text.gameObject.SetActive(false);
                breakerButton.gameObject.SetActive(false);
            }

            // 範囲外から Tool を押しても起動しないよう、参照を外す
            breaker = null;
        }
    }

    public bool IsMoving()
    {
        return moveInputValue.sqrMagnitude > 0.01f;
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    // スタミナの消費と回復、それに応じた速度の切り替え。
    // 走る: 消費 → 0 になったら lostStamina(満タンになるまで走れず、遅い速度で回復)
    // 歩く / 停止: 通常の速度で回復
    private void ChangeSpeed()
    {
        if (!lostStamina && (stamina > 0) && isPushRun && (moveInputValue.y > 0))
        {
            currentSpeed = runSpeed;
            isRunning = true;
            stamina -= Time.deltaTime;
            if (stamina < 0)
            {
                stamina = 0;
                lostStamina = true;
            }
        }
        else if (lostStamina && stamina < maxStamina)
        {
            currentSpeed = tiredSpeed;
            isRunning = false;

            stamina += (maxStamina / TiredRecoverTime) * Time.deltaTime;
            if (stamina > maxStamina)
            {
                stamina = maxStamina;
                lostStamina = false;
            }
        }
        else
        {
            currentSpeed = moveSpeed;
            isRunning = false;

            if (stamina < maxStamina)
            {
                stamina += (maxStamina / NormalRecoverTime) * Time.deltaTime;
                if (stamina > maxStamina)
                {
                    stamina = maxStamina;
                }
            }
        }
    }

    private void EnterHide()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("OpenLocker", transform.position);
            AudioManager.Instance.PlaySELoop("HeartBeat", transform);
        }

        isFounding = true;
        preHidePosition = transform.position;
        Vector3 targetPos = currentHidePlace.position;
        transform.position = new Vector3(targetPos.x, preHidePosition.y, targetPos.z);
        transform.rotation = Quaternion.Euler(0, currentHidePlaceRotation.eulerAngles.y, 0);

        hideview.gameObject.SetActive(true);

        rb.velocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll; // 隠れている間は動かさない

        if (text != null)
        {
            text.text = "Exit";
        }

        hideTimeImage.gameObject.SetActive(true);

        // 見られながら隠れた場合は、ロッカーのColliderを無効にして接触判定を通す
        if (dinosaur_Base != null && currentHideCollider != null
            && currentHideCollider.CompareTag("HidePlace") && dinosaur_Base.IsLooked)
        {
            currentHideCollider.enabled = false;
        }
    }

    private void CancelHide()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("CloseLocker", transform.position);
            AudioManager.Instance.DestroySE("HeartBeat", transform);
        }
        isFounding = false;

        if (currentHideCollider != null)
        {
            currentHideCollider.enabled = true; // 隠れる場所のColliderを復活
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation; // 回転だけ固定
        hideview.gameObject.SetActive(false);

        if (currentHidePlace != null)
        {
            // ロッカーの前(ロッカーの向きに対して前方1m)に出て、その向きに視点を合わせる
            Vector3 exitDirection = currentHidePlace.forward;
            Vector3 exitPos = currentHidePlace.position + exitDirection * 1.0f;
            transform.position = new Vector3(exitPos.x, preHidePosition.y, exitPos.z);

            float yAngle = currentHidePlace.rotation.eulerAngles.y;
            firstPersonCamera.SetRotation(yAngle, 0f);
        }

        currentHidePlace = null;
        currentHideCollider = null;
        hideTimeImage.gameObject.SetActive(false);

        if (text != null)
        {
            text.text = "Hide";
        }
    }

    // 隠れていられる時間を減らし、0 になったら強制的に隠れ解除する
    private IEnumerator HideCountdown()
    {
        while (currentHideTime > 0f)
        {
            currentHideTime -= Time.deltaTime;
            hideTimeImage.fillAmount = currentHideTime / maxHideTime;
            yield return null;
        }

        if (isFounding)
        {
            CancelHide();
        }
    }

    // 隠れ解除直後の連続操作を防ぐ待機時間
    private IEnumerator ResetTime()
    {
        text.gameObject.SetActive(false);
        hideButton.gameObject.SetActive(false);
        isReseting = true;
        while (resetTime > 0f)
        {
            resetTime -= Time.deltaTime;
            yield return null;
        }

        isReseting = false;
        resetTime = 1.5f;

        if (currentHidePlace != null)
        {
            text.gameObject.SetActive(true);
            hideButton.gameObject.SetActive(true);
        }
    }
}