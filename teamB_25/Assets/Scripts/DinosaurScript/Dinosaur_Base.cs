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
    [SerializeField] private float vigilanceDistance = 20f;

    // 追跡中の移動速度
    [SerializeField] private float chaseSpeed = 6f;

    // 警戒中の移動速度
    [SerializeField] private float vigilanceSpeed = 2f;

    // 巡回中の移動速度
    [SerializeField] private float patrolSpeed = 2f;

    // 自前移動の際の旋回速度
    [SerializeField] private float turnSpeed = 2f;

    // 巡回ポイント（巡回用の目的地となる座標）
    [SerializeField] private Transform[] patrolPoints;

    [SerializeField] private Transform modelTransform;

    // 現在の巡回ポイントのインデックス
    private int currentPatrolIndex = 0;

    private Vector3 vigilanceTarget;

    //吠えている時間
    private float roarTimer = 0f;
    private float roarDuration = 3f;

    private bool hasRoared = false;

    // 行動ステートの定義
    public enum State
    {
        Patrol,     // 巡回中
        Chase,      // 追跡中
        Vigilance,  // 警戒中
        Roar        // 吠え中
    }

    // 現在のステート（初期値は巡回）
    private State currentState = State.Patrol;

    // Rayを使ったPlayer検知システム
    [SerializeField] private float detectionRange = 10f;      // Rayの距離
    [SerializeField] private float detectionAngle = 30f;      // 視野角（左右の許容角度）

    // 初期化処理
    void Start()
    {
        // NavMeshAgentを取得
        agent = GetComponent<NavMeshAgent>();

        rb = GetComponent<Rigidbody>();

        // 巡回中の速度に設定
        agent.speed = patrolSpeed;

        // 最初に適当な警戒ポイントを設定
        SetRandomVigilanceTarget();

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

        // 現在のステートをログに出力
        Debug.Log("Current State: " + currentState);

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            // 進行方向に向かせた上で、Y軸を180°回転させる
            Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
            modelTransform.rotation = lookRotation * Quaternion.Euler(0f, 180f, 0f);
        }

        // Rayを使ってプレイヤー検知
        bool playerDetectedByRay = DetectPlayerByRay();

        // ステートの切り替え判定
        switch (currentState)
        {
            case State.Patrol:
                if (playerDetectedByRay) // ←距離チェックを除外
                {
                    SwitchState(State.Roar);
                }
                else if (distanceToPlayer < vigilanceDistance)
                {
                    SwitchState(State.Vigilance);
                }
                PatrolState();
                break;

            case State.Vigilance:
                if (playerDetectedByRay) // ←距離チェックを除外
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
                // 距離が遠すぎる場合のみ戻す（ここではRayチェックしない）
                if (distanceToPlayer >= vigilanceDistance)
                {
                    SwitchState(State.Patrol);
                }
                ChaseState();
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

    // Rayでプレイヤーを検知する処理
    private bool DetectPlayerByRay()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f; // 恐竜の目線の高さ
        Vector3 toPlayer = playerTransform.position - origin;
        toPlayer.y = 0f; // 水平方向に限定（必要に応じて削除可）

        float distanceToPlayer = toPlayer.magnitude;
        Vector3 direction = toPlayer.normalized;

        // プレイヤーが視野角内か確認
        float angleToPlayer = Vector3.Angle(transform.forward * -1, direction);
        if (angleToPlayer > detectionAngle) return false;

        // デバッグ表示
        Debug.DrawRay(origin, direction * detectionRange, Color.red);

        // プレイヤーまでRayを飛ばし、途中で障害物に当たったらfalse
        if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == playerTransform)
            {
                return true; // プレイヤーがRayの先にいる
            }
            else
            {
                return false; // 壁などに当たってプレイヤーが見えない
            }
        }

        return false;
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
        else if (newState == State.Vigilance)
        {
            agent.speed = vigilanceSpeed;
            agent.SetDestination(playerTransform.position);
        }
        else if (newState == State.Roar)
        {
            agent.ResetPath(); // 停止
            roarTimer = 0f;
        }
        else if (newState == State.Chase)
        {
            agent.speed = chaseSpeed;
        }

        // NavMeshAgentは常にONでよい
        agent.enabled = true;
    }


    // 巡回中の処理
    void PatrolState()
    {
        // 巡回ポイントが設定されていない場合は処理しない
        if (patrolPoints.Length == 0) return;
        AudioManager.Instance.PlaySELoop("Walk", transform);

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
        AudioManager.Instance.PlaySELoop("Dash", transform);
    }

    void SetRandomVigilanceTarget()
    {
        // 警戒距離の範囲内でランダムな方向と距離を決める
        Vector2 randomCircle = Random.insideUnitCircle * vigilanceDistance;
        vigilanceTarget = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // NavMesh上の位置にするならサンプル
        NavMeshHit hit;
        if (NavMesh.SamplePosition(vigilanceTarget, out hit, 5f, NavMesh.AllAreas))
        {
            vigilanceTarget = hit.position;
        }

        agent.SetDestination(vigilanceTarget);
    }

    // 警戒中の処理（ゆっくり近づく）
    void VigilanceState()
    {
        agent.speed = vigilanceSpeed;

        // 目的地に近づいたら新しい警戒ポイントを設定
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            SetRandomVigilanceTarget();
        }
    }

    void RoarState()
    {
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
            hasRoared = false; // 次回Roarのためにリセット
            agent.isStopped = false;
            SwitchState(State.Chase);
        }
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

    // 視野範囲（円と角度）をシーンビューで描画
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // 恐竜の目線の高さ
        Vector3 origin = transform.position + Vector3.up * 1.5f;

        // 視野距離の円（緑）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, detectionRange);

        // 視野角（扇状の範囲）
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward;

        // 左方向ベクトル
        Vector3 leftDir = Quaternion.Euler(0, -detectionAngle, 0) * forward;
        // 右方向ベクトル
        Vector3 rightDir = Quaternion.Euler(0, detectionAngle, 0) * forward;

        // 視野角の両端の線を描く
        Gizmos.DrawRay(origin, leftDir * detectionRange * -1);
        Gizmos.DrawRay(origin, rightDir * detectionRange * -1);

        // 視線の中心（前方）
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, forward * detectionRange * -1);
    }

}
