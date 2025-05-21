using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBase : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] public float stopTime = 0f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip runSound;
    [SerializeField] private PlayerBase player; // PlayerBase スクリプト参照

    private Rigidbody rigidbody;
    private GameInputs gameInputs;
    private Vector2 moveInputValue;
    private Vector3 velocity = Vector3.zero;
    private AudioSource audioSource;
    private float stepTimer;


    private bool isFounding = false;
    private bool isRunning = false;

    public int Hp = 0;    

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();

        gameInputs = new GameInputs();

        gameInputs.Player.Move.started += OnMove;
        gameInputs.Player.Move.performed += OnMove;
        gameInputs.Player.Move.canceled += OnMove;

        gameInputs.Player.Run.started += ctx => {
            isRunning = true;
            Debug.Log("Run started");
        };
        gameInputs.Player.Run.canceled += ctx => {
            isRunning = false;
            Debug.Log("Run canceled");
        };



        gameInputs.Enable();
    }

    private void OnDestroy()
    {
        gameInputs?.Dispose();
    }

    public void Start()
    {
        Attack();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (player.IsRunning())
        {
            if (audioSource.clip != runSound || !audioSource.isPlaying)
            {
                audioSource.clip = runSound;
                audioSource.Play();
            }
        }
        else if (player.IsMoving())
        {
            if (audioSource.clip != walkSound || !audioSource.isPlaying)
            {
                audioSource.clip = walkSound;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
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

    public bool IsMoving()
    {
        return moveInputValue.sqrMagnitude > 0.01f;
    }

    public bool IsRunning()
    {
        Debug.Log($"isRunning={isRunning}, moveInputValue={moveInputValue}");
        return isRunning && moveInputValue.y > 0.5f;
    }    

    private void FixedUpdate()
    {
        float currentSpeed = (isRunning && moveInputValue.y > 0) ? runSpeed : moveSpeed;


        if (moveInputValue.sqrMagnitude > 0.01f) // ほぼゼロでなければ
        {
            // カメラの方向に合わせた移動
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
            // 停止時は滑らかに止まる
            Vector3 targetVelocity = new Vector3(0, rigidbody.velocity.y, 0);
            rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, targetVelocity, ref velocity, stopTime);
        }
    }

}
