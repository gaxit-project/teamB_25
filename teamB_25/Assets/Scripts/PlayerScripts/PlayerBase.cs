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
    [SerializeField] private float reloadtime = 1f;
    [SerializeField] private FirstPersonCameraController firstPersonCamera;


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

    private bool wasLookedWhenHiding = false;

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
                wasLookedWhenHiding = false;
                StopCoroutine(HideCountdown()); // プレイヤーが自分で出たら中断
                CancelHide();
            }
            wasLookedWhenHiding = Dinosaur_Base.dinosLookingAtPlayer.Count > 0;
            Debug.LogError("死亡" + wasLookedWhenHiding);
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

        Debug.Log($"isFounding: {isFounding}, 見ている恐竜の数: {Dinosaur_Base.dinosLookingAtPlayer.Count}");

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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (isFounding && wasLookedWhenHiding)
            {
                Debug.Log("ロッカー中に見つかって接触：死亡");
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM();
                }
                sceneChangeManager.ChangeScene("DeadScene");
            }
            else if (!isFounding)
            {
                Debug.Log("隠れていない状態で接触：死亡");
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM();
                }
                sceneChangeManager.ChangeScene("DeadScene");
            }
            else
            {
                Debug.Log("隠れていて見られていないのでセーフ");
            }
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (isFounding) return; // 既に隠れてるなら処理しない

        if (other.gameObject.CompareTag("HidePlace"))
        {
            Collider parentCollider = other.transform.parent.GetComponent<Collider>();
            
            currentHidePlace = parentCollider.transform;
            currentHidePlaceRotation = parentCollider.transform.rotation;
            Debug.Log("Enter HidePlace"); // ← これで呼ばれているか確認
            if (other.transform.parent != null)
            {
                if (parentCollider != null)
                {
                    currentHideCollider = parentCollider;
                    Debug.Log("Parent Collider: " + parentCollider.name);
                }
                else
                {
                    Debug.LogWarning("1つ上の親にはColliderがありません");
                }
            }


            if (text != null)
            {
                text.gameObject.SetActive(true);
                hideButton.gameObject.SetActive(true);
                text.text = "Hide";
            }

        }        

        if (other.gameObject.CompareTag("Breaker") && !breaker.isActivated)
        {
            Transform parent = other.transform.parent;// 1つ上の親のColliderを取得
            breaker = parent.GetComponent<Breaker>();//親のオブジェクトのBreakerスクリプトを取得
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
        //rigidbody.isKinematic = false;  // kinematic解除
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

        if (currentHideCollider != null && currentHideCollider.CompareTag("HidePlace") && dinosaur_Base.IsLooked)
        {
            currentHideCollider.enabled = false;
        }
    }

    private void CancelHide()
    {
        AudioManager.Instance.PlaySE("CloseLocker", transform.position);
        isChangingCamera = false;
        isFounding = false;
        
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

        //rigidbody.isKinematic = false;
        rigidbody.constraints = RigidbodyConstraints.None;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation; // 回転だけ固定
        hideview.gameObject.SetActive(false);

       
        if (currentHidePlace != null)
        {
            // ロッカーの前に出る位置を計算（ロッカーの向きに対して前方へ1m）
            Vector3 exitDirection = currentHidePlace.forward;
            Vector3 exitPos = currentHidePlace.position + exitDirection * 1.0f;
            transform.position = new Vector3(exitPos.x, preHidePosition.y, exitPos.z);

            float yAngle = currentHidePlace.rotation.eulerAngles.y;
            firstPersonCamera.SetRotation(yAngle, 0f);
        }
        else
        {
            Debug.LogWarning("currentHidePlace is null in CancelHide!");
        }

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

}
