using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerBase : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float tiredSpeed;
    [SerializeField] private float currentSpeed;
    [SerializeField] public float stopTime = 0f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerBase player; // PlayerBase  X N   v g Q  
    [SerializeField] SceneChangeManager sceneChangeManager;
    [SerializeField] private float stamina = 10f;
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float staminaDuration;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Image hideButton;
    [SerializeField] private Image breakerButton;
    [SerializeField] private Image image;
    [SerializeField] private Image hideview;
    [SerializeField] private int hideTime = 10;
    [SerializeField] private Transform eyePosition;
    [SerializeField] private float rayLength = 3f;

    private Rigidbody rigidbody;
    private GameInputs gameInputs;
    private Vector2 moveInputValue;
    private Vector3 velocity = Vector3.zero;
    private float stepTimer;
    private Vector3 preHidePosition;
    private Transform currentHidePlace = null;
    private Quaternion currentHidePlaceRotation;
    private Collider currentHideCollider; // 隠れる場所のCollider
    private bool toolTriggered = false;
    private Breaker breaker;


    private Dinosaur_Base dinosaur_Base;
    private NormalDinosaur normalDinosaur;

    private bool isFounding = false;
    public bool IsFounding => isFounding;
    // PlayerBase.cs に追加
    public bool IsRunningNow => IsRunning(); // 外部から呼べるようにする
    public bool IsWalkingNow => IsMoving() && !IsRunning();

    private bool isRunning = false;
    private bool isPushRun = false;
    private bool isPushHide = false;
    private bool lostStamina = false;
    private bool isChangingCamera = false;

    public int Hp = 0;

    public static bool countdownActive = false; // StartTimerを待つ

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        image.color = Color.green;

        gameInputs = new GameInputs();

        if (isFounding == false)
        {
            gameInputs.Player.Move.started += OnMove;
            gameInputs.Player.Move.performed += OnMove;
            gameInputs.Player.Move.canceled += OnMove;
        }


        gameInputs.Player.Run.started += ctx => {
            isPushRun = true;
            Debug.Log("Run started");
        };
        gameInputs.Player.Run.canceled += ctx => {
            isPushRun = false;
            Debug.Log("Run canceled");
        };

        gameInputs.Player.Hide.started += ctx => {
            if (!isFounding && currentHidePlace != null)
            {
                EnterHide();

                StartCoroutine(HideCountdown());
            }
            else if (isFounding)
            {
                StopCoroutine(HideCountdown()); // プレイヤーが自分で出たら中断
                CancelHide();
            }
        };


        gameInputs.Enable();
    }

    private void OnDestroy()
    {
        gameInputs?.Dispose();
    }

    public void Start()
    {
        dinosaur_Base = GameObject.FindWithTag("Enemy").GetComponent<Dinosaur_Base>();
        normalDinosaur = GameObject.FindWithTag("Enemy").GetComponent<NormalDinosaur>();
        countdownActive = false;
        Attack();
    }

    private void Update()
    {
        if (!countdownActive) return;



        if (IsRunning())
        {
            AudioManager.Instance.PlaySELoop(AudioDefine.PlayerRun, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk);
        }
        else if (IsMoving())
        {
            AudioManager.Instance.PlaySELoop(AudioDefine.PlayerWalk, transform);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun);
        }
        else
        {
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun);
        }

        if (isFounding)
        {
            AudioManager.Instance.DestroySE(AudioDefine.PlayerWalk);
            AudioManager.Instance.DestroySE(AudioDefine.PlayerRun);
            return;
        }

        toolTriggered = gameInputs.Player.Tool.triggered;

        
    }

    public virtual void Attack()
    {
        Debug.Log("test");
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInputValue = context.ReadValue<Vector2>();
    }

    public void OnTriggerStay(Collider other)
    {
        if (isFounding) return; // 既に隠れてるなら処理しない

        Collider parentCollider = other.transform.parent?.GetComponent<Collider>();//判定に使ったコライダーの親のコライダーを取得
        if (other.gameObject.CompareTag("HidePlace"))
        {
            currentHidePlace = parentCollider.transform;
            currentHidePlaceRotation = parentCollider.transform.rotation;
            Debug.Log("Enter HidePlace"); // ← これで呼ばれているか確認
            currentHideCollider = parentCollider;//隠れるコライダーを設定したコライダーにする
            if (text != null)
            {
                text.gameObject.SetActive(true);
                hideButton.gameObject.SetActive(true);
                text.text = "Hide";
            }

        }

        breaker = parentCollider.GetComponent<Breaker>();//親のオブジェクトのBreakerスクリプトを取得

        if (other.gameObject.CompareTag("Breaker") && !breaker.isActivated)
        {
            Debug.Log("Hit Breaker");
            if (breakerButton != null)
            {
                breakerButton.gameObject.SetActive(true);
                text.gameObject.SetActive(true);
                text.text = "Boot";
            }
            if (gameInputs.Player.Tool.triggered && toolTriggered)
            {
                breakerButton.gameObject.SetActive(false);
                text.gameObject.SetActive(false);
                breaker.bootBreaker();
                
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("HidePlace"))
        {
            if (!isFounding)
            {
                currentHidePlace = null;//隠れ場所の情報をリセット
                Debug.Log("Exit HidePlace");

                if (text != null)
                {
                    hideButton.gameObject.SetActive(false);
                    text.gameObject.SetActive(false);
                }

            }
        }

        if (other.gameObject.CompareTag("Breaker") && !breaker.isActivated)
        {
            if (!isFounding)
            {

                if (text != null)
                {
                    text.gameObject.SetActive(false);
                    breakerButton.gameObject.SetActive(false);
                }

            }
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
                Debug.Log("スタミナが空です。走れません");
            }
        }
        else if (lostStamina && stamina < maxStamina)
        {
            staminaDuration = 6f;
            currentSpeed = tiredSpeed;
            isRunning = false;

            if (stamina < maxStamina)
            {
                stamina += (maxStamina / staminaDuration) * Time.deltaTime;

                // 上限で止める
                if (stamina > maxStamina)
                {
                    stamina = maxStamina;
                    lostStamina = false;
                    Debug.Log("スタミナが満タンです。いつでも走れます");
                }
            }
        }
        else
        {
            staminaDuration = 4f;
            currentSpeed = moveSpeed;
            isRunning = false;

            if (stamina < maxStamina)
            {
                stamina += (maxStamina / staminaDuration) * Time.deltaTime;

                // 上限で止める
                if (stamina > maxStamina)
                {
                    stamina = maxStamina;
                    Debug.Log("スタミナが満タンになりました");
                }
            }
        }
    }

    private void EnterHide()
    {
        AudioManager.Instance.PlaySE("OpenLocker", transform.position);
        isFounding = true;//隠れているflag
        isChangingCamera = true;
        preHidePosition = transform.position;
        Vector3 targetPos = currentHidePlace.position;
        transform.position = new Vector3(targetPos.x, preHidePosition.y, targetPos.z);
        transform.rotation = Quaternion.Euler(0, currentHidePlaceRotation.eulerAngles.y, 0);

        AudioManager.Instance.PlaySE("SE2", transform.position);
        hideview.gameObject.SetActive(true);

        rigidbody.velocity = Vector3.zero;

        // 物理演算を生かしたまま動かないように
        rigidbody.isKinematic = false;  // kinematic解除
        rigidbody.constraints = RigidbodyConstraints.FreezeAll; // 動きを凍結

        // ColliderをTriggerにして衝突検知をOnTriggerEnterで行う
        ///Collider col = GetComponent<Collider>();
        ///if (col != null)
        ///{
        /// NormalDinosaur dino = GameObject.FindWithTag("Enemy").GetComponent<NormalDinosaur>();
        ///  if (dino != null && dino.IsLooked)
        ///{
        /// col.isTrigger = false; // ⬅ 見つかっているなら衝突判定あり
        /// }
        /// else
        /// {
        ///  col.isTrigger = true; // ⬅ 見つかっていないならすり抜けOK
        /// }
        ///}

        if (text != null) text.text = "Exit";
        Debug.Log("Hiding");

        if (currentHideCollider != null && currentHideCollider.CompareTag("HidePlace"))
        {
            currentHideCollider.enabled = false;
        }
    }

    private void CancelHide()
    {
        AudioManager.Instance.PlaySE("CloseLocker", transform.position);
        isFounding = false;
        isChangingCamera = false;

        if (currentHideCollider != null)
        {
            currentHideCollider.enabled = true; // 隠れる場所のCollider復活
        }

        // ColliderのisTriggerを戻す
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        rigidbody.isKinematic = false;
        rigidbody.constraints = RigidbodyConstraints.None;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation; // 回転だけ固定
        hideview.gameObject.SetActive(false);

        transform.position = preHidePosition;//もとの位置に戻す
        currentHidePlace = null;
        currentHideCollider = null;

        Debug.Log("Unhide");
    }

    private IEnumerator HideCountdown()
    {
        yield return new WaitForSeconds(hideTime); // hideTime秒待つ
        if (isFounding) // まだ隠れていれば
        {
            CancelHide();
        }
    }

    

    private void FixedUpdate()
    {
        if (!countdownActive) return;

        //  ここを直さないといけない
        if (isFounding)
        {
            NormalDinosaur dino = GameObject.FindWithTag("Enemy")?.GetComponent<NormalDinosaur>();
            Collider col = GetComponent<Collider>();

            if (dino != null && col != null)
            {
                // 恐竜に見られていれば isTrigger = false（= 衝突有効）
                //見られていなければ isTrigger = true（= すり抜け）
                bool shouldBeTrigger = !dino.IsLooked;

                col.isTrigger = shouldBeTrigger;
                Debug.Log("isTrigger を " + shouldBeTrigger + " に切り替えました（IsLooked: " + dino.IsLooked + "）");
            }

            rigidbody.velocity = Vector3.zero;
            velocity = Vector3.zero;
            moveInputValue = Vector2.zero;
            return;
        }

        // ↓ 既存の処理
        ChangeSpeed();
        image.fillAmount = stamina / maxStamina;

        image.color = lostStamina ? Color.red : Color.green;

        if (moveInputValue.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = camForward * moveInputValue.y + camRight * moveInputValue.x;
            rigidbody.velocity = moveDirection * currentSpeed;
        }
        else
        {
            Vector3 targetVelocity = new Vector3(0, rigidbody.velocity.y, 0);
            rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, targetVelocity, ref velocity, stopTime);
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollisionEnter with: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Enemy"))
        {
            NormalDinosaur dino = collision.gameObject.GetComponent<NormalDinosaur>();

            if (isFounding && dino != null && dino.IsLooked)
            {
                Debug.Log("ロッカー中に見つかって接触：死亡");
                sceneChangeManager.ChangeScene("DeadScene");
            }
            else if (!isFounding)
            {
                Debug.Log("隠れていない状態で接触：死亡");
                sceneChangeManager.ChangeScene("DeadScene");
            }
            else
            {
                Debug.Log("隠れていて見られていないのでセーフ");
            }
        }
    }


}
