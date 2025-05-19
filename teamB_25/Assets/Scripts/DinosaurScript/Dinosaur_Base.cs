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
    [SerializeField] private float vigilanceDistance = 15f;

    // �ǐՒ��̈ړ����x
    [SerializeField] private float chaseSpeed = 3f;

    // �x�����̈ړ����x
    [SerializeField] private float vigilanceSpeed = 1f;

    // ���񒆂̈ړ����x
    [SerializeField] private float patrolSpeed = 1.5f;

    // ���O�ړ��̍ۂ̐��񑬓x
    [SerializeField] private float turnSpeed = 2f;

    // ����|�C���g�i����p�̖ړI�n�ƂȂ���W�j
    [SerializeField] private Transform[] patrolPoints;

    // ���݂̏���|�C���g�̃C���f�b�N�X
    private int currentPatrolIndex = 0;

    // �s���X�e�[�g�̒�`
    public enum State
    {
        Patrol,     // ����
        Chase,      // �ǐՒ�
        Vigilance   // �x����
    }

    // ���݂̃X�e�[�g�i�����l�͏���j
    private State currentState = State.Patrol;

    // ����������
    void Start()
    {
        // NavMeshAgent���擾
        agent = GetComponent<NavMeshAgent>();

        rb = GetComponent<Rigidbody>();

        // ���񒆂̑��x�ɐݒ�
        agent.speed = patrolSpeed;

        // �ŏ��̏���|�C���g�ֈړ��J�n
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    // ���t���[�����s����鏈��
    void Update()
    {
        // �v���C���[�Ƃ̋����𑪒�
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // �X�e�[�g�̐؂�ւ�����
        switch (currentState)
        {
            case State.Patrol:
                // �v���C���[���߂����Chase�ցA���߂����Vigilance��
                if (distanceToPlayer < chaseDistance)
                    SwitchState(State.Chase);
                else if (distanceToPlayer < vigilanceDistance)
                    SwitchState(State.Vigilance);
                break;

            case State.Vigilance:
                // ���߂Â���Chase�ցA���ꂽ��Patrol�֖߂�
                if (distanceToPlayer < chaseDistance)
                    SwitchState(State.Chase);
                else if (distanceToPlayer >= vigilanceDistance)
                    SwitchState(State.Patrol);
                break;

            case State.Chase:
                // �v���C���[�����������Patrol�ɖ߂�
                if (distanceToPlayer >= vigilanceDistance)
                    SwitchState(State.Patrol);
                break;
        }

        // ���݂̃X�e�[�g�ɉ��������������s
        switch (currentState)
        {
            case State.Patrol:
                PatrolState();     // ���񏈗�
                break;
            case State.Chase:
                ChaseState();      // �ǐՏ���
                break;
            case State.Vigilance:
                VigilanceState();  // �x������
                break;
        }
    }

    // �X�e�[�g�ύX���̏���
    void SwitchState(State newState)
    {
        currentState = newState;

        if (newState == State.Patrol)
        {
            agent.speed = patrolSpeed;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
        else if (newState == State.Chase)
        {
            agent.speed = chaseSpeed;
        }
        else if (newState == State.Vigilance)
        {
            agent.speed = vigilanceSpeed;
        }

        // NavMeshAgent�͏��ON�ł悢
        agent.enabled = true;
    }


    // ���񒆂̏���
    void PatrolState()
    {
        // ����|�C���g���ݒ肳��Ă��Ȃ��ꍇ�͏������Ȃ�
        if (patrolPoints.Length == 0) return;

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
    }

    // �x�����̏����i�������߂Â��j
    void VigilanceState()
    {
        agent.SetDestination(playerTransform.position);
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

}
