using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 恐竜の基本AIを制御するスクリプト（巡回・警戒・追跡）
public class Dinosaur_Base : MonoBehaviour
{
    // === 必須コンポーネント ===
    private NavMeshAgent agent; //移動経路(NavMesh)を使って目的地まで歩かせるために使用
    private Rigidbody rb; //物理挙動を扱うために使用

    // === 共通オブジェクト参照 ===
    [SerializeField] private Transform playerTransform; //プレイヤーの位置を取得
    [SerializeField] private Transform modelTransform; //恐竜の見た目を進行咆哮に向かせるために使用

    // === Patrolで使っている変数 ===
    [Header("Patrol 設定")]
    [SerializeField] private Transform[] patrolPoints; //巡回ポイント
    [SerializeField] private float patrolSpeed = 2f; //巡回スピード
    private int patrolDest = 0; //現在の目的地

    private float idleTimer = 0f;
    private float idleDuration = 4f;
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

    // === ロッカーに入ったところを見られているかどうか ===
    private bool isLooked = false;
    public bool IsLooked => isLooked;

    private bool previousIsLooked = false; // 追加：前回のisLooked状態を記録

    private DinosaurAnimationManager animationManager;

    private PlayerBase playerScript;

    //animationランダム再生用
    private bool playedIdleAnimation = false;

    // === 見ている恐竜の一元化 ===
    public static List<Dinosaur_Base> dinosLookingAtPlayer = new List<Dinosaur_Base>();

    //private bool hasPlayedChaseBGM = false;

    private string currentSE = ""; // 現在再生中のSE名

    [SerializeField] private WarningUIManager warningUIManager;

    // === 現在の状態 ===
    private State currentState = State.Patrol;

    public enum State
    {
        Patrol,
        Idle,
        Vigilance,
        Chase,
        Roar,
        // Leap
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
        if (patrolPoints.Length > 0 && patrolPoints[patrolDest] != null)
        {
            agent.SetDestination(patrolPoints[patrolDest].position);
        }
        else
        {
            Debug.LogError("patrolPoints[" + patrolDest + "] が null です！");
        }
    }

    // 毎フレーム実行される処理
    void FixedUpdate()
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

        isLooked = DetectPlayerByRay();

        if (isLooked != previousIsLooked)
        {
            Debug.Log($"[Look状態変化] isLooked が {previousIsLooked} → {isLooked} に変化しました");

            if (isLooked)
            {
                if (!dinosLookingAtPlayer.Contains(this))
                {
                    dinosLookingAtPlayer.Add(this);
                    Debug.Log($"{name} がプレイヤーを見つけてリストに追加されました");
                }
            }
            else
            {
                if (dinosLookingAtPlayer.Contains(this))
                {
                    dinosLookingAtPlayer.Remove(this);
                    Debug.Log($"{name} がプレイヤーを見失ってリストから削除されました");
                }
            }

            previousIsLooked = isLooked;
        }


        // 例: Update()の視認判定部分
        if (isLooked)
        {
            isPlayerVisible = true;
            timeSinceLastSeen = 0f;
            //Debug.Log("Player detected by ray. timeSinceLastSeen reset to 0");
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
            //Debug.Log($"Player NOT detected. timeSinceLastSeen = {timeSinceLastSeen:F2} seconds");

            if (timeSinceLastSeen > loseSightDuration)
            {
                if (isPlayerVisible)
                {
                    Debug.Log("Lost sight of player. Setting isPlayerVisible to false.");
                    isPlayerVisible = false;
                }

            }
        }


        switch (currentState)
        {
            case State.Patrol:
                if (isPlayerVisible)
                {
                    SwitchState(State.Roar);  // 見えたら咆哮
                }
                else
                {
                    PlayerBase player = playerTransform.GetComponent<PlayerBase>();

                    if (player != null && !player.IsFounding) // ← ロッカー中は遷移しない
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

                PatrolState();
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
                if (timeSinceLastSeen > loseSightDuration)
                {
                    SwitchState(State.Patrol);
                }
                ChaseState();
                break;

            //case State.Leap:
                // LeapState(); ← コメントアウト
              //  break;
        }
    }

