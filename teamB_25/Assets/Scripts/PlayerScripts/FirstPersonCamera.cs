using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraController : MonoBehaviour
{
    [SerializeField] private Transform playerBody; // �v���C���[�{��
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float shakeAmount = 0.05f;
    [SerializeField] private float shakeSpeed = 10f;

    [SerializeField] private PlayerBase playerBase; // �v���C���[�̃X�N���v�g�Q��
    private Vector3 initialLocalPos;
    private float shakeTimer = 0f;

    private GameInputs inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float yRotation = 0f;


    private void Awake()
    {
        inputActions = new GameInputs();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
    }

    private void OnDisable()
    {
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;
        inputActions.Disable();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        Vector2 delta = lookInput * sensitivity;

        // �㉺�F�J�����ɂ����K�p
        xRotation -= delta.y;
        xRotation = Mathf.Clamp(xRotation, -40f, 20f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // ���E�F�v���C���[�{�̂ɓK�p
        yRotation += delta.x;
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);

        if (playerBase.IsRunning()) // �v���C���[�������Ă��邩����i���ɗႠ��j
        {
            shakeTimer += Time.deltaTime * shakeSpeed;
            float x = Mathf.Sin(shakeTimer) * shakeAmount;
            float y = Mathf.Cos(shakeTimer * 2f) * shakeAmount * 0.5f;
            transform.localPosition = initialLocalPos + new Vector3(x, y, 0f);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos, Time.deltaTime * 5f);
            shakeTimer = 0f;
        }
    }

    private void Start()
    {
        // �v���C���[�{�̂̌����� yRotation �����킹��
        yRotation = playerBody.eulerAngles.y;

        // �J�����̏㉺���������f���������ꍇ�i�K�v�Ȃ�j
        xRotation = transform.localEulerAngles.x;

        initialLocalPos = transform.localPosition;
    }
}
