using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Dinosaur_Base : MonoBehaviour
{
    private NavMeshAgent agent;

    [SerializeField] private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void Update()
    {
        PatrolState(); // �X�e�[�g�Ǘ���͐؂�ւ��ŌĂяo���z��
    }

    void PatrolState()
    {
        if (patrolPoints.Length == 0) return;

        // ���B�����玟��
        if (!agent.pathPending && agent.remainingDistance <= 0.1f)
        {
            GoToNextPatrolPoint();
        }
    }

    void GoToNextPatrolPoint()
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    // ���̃X�e�[�g�������ɗp�Ӎς�
    void ChaseState() { }
    void ScaredState() { }
}