    private void UpdateFootstepSE(string newSE)
    {
        if (AudioManager.Instance == null) return;

        if (currentSE != newSE)
        {
            if (!string.IsNullOrEmpty(currentSE))
            {
                AudioManager.Instance.DestroySE(currentSE,transform);
            }

            if (!string.IsNullOrEmpty(newSE))
            {
                AudioManager.Instance.PlaySELoop(newSE, transform);
            }

            currentSE = newSE;
        }
    }


    // Rayでプレイヤーを検知する処理
    private bool DetectPlayerByRay()
    {
        PlayerBase playerBase = playerTransform.GetComponent<PlayerBase>();
        //if (playerBase != null && playerBase.IsFounding)
        //{
            //return false;
        //}

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
        // BGM停止条件
        if (currentState == State.Chase && newState != State.Chase)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
                Debug.Log("チェイスBGM停止");
            }
        }

        currentState = newState;
        SetSpeedForState(newState);

        if (warningUIManager != null)
        {
            if (newState == State.Chase || newState == State.Roar)
                warningUIManager.ShowWarning();
            else
                warningUIManager.HideWarning();
        }

        switch (newState)
        {
            case State.Patrol:
                if (patrolPoints.Length > 0)
                    agent.SetDestination(patrolPoints[patrolDest].position);
                break;

            case State.Idle:
                agent.ResetPath(); // 移動停止
                idleTimer = 0f;
                playedIdleAnimation = false;
                UpdateFootstepSE("Idle");
                break;

            case State.Vigilance:
                agent.SetDestination(playerTransform.position);
                break;

            case State.Roar:
                agent.ResetPath();
                roarTimer = 0f;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBGM("ChaseBGM");
                    Debug.Log("チェイスBGM開始");
                }
                break;
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
        if (patrolPoints.Length == 0) return;

        UpdateFootstepSE(AudioDefine.Walk);
        animationManager?.PlayWalk();

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            patrolDest = (patrolDest + 1) % patrolPoints.Length;

            // ランダムにIdleへ遷移するか判定
            if (Time.time >= nextIdleTime)
            {
                nextIdleTime = Time.time + Random.Range(10f, 60f);
                SwitchState(State.Idle);
                return;
            }

            agent.SetDestination(patrolPoints[patrolDest].position);
        }
    }

    void IdleState()
    {
        idleTimer += Time.deltaTime;

        // 首振り演出
        float rotationSpeed = 30f;
        transform.Rotate(0f, Mathf.Sin(Time.time * 2f) * rotationSpeed * Time.deltaTime, 0f);

        // 最初に一度だけIdleアニメを再生
        if (!playedIdleAnimation)
        {
            if (Random.value < 0.8f && animationManager != null)
                animationManager.PlayIdle();
            else if (animationManager != null)
                animationManager.PlaySniff();

            playedIdleAnimation = true;
        }

        // Idle時間が終わったらPatrolへ戻る
        if (idleTimer >= idleDuration)
        {
            SwitchState(State.Patrol);
        }
    }

    void ChaseState()
    {
        agent.isStopped = false; // ← 追加
        animationManager?.PlayRun();
        agent.SetDestination(playerTransform.position);
        UpdateFootstepSE(AudioDefine.Dash);
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
        if (animationManager != null)
        {
            animationManager.PlayWalk(); // ← または他のアニメーション呼び出し
        }

        // 目的地に近づいたら新しい警戒ポイントを設定
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            SetRandomVigilanceTarget();
        }
    }

    void RoarState()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.DestroySE("Dash", transform);
            AudioManager.Instance.DestroySE("Walk", transform);
        }
        roarTimer += Time.deltaTime;

        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        if (!hasRoared)
        {
            animationManager?.PlayRoar();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE("Rouring", transform.position);
            }

            hasRoared = true;
        }


        if (roarTimer >= roarDuration)
        {
            hasRoared = false; // 次回Roarのためにリセット
            // Roar 終了と同時に Chase 開始
            agent.isStopped = false;               // ← ここで解除
            SwitchState(State.Chase);             // すぐ Chase に切り替え
        }
    }
    /*
void LeapState()
{
    // 1. 溜め時間の進行
    if (!isWaitingAfterLeap)
    {
        chargeTimer += Time.deltaTime;

        if (chargeTimer < chargeDuration && animationManager != null)
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
        if (animationManager != null)
        {
            animationManager.PlayLeap(); // ← または他のアニメーション呼び出し
        }

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
        if (animationManager != null)
        {
            animationManager.PlayWalk(); // ← または他のアニメーション呼び出し
        }

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
*/


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
