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
        PatrolState(); // ステート管理後は切り替えで呼び出す想定
    }

    void PatrolState()
    {
        if (patrolPoints.Length == 0) return;

        // 到達したら次へ
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

    // 他のステートもここに用意済み
    void ChaseState() { }
    void ScaredState() { }
}
