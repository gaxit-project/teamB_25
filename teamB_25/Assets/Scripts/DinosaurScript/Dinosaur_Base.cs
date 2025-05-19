using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 恐竜の基本AIを制御するスクリプト（巡回・警戒・追跡）
public class Dinosaur_Base : MonoBehaviour
{
    // NavMeshによる自動経路探索と移動を行うコンポーネント
    private NavMeshAgent agent;

    private Rigidbody rb; // Rigidbodyを追加

    // プレイヤーのTransform（追跡対象）
    [SerializeField] private Transform playerTransform;

    // チェイス（追跡）に切り替える距離
    [SerializeField] private float chaseDistance = 10f;

    // 警戒状態に入る距離（チェイス未満）
    [SerializeField] private float vigilanceDistance = 15f;

    // 追跡中の移動速度
    [SerializeField] private float chaseSpeed = 3f;

    // 警戒中の移動速度
    [SerializeField] private float vigilanceSpeed = 1f;

    // 巡回中の移動速度
    [SerializeField] private float patrolSpeed = 1.5f;

    // 自前移動の際の旋回速度
    [SerializeField] private float turnSpeed = 2f;

    // 巡回ポイント（巡回用の目的地となる座標）
    [SerializeField] private Transform[] patrolPoints;

    // 現在の巡回ポイントのインデックス
    private int currentPatrolIndex = 0;

    // 行動ステートの定義
    public enum State
    {
        Patrol,     // 巡回中
        Chase,      // 追跡中
        Vigilance   // 警戒中
    }

    // 現在のステート（初期値は巡回）
    private State currentState = State.Patrol;

    // 初期化処理
    void Start()
    {
        // NavMeshAgentを取得
        agent = GetComponent<NavMeshAgent>();

        rb = GetComponent<Rigidbody>();

        // 巡回中の速度に設定
        agent.speed = patrolSpeed;

        // 最初の巡回ポイントへ移動開始
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    // 毎フレーム実行される処理
    void Update()
    {
        // プレイヤーとの距離を測定
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // ステートの切り替え判定
        switch (currentState)
        {
            case State.Patrol:
                // プレイヤーが近ければChaseへ、やや近ければVigilanceへ
                if (distanceToPlayer < chaseDistance)
                    SwitchState(State.Chase);
                else if (distanceToPlayer < vigilanceDistance)
                    SwitchState(State.Vigilance);
                break;

            case State.Vigilance:
                // より近づけばChaseへ、離れたらPatrolへ戻る
                if (distanceToPlayer < chaseDistance)
                    SwitchState(State.Chase);
                else if (distanceToPlayer >= vigilanceDistance)
                    SwitchState(State.Patrol);
                break;

            case State.Chase:
                // プレイヤーが遠ざかればPatrolに戻る
                if (distanceToPlayer >= vigilanceDistance)
                    SwitchState(State.Patrol);
                break;
        }

        // 現在のステートに応じた処理を実行
        switch (currentState)
        {
            case State.Patrol:
                PatrolState();     // 巡回処理
                break;
            case State.Chase:
                ChaseState();      // 追跡処理
                break;
            case State.Vigilance:
                VigilanceState();  // 警戒処理
                break;
        }
    }

    // ステート変更時の処理
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

        // NavMeshAgentは常にONでよい
        agent.enabled = true;
    }


    // 巡回中の処理
    void PatrolState()
    {
        // 巡回ポイントが設定されていない場合は処理しない
        if (patrolPoints.Length == 0) return;

        // 現在の目的地に到達したら、次のポイントに移動
        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    // 追跡中の処理（transformによる自前移動）
    void ChaseState()
    {
        agent.SetDestination(playerTransform.position);
    }

    // 警戒中の処理（ゆっくり近づく）
    void VigilanceState()
    {
        agent.SetDestination(playerTransform.position);
    }

    // transformによる恐竜っぽい移動処理（前進＋回転）
    void MoveTowards(Vector3 target, float speed)
    {
        // 進行方向を計算（yは無視して地面に水平な方向）
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0f;

        // 回転処理（滑らかに向きを変える）
        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, turnSpeed * Time.deltaTime);
        }

        // 前方に移動（NavMeshAgentを使わない）
        transform.position += transform.forward * speed * Time.deltaTime;
    }

}
