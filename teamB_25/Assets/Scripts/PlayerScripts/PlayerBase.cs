using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerBase : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float tiredSpeed = 1.5f;
    [SerializeField] private float stopTime = 0.1f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float staminaRecoveryDuration = 4f;
    [SerializeField] private Image staminaImage;

    [Header("Hide")]
    [SerializeField] private float defaultHideTime = 5f;
    [SerializeField] private Image hideTimeImage;
    [SerializeField] private TMP_Text actionText;

    [Header("Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private FirstPersonCameraController firstPersonCamera;

    [Header("Death")]
    [SerializeField] private RawImage deathEffectImage;
    [SerializeField] private float effectDuration = 3f;
    [SerializeField] private SceneChangeManager sceneChangeManager;

    private Rigidbody rb;
    private GameInputs gameInputs;
    private Vector2 moveInput;
    private Vector3 velocity = Vector3.zero;

    private float stamina;
    private float currentSpeed;
    private bool isPushRun = false;
    private bool isHidden = false;
    private float currentHideTime;
    private Coroutine hideCoroutine;

    private Transform hidePlace;
    private Quaternion hidePlaceRotation;
    private Collider hideCollider;

    private bool isProcessingDeath = false;

    // 🔹 Dinosaur / Camera / Timer から参照されるプロパティ
    public bool IsFounding => isHidden; // 隠れているかどうか
    public bool IsRunningNow => isPushRun && moveInput.y > 0 && stamina > 0;
    public bool IsWalkingNow => moveInput.sqrMagnitude > 0.01f && !IsRunningNow;
    public bool IsRunning => IsRunningNow; // Camera 用の旧API互換
    public bool countdownActive { get; set; } // StartTimer 用

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gameInputs = new GameInputs();

        gameInputs.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        gameInputs.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        gameInputs.Player.Run.started += ctx => isPushRun = true;
        gameInputs.Player.Run.canceled += ctx => isPushRun = false;

        gameInputs.Player.Tool.started += ctx =>
        {
            if (!isHidden && hidePlace != null)
            {
                EnterHide();
            }
            else if (isHidden)
            {
                CancelHide();
            }
        };

        gameInputs.Enable();
    }

    private void Start()
    {
        stamina = maxStamina;
        currentSpeed = moveSpeed;
        countdownActive = false; // 初期化
        if (deathEffectImage != null) deathEffectImage.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        UpdateSpeed();
        UpdateStaminaUI();

        if (moveInput.sqrMagnitude > 0.01f && !isHidden)
        {
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
            rb.velocity = moveDir * currentSpeed;
        }
        else
        {
            Vector3 targetVelocity = new Vector3(0, rb.velocity.y, 0);
            rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref velocity, stopTime);
        }
    }

    // PlayerBase.cs
    public virtual void Attack()
    {
        Debug.Log("Base Attack");
    }

    private void UpdateSpeed()
    {
        if (isPushRun && stamina > 0f && moveInput.y > 0)
        {
            currentSpeed = runSpeed;
            stamina -= Time.fixedDeltaTime;
            if (stamina < 0) stamina = 0;
        }
        else
        {
            currentSpeed = moveSpeed;
            if (stamina < maxStamina)
                stamina += (maxStamina / staminaRecoveryDuration) * Time.fixedDeltaTime;
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaImage != null)
            staminaImage.fillAmount = stamina / maxStamina;
    }

    private void EnterHide()
    {
        isHidden = true;
        currentHideTime = defaultHideTime;
        rb.velocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        transform.position = new Vector3(hidePlace.position.x, transform.position.y, hidePlace.position.z);
        transform.rotation = Quaternion.Euler(0, hidePlaceRotation.eulerAngles.y, 0);

        hideTimeImage.gameObject.SetActive(true);
        actionText.text = "Exit";

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideCountdown());
    }

    private void CancelHide()
    {
        isHidden = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        hideTimeImage.gameObject.SetActive(false);
        actionText.text = "Hide";
    }

    private IEnumerator HideCountdown()
    {
        while (currentHideTime > 0f)
        {
            currentHideTime -= Time.deltaTime;
            hideTimeImage.fillAmount = currentHideTime / defaultHideTime;
            yield return null;
        }
        CancelHide();
    }

    private void OnTriggerStay(Collider other)
    {
        if (isHidden) return;

        if (other.CompareTag("HidePlace"))
        {
            hidePlace = other.transform;
            hidePlaceRotation = other.transform.rotation;
            hideCollider = other.GetComponent<Collider>();
            actionText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HidePlace"))
        {
            hidePlace = null;
            hideCollider = null;
            actionText.gameObject.SetActive(false);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (isProcessingDeath) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            StartCoroutine(PlayDeathEffect());
        }
    }

    private IEnumerator PlayDeathEffect()
    {
        isProcessingDeath = true;

        if (deathEffectImage != null) deathEffectImage.gameObject.SetActive(true);

        yield return new WaitForSeconds(effectDuration);

        sceneChangeManager.ChangeScene("DeadScene");
    }
}
