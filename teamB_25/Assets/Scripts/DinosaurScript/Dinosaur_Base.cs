using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 恐竜の基本AIを制御するスクリプト（巡回・警戒・追跡）
public class Dinosaur_Base : MonoBehaviour
{
    // === 必須コンポーネント ===
    private NavMeshAgent agent;
    private Rigidbody rb;

    // === 共通オブジェクト参照 ===
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform modelTransform;

    // === Patrolで使っている変数 ===
    [Header("Patrol 設定")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    private int currentPatrolIndex = 0;
    private float idleTimer = 0f;
    private float waitDuration = 4f;
    private float nextIdleTime = 0f;
    private bool isWaiting = false;

    // === Vigilanceで使っている変数 ===
    [Header("Vigilance 設定")]
    [SerializeField] private float vigilanceWalkDistance = 20f;
    [SerializeField] private float vigilanceRunDistance = 30f;
    [SerializeField] private float vigilanceSpeed = 2f;
    private Vector3 vigilanceTarget;

    // === Chaseで使っている変数 ===
    [Header("Chase 設定")]
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float chaseSpeed = 6f;
    private float timeSinceLastSeen = Mathf.Infinity;
    [SerializeField] private float loseSightDuration = 3f;
    private bool isPlayerVisible = false;

    // === Roarで使っている変数 ===
    [Header("Roar 設定")]
    private float roarTimer = 0f;
    private float roarDuration = 3f;
    private bool hasRoared = false;

    // === Leapで使っている変数 ===
    [Header("Leap 設定")]
    [SerializeField] private float leapDistance = 3f;
    [SerializeField] private float leapSpeed = 10f;
    [SerializeField] private float leapDuration = 1.5f;
    private float chargeTimer = 0f;
    private float chargeDuration = 1.5f;
    private Vector3 leapDirection;
    private float leapTimer = 0f;
    private float postLeapWaitTimer = 0f;
    private bool isWaitingAfterLeap = false;

    // === 自前回転制御 ===
    [SerializeField] private float turnSpeed = 2f;

    // === 視野角（視界検知） ===
    [Header("視野角設定")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float detectionAngle = 30f;

    private DinosaurAnimationManager animationManager;

    private PlayerBase playerScript;

    //animationランダム再生用
    private bool playedIdleAnimation = false;

    // === 現在の状態 ===
    private State currentState = State.Patrol;
    public enum State
    {
        Patrol,
        Chase,
        Vigilance,
        Roar,
        Leap
    }

    private void Awake()
    {
        animationManager = GetComponent<DinosaurAnimationManager>();
    }

    // 初期化処理
    void Start()
    {
        // NavMeshAgentを取得
        agent = GetComponent<NavMeshAgent>();

        rb = GetComponent<Rigidbody>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        playerScript = FindObjectOfType<PlayerBase>();

        // 恐竜らしい曲がり方を実現するためにNavMeshAgent に回転を任せず、このスクリプトで制御する
        agent.updateRotation = false;

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
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        Debug.Log("Current State: " + currentState);

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
                    SwitchState(State.Roar);  // プレイヤー視認で咆哮へ
                }
                else
                {
                    PlayerBase player = playerTransform.GetComponent<PlayerBase>();

                    if (player != null)
                    {
                        if (distanceToPlayer < vigilanceWalkDistance)
                        {
                            if (player.IsRunningNow)
                            {
                                SwitchState(State.Chase);  // 近距離で走っていたら追跡
                            }
                            else if (player.IsWalkingNow)
                            {
                                SwitchState(State.Vigilance);  // 近距離で歩いていたら警戒
                            }
                        }
                        else if (distanceToPlayer < vigilanceRunDistance && player.IsRunningNow)
                        {
                            SwitchState(State.Vigilance);  // 中距離で走っていたら警戒
                        }
                    }
                }

                PatrolState(); // 巡回処理は常に実行
                break;

            case State.Vigilance:
                if (isPlayerVisible)
                {
                    SwitchState(State.Roar);
                }
                else if (distanceToPlayer >= vigilanceRunDistance)
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

    // Rayでプレイヤーを検知する処理
    private bool DetectPlayerByRay()
    {
        PlayerBase playerBase = playerTransform.GetComponent<PlayerBase>();
        if (playerBase != null && playerBase.IsFounding)
        {
            return false;
        }

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

        SetSpeedForState(newState); // ←★ここで状態に応じた速度を自動設定

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
            agent.ResetPath(); // 停止
            roarTimer = 0f;
        }
        else if (newState == State.Leap)
        {
            leapTimer = 0f;
        }

        agent.enabled = true;
    }

    // 状態に応じた速度設定を一元化
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
                // Roar や Leap は NavMeshAgent を使わないため速度設定しない
        }
    }
    // 巡回中の処理
    void PatrolState()
    {
        // 巡回ポイントが設定されていない場合は処理しない
        if (patrolPoints.Length == 0) return;

        // 停止中の処理（立ち止まってキョロキョロ）
        if (isWaiting)
        {
            idleTimer += Time.deltaTime;

            // 方向転換風の演出（例：左右にゆっくり回転）とりあえず入れてみた
            float rotationSpeed = 30f;
            transform.Rotate(0f, Mathf.Sin(Time.time * 2f) * rotationSpeed * Time.deltaTime, 0f);

            // 停止中にアニメーションを再生（1回だけ再生するようにしたいならフラグが必要）
            if (!playedIdleAnimation)
            {
                if (Random.value < 0.8f)
                {
                    animationManager.PlayIdle();
                }
                else
                {
                    animationManager.PlaySniff();
                }
                playedIdleAnimation = true;
            }

            if (idleTimer >= waitDuration)
            {
                isWaiting = false;
                idleTimer = 0f;
                nextIdleTime = Time.time + Random.Range(10f, 60f);
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);

                AudioManager.Instance.DestroySE("Idle");
                AudioManager.Instance.PlaySELoop("Walk", transform);

                animationManager.PlayWalk();
            }

            return;
        }

        // 巡回中に一定時間経過したら立ち止まる
        if (Time.time >= nextIdleTime)
        {
            isWaiting = true;
            agent.ResetPath(); // 一時停止
            AudioManager.Instance.DestroySE("Walk");
            AudioManager.Instance.PlaySELoop("Idle", transform); // 「フンッ…」みたいな声でも可

            animationManager.PlayIdle();
            return;
        }

        // 巡回動作中の処理
        AudioManager.Instance.DestroySE("Dash");
        AudioManager.Instance.PlaySELoop("Walk", transform);

        animationManager.PlayWalk();

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }


    // 追跡中の処理（transformによる自前移動）
    void ChaseState()
    {
        animationManager.PlayRun();

        if (playerScript != null && playerScript.IsFounding) // ← 修正ポイント
        {
            // ランダムな方向へ移動
            Vector3 randomDirection = Random.insideUnitSphere * 5f;
            randomDirection.y = 0f; // 水平移動のみに制限
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            agent.SetDestination(playerTransform.position);
        }

        AudioManager.Instance.DestroySE("Walk");
        AudioManager.Instance.PlaySELoop("Dash", transform);
    }

    void SetRandomVigilanceTarget()
    {
        // 警戒距離の範囲内でランダムな方向と距離を決める
        Vector2 randomCircle = Random.insideUnitCircle * vigilanceRunDistance;
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
        animationManager.PlayWalk();
        // 目的地に近づいたら新しい警戒ポイントを設定
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
            animationManager.PlayRoar();
            hasRoared = true;
        }

        if (roarTimer >= roarDuration)
        {
            hasRoared = false; // 次回Roarのためにリセット
            agent.isStopped = false;
            SwitchState(State.Chase);
        }
    }

    void LeapState()
    {
        // 1. 溜め時間の進行
        if (!isWaitingAfterLeap)
        {
            chargeTimer += Time.deltaTime;

            if (chargeTimer < chargeDuration)
            {
                // 溜め期間中は動かさない
                animationManager.PlayWalk();
                return;
            }

            // 2. 飛びつき方向の決定（1回だけ）
            if (leapTimer == 0f)
            {
                leapDirection = (playerTransform.position - transform.position).normalized;
                leapDirection.y = 0f;
            }

            // 3. 飛びつき移動
            animationManager.PlayLeap();
            leapTimer += Time.deltaTime;
            transform.position += leapDirection * leapSpeed * Time.deltaTime;

            // 4. 飛び終わったら待機状態へ
            if (leapTimer >= leapDuration)
            {
                isWaitingAfterLeap = true;
                postLeapWaitTimer = 0f;
            }
        }
        else
        {
            animationManager.PlayWalk();
            // 5. 飛び終わり後の1秒待機処理
            postLeapWaitTimer += Time.deltaTime;

            if (postLeapWaitTimer >= 1f)
            {
                // 6. タイマーリセットしてChaseへ
                chargeTimer = 0f;
                leapTimer = 0f;
                isWaitingAfterLeap = false;
                SwitchState(State.Chase);
            }
        }
    }

    public bool IsFoundingPlayer()
    {
        return playerScript != null && playerScript.IsFounding;
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
