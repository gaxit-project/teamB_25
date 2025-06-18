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
    [SerializeField] private PlayerBase player; // PlayerBase �X�N���v�g�Q��
    [SerializeField] private float stamina = 10f;
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float staminaDuration;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Image image;
    [SerializeField] private Image hideview;

    private Rigidbody rigidbody;
    private GameInputs gameInputs;
    private Vector2 moveInputValue;
    private Vector3 velocity = Vector3.zero;
    private float stepTimer;
    private Vector3 preHidePosition;
    private Transform currentHidePlace = null;
    private Quaternion currentHidePlaceRotation;
    private Collider currentHideCollider; // 隠れる場所のCollider


    private bool isFounding = false;
    public bool IsFounding => isFounding;

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

        gameInputs = new GameInputs();

        if(isFounding == false)
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
                AudioManager.Instance.PlaySE("OpenLocker", transform.position);
                isFounding = true;
                isChangingCamera = true;
                preHidePosition = transform.position;
                Vector3 targetPos = currentHidePlace.position;
                transform.position = new Vector3(targetPos.x, preHidePosition.y, targetPos.z);
                transform.rotation = Quaternion.Euler(0, currentHidePlaceRotation.eulerAngles.y, 0);

                AudioManager.Instance.PlaySE("SE2",transform.position);
                hideview.gameObject.SetActive(true);

                rigidbody.velocity = Vector3.zero;

                // 動かないようにKinematic化
                rigidbody.isKinematic = true;
                // 移動・回転を凍結
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                if (text != null)
                {
                    text.text = "Exit x";
                }
                Debug.Log("Hiding");
                
                if (currentHideCollider != null)
                {
                    currentHideCollider.enabled = false; // 当たり判定を無効化
                }
            }
            // 隠れてる状態で押されたら解除
            else if (isFounding)
            {
                AudioManager.Instance.PlaySE("CloseLocker", transform.position);
                isFounding = false;
                isChangingCamera = false;
                
                if (currentHideCollider != null)
                {
                    currentHideCollider.enabled = true; // 当たり判定を復活
                }

                rigidbody.isKinematic = false;
                rigidbody.constraints = RigidbodyConstraints.None;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotation; // 回転だけ固定
                hideview.gameObject.SetActive(false);

                transform.position = preHidePosition;
                currentHidePlace = null;
                currentHideCollider = null;
                

                Debug.Log("Unhide"); 
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
    }

    public virtual void Attack()
    {
        Debug.Log("test");
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInputValue = context.ReadValue<Vector2>();
    }

    public void OnCollisionStay(Collision other)
    {
        if(other.gameObject.CompareTag("HidePlace"))
        {
            currentHidePlace = other.transform;
            currentHidePlaceRotation = other.transform.rotation;
            Debug.Log("Enter HidePlace"); // ← これで呼ばれているか確認
            currentHideCollider = other.collider;
            if(text != null)
            {
                text.gameObject.SetActive(true);
                text.text = "Hide x";
            }
            
        }
        //isHideCollision = false;
    }

    public void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("HidePlace"))
        {
            if (!isFounding)
            {
                currentHidePlace = null; 
                Debug.Log("Exit HidePlace");

                if(text != null)
                {
                    text.gameObject.SetActive(false);
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
        //Debug.Log($"isRunning={isRunning}, moveInputValue={moveInputValue}");
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


    private void FixedUpdate()
    {
        if (!countdownActive) return;  // ← 隠れ中は処理を中断
        
        ChangeSpeed();
        image.fillAmount = stamina / maxStamina;

        if (lostStamina)
        {
            // 回復中（スタミナ切れ）→ 赤っぽい色
            image.color = Color.red;
        }
        else
        {
            // 通常 → 緑色
            image.color = Color.green;
        }

        if (isFounding)
        { 
            rigidbody.velocity = Vector3.zero;
            velocity = Vector3.zero;
            moveInputValue = Vector2.zero;
            return;
        }

        if (moveInputValue.sqrMagnitude > 0.01f) // �قڃ[���łȂ����
        {
            // �J�����̕����ɍ��킹���ړ�
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
            // ��~���͊��炩�Ɏ~�܂�
            Vector3 targetVelocity = new Vector3(0, rigidbody.velocity.y, 0);
            rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, targetVelocity, ref velocity, stopTime);
        }
    }

}
