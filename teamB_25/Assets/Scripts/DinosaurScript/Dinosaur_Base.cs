using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// �����̊�{AI�𐧌䂷��X�N���v�g�i����E�x���E�ǐՁj
public class Dinosaur_Base : MonoBehaviour
{
    // NavMesh�ɂ�鎩���o�H�T���ƈړ����s���R���|�[�l���g
    private NavMeshAgent agent;

    private Rigidbody rb; // Rigidbody��ǉ�

    // �v���C���[��Transform�i�ǐՑΏہj
    [SerializeField] private Transform playerTransform;

    // �`�F�C�X�i�ǐՁj�ɐ؂�ւ��鋗��
    [SerializeField] private float chaseDistance = 10f;

    // �x����Ԃɓ��鋗���i�`�F�C�X�����j
    [SerializeField] private float vigilanceDistance = 20f;

    // �ǐՒ��̈ړ����x
    [SerializeField] private float chaseSpeed = 6f;

    // �x�����̈ړ����x
    [SerializeField] private float vigilanceSpeed = 2f;

    // ���񒆂̈ړ����x
    [SerializeField] private float patrolSpeed = 2f;

    // ���O�ړ��̍ۂ̐��񑬓x
    [SerializeField] private float turnSpeed = 2f;

    // ����|�C���g�i����p�̖ړI�n�ƂȂ���W�j
    [SerializeField] private Transform[] patrolPoints;

    [SerializeField] private Transform modelTransform;

    [SerializeField] private float leapDistance = 3f;     // ��т���������
    [SerializeField] private float leapSpeed = 10f;       // ��т��ړ����x
    [SerializeField] private float leapDuration = 1.5f;   // ��т��p�����ԁi�b�j
    private Vector3 leapDirection;

    private float timeSinceLastSeen = Mathf.Infinity;
    [SerializeField] private float loseSightDuration = 3f; // �ǐՂ���߂�܂ł̗P�\�b��
    private bool isPlayerVisible = false;

    private float chargeTimer = 0f;
    private float chargeDuration = 1.5f;  // ���ߎ���1�b
    private float leapTimer = 0f;
    float postLeapWaitTimer = 0f;
    bool isWaitingAfterLeap = false;

    // ���݂̏���|�C���g�̃C���f�b�N�X
    private int currentPatrolIndex = 0;

    private Vector3 vigilanceTarget;

    //�i���Ă��鎞��
    private float roarTimer = 0f;
    private float roarDuration = 3f;

    private bool hasRoared = false;

    // �s���X�e�[�g�̒�`
    public enum State
    {
        Patrol,     // ����
        Chase,      // �ǐՒ�
        Vigilance,  // �x����
        Roar,        // �i����
        Leap
    }

    // ���݂̃X�e�[�g�i�����l�͏���j
    private State currentState = State.Patrol;

    // Ray���g����Player���m�V�X�e��
    [SerializeField] private float detectionRange = 10f;      // Ray�̋���
    [SerializeField] private float detectionAngle = 30f;      // ����p�i���E�̋��e�p�x�j

    // ����������
    void Start()
    {
        // NavMeshAgent���擾
        agent = GetComponent<NavMeshAgent>();

        rb = GetComponent<Rigidbody>();

        // �����炵���Ȃ�������������邽�߂�NavMeshAgent �ɉ�]��C�����A���̃X�N���v�g�Ő��䂷��
        agent.updateRotation = false;

        // �ŏ��ɓK���Ȍx���|�C���g��ݒ�
        SetRandomVigilanceTarget();

        // �ŏ��̏���|�C���g�ֈړ��J�n
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    // ���t���[�����s����鏈��
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        //Debug.Log("Current State: " + currentState);

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 direction = agent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation,
                targetRotation * Quaternion.Euler(0f, 180f, 0f),
                turnSpeed * Time.deltaTime
            );
        }

        bool playerDetectedByRay = DetectPlayerByRay();

        if (playerDetectedByRay)
        {
            isPlayerVisible = true;
            timeSinceLastSeen = 0f;
            Debug.Log($"Player detected by ray. timeSinceLastSeen reset to 0");
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
            Debug.Log($"Player NOT detected. timeSinceLastSeen = {timeSinceLastSeen:F2} seconds");

            if (timeSinceLastSeen > loseSightDuration)
            {
                if (isPlayerVisible) Debug.Log("Lost sight of player. Setting isPlayerVisible to false.");
                isPlayerVisible = false;
            }
        }

        switch (currentState)
        {
            case State.Patrol:
                if (isPlayerVisible)
                {
                    SwitchState(State.Roar);  // Patrol��Roar�̂�OK
                }
                else if (distanceToPlayer < vigilanceDistance)
                {
                    SwitchState(State.Vigilance);
                }
                PatrolState();
                break;

            case State.Vigilance:
                if (isPlayerVisible)
                {
                    SwitchState(State.Roar);
                }
                else if (distanceToPlayer >= vigilanceDistance)
                {
                    SwitchState(State.Patrol);
                }
                VigilanceState();
                break;

            case State.Roar:
                RoarState();
                break;

            case State.Chase:
                if (distanceToPlayer <= leapDistance)
                {
                    SwitchState(State.Leap);
                }
                else if (timeSinceLastSeen > loseSightDuration)
                {
                    SwitchState(State.Patrol);
                }
                ChaseState();
                break;

            case State.Leap:
                LeapState();
                break;
        }
    }

    // Ray�Ńv���C���[�����m���鏈��
    private bool DetectPlayerByRay()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f; // �����̖ڐ��̍���
        Vector3 toPlayer = playerTransform.position - origin;
        toPlayer.y = 0f; // ���������Ɍ���i�K�v�ɉ����č폜�j

        float distanceToPlayer = toPlayer.magnitude;
        Vector3 direction = toPlayer.normalized;

        // �v���C���[������p�����m�F
        float angleToPlayer = Vector3.Angle(transform.forward * -1, direction);
        if (angleToPlayer > detectionAngle) return false;

        // �f�o�b�O�\��
        Debug.DrawRay(origin, direction * detectionRange, Color.red);

        // �v���C���[�܂�Ray���΂��A�r���ŏ�Q���ɓ���������false
        if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == playerTransform)
            {
                return true; // �v���C���[��Ray�̐�ɂ���
            }
            else
            {
                return false; // �ǂȂǂɓ������ăv���C���[�������Ȃ�
            }
        }

        return false;
    }

    // �X�e�[�g�ύX���̏���
    void SwitchState(State newState)
    {
        currentState = newState;

        SetSpeedForState(newState); // ���������ŏ�Ԃɉ��������x�������ݒ�

        if (newState == State.Patrol)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
        else if (newState == State.Vigilance)
        {
            agent.SetDestination(playerTransform.position);
        }
        else if (newState == State.Roar)
        {
            agent.ResetPath(); // ��~
            roarTimer = 0f;
        }
        else if (newState == State.Leap)
        {
            leapTimer = 0f;
        }

        agent.enabled = true;
    }

    // ��Ԃɉ��������x�ݒ���ꌳ��
    void SetSpeedForState(State state)
    {
        switch (state)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                break;
            case State.Vigilance:
                agent.speed = vigilanceSpeed;
                break;
            case State.Chase:
                agent.speed = chaseSpeed;
                break;
                // Roar �� Leap �� NavMeshAgent ���g��Ȃ����ߑ��x�ݒ肵�Ȃ�
        }
    }
    // ���񒆂̏���
    void PatrolState()
    {
        // ����|�C���g���ݒ肳��Ă��Ȃ��ꍇ�͏������Ȃ�
        if (patrolPoints.Length == 0) return;
        AudioManager.Instance.DestroySE("Dash");
        AudioManager.Instance.PlaySELoop("Walk", transform);

        // ���݂̖ړI�n�ɓ��B������A���̃|�C���g�Ɉړ�
        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    // �ǐՒ��̏����itransform�ɂ�鎩�O�ړ��j
    void ChaseState()
    {
        agent.SetDestination(playerTransform.position);

        AudioManager.Instance.DestroySE("Walk");
        AudioManager.Instance.PlaySELoop("Dash", transform);
    }


    void SetRandomVigilanceTarget()
    {
        // �x�������͈͓̔��Ń����_���ȕ����Ƌ��������߂�
        Vector2 randomCircle = Random.insideUnitCircle * vigilanceDistance;
        vigilanceTarget = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // NavMesh��̈ʒu�ɂ���Ȃ�T���v��
        NavMeshHit hit;
        if (NavMesh.SamplePosition(vigilanceTarget, out hit, 5f, NavMesh.AllAreas))
        {
            vigilanceTarget = hit.position;
        }

        agent.SetDestination(vigilanceTarget);
    }

    // �x�����̏����i�������߂Â��j
    void VigilanceState()
    {
        // �ړI�n�ɋ߂Â�����V�����x���|�C���g��ݒ�
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            SetRandomVigilanceTarget();
        }
    }

    void RoarState()
    {
        AudioManager.Instance.DestroySE("Dash");
        AudioManager.Instance.DestroySE("Walk");
        roarTimer += Time.deltaTime;

        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        if (!hasRoared && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("Rouring", transform.position);
            hasRoared = true;
        }

        if (roarTimer >= roarDuration)
        {
            hasRoared = false; // ����Roar�̂��߂Ƀ��Z�b�g
            agent.isStopped = false;
            SwitchState(State.Chase);
        }
    }

    void LeapState()
    {
        // 1. ���ߎ��Ԃ̐i�s
        if (!isWaitingAfterLeap)
        {
            chargeTimer += Time.deltaTime;

            if (chargeTimer < chargeDuration)
            {
                // ���ߊ��Ԓ��͓������Ȃ�
                return;
            }

            // 2. ��т������̌���i1�񂾂��j
            if (leapTimer == 0f)
            {
                leapDirection = (playerTransform.position - transform.position).normalized;
                leapDirection.y = 0f;
            }

            // 3. ��т��ړ�
            leapTimer += Time.deltaTime;
            transform.position += leapDirection * leapSpeed * Time.deltaTime;

            // 4. ��яI�������ҋ@��Ԃ�
            if (leapTimer >= leapDuration)
            {
                isWaitingAfterLeap = true;
                postLeapWaitTimer = 0f;
            }
        }
        else
        {
            // 5. ��яI�����1�b�ҋ@����
            postLeapWaitTimer += Time.deltaTime;

            if (postLeapWaitTimer >= 1f)
            {
                // 6. �^�C�}�[���Z�b�g����Chase��
                chargeTimer = 0f;
                leapTimer = 0f;
                isWaitingAfterLeap = false;
                SwitchState(State.Chase);
            }
        }
    }


    // transform�ɂ�鋰�����ۂ��ړ������i�O�i�{��]�j
    void MoveTowards(Vector3 target, float speed)
    {
        // �i�s�������v�Z�iy�͖������Ēn�ʂɐ����ȕ����j
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0f;

        // ��]�����i���炩�Ɍ�����ς���j
        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, turnSpeed * Time.deltaTime);
        }

        // �O���Ɉړ��iNavMeshAgent���g��Ȃ��j
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // ����͈́i�~�Ɗp�x�j���V�[���r���[�ŕ`��
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // �����̖ڐ��̍���
        Vector3 origin = transform.position + Vector3.up * 1.5f;

        // ���싗���̉~�i�΁j
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, detectionRange);

        // ����p�i���͈̔́j
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward;

        // �������x�N�g��
        Vector3 leftDir = Quaternion.Euler(0, -detectionAngle, 0) * forward;
        // �E�����x�N�g��
        Vector3 rightDir = Quaternion.Euler(0, detectionAngle, 0) * forward;

        // ����p�̗��[�̐���`��
        Gizmos.DrawRay(origin, leftDir * detectionRange * -1);
        Gizmos.DrawRay(origin, rightDir * detectionRange * -1);

        // �����̒��S�i�O���j
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, forward * detectionRange * -1);
    }

}
