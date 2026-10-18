using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 恐竜AI本体。Patrol / Idle / Vigilance / Roar / Chase のステートマシン。
/// 状態遷移は EvaluateTransitions に集約し、各状態の挙動は *State() メソッドに分けている。
/// 連携: PlayerBase(プレイヤーの状態), DinosaurAnimationManager, AudioManager, WarningUIManager
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Dinosaur_Base : MonoBehaviour
{
    // === 必須コンポーネント ===
    private NavMeshAgent agent;
    private DinosaurAnimationManager animationManager;

    // === 参照 ===
    private Transform playerTransform;
    private PlayerBase playerScript;
    [SerializeField] private Transform modelTransform; // 見た目を進行方向へ向けるために使用
    [SerializeField] private WarningUIManager warningUIManager; // 視野狭窄UI

    // === Patrol(巡回) ===
    [Header("Patrol 設定")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    private int patrolDest = 0;

    // === Idle ===
    private float idleTimer = 0f;
    private float idleDuration = 4f;
    private float nextIdleTime = 0f;
    private bool playedIdleAnimation = false;

    // === Vigilance(警戒) ===
    [Header("Vigilance 設定")]
    [SerializeField] private float vigilanceWalkDistance = 20f;
    [SerializeField] private float vigilanceRunDistance = 30f;
    [SerializeField] private float vigilanceSpeed = 2f;

    // === Chase(追跡) ===
    [Header("Chase 設定")]
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float lostSightDelay = 2.0f;
    private bool isPlayerVisible = false;
    private float lostSightTimer = 0f;

    // === Roar(咆哮) ===
    private float roarTimer = 0f;
    private float roarDuration = 2.5f;
    private bool hasRoared = false;

    // === 回転・視野 ===
    [SerializeField] private float turnSpeed = 2f;

    [Header("視野角設定")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float detectionAngle = 30f;

    // === ロッカーに入ったところを見られているかどうか ===
    private bool isLooked = false;
    public bool IsLooked => isLooked;

    // === 見ている恐竜の一元化 ===
    public static List<Dinosaur_Base> dinosLookingAtPlayer = new List<Dinosaur_Base>();

    private string currentSE = ""; // 現在再生中の足音SE名

    // === 現在の状態 ===
    private State currentState = State.Patrol;

    public enum State
    {
        Patrol,
        Idle,
        Vigilance,
        Chase,
        Roar,
    }

    private void Awake()
    {
        animationManager = GetComponent<DinosaurAnimationManager>();
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        playerScript = FindObjectOfType<PlayerBase>();

        if (playerTransform == null || playerScript == null || modelTransform == null)
        {
            enabled = false;
            return;
        }

        // 恐竜らしい曲がり方にするため、回転はNavMeshAgentに任せず FixedUpdate で制御する
        agent.updateRotation = false;

        if (patrolPoints.Length > 0 && patrolPoints[patrolDest] != null)
        {
            agent.SetDestination(patrolPoints[patrolDest].position);
        }
    }

    // 状態遷移の判定はここに集約する（各状態の挙動側では遷移させない）
    State EvaluateTransitions(State current)
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (current)
        {
            case State.Patrol:
                if (isPlayerVisible) return State.Roar;
                if (RandomIdleTriggered()) return State.Idle;
                if (!playerScript.IsFounding)
                {
                    if (distanceToPlayer < vigilanceWalkDistance)
                    {
                        if (playerScript.IsRunningNow) return State.Chase;
                        if (playerScript.IsWalkingNow) return State.Vigilance;
                    }
                    else if (distanceToPlayer < vigilanceRunDistance && playerScript.IsRunningNow)
                    {
                        return State.Vigilance;
                    }
                }
                break;

            case State.Idle:
                if (isPlayerVisible) return State.Roar;
                if (idleTimer >= idleDuration) return State.Patrol;
                break;

            case State.Vigilance:
                if (isPlayerVisible) return State.Roar;
                if (distanceToPlayer >= vigilanceRunDistance) return State.Patrol;
                break;

            case State.Roar:
                if (roarTimer >= roarDuration) return State.Chase;
                break;

            case State.Chase:
                // 一瞬見失っただけでは追跡をやめず、lostSightDelay 秒見えなかったら巡回に戻る
                if (!isPlayerVisible)
                {
                    lostSightTimer += Time.deltaTime;
                    if (lostSightTimer >= lostSightDelay)
                    {
                        return State.Patrol;
                    }
                }
                else
                {
                    lostSightTimer = 0f;
                }
                break;
        }

        return current;
    }

    // 処理順: 向き更新 → 視界判定 → 状態遷移 → 各状態の挙動
    void FixedUpdate()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 direction = agent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // モデルの正面が -Z のため、180度補正をかけている
            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation,
                targetRotation * Quaternion.Euler(0f, 180f, 0f),
                turnSpeed * Time.deltaTime
            );
        }

        isPlayerVisible = DetectPlayerByRay();

        State next = EvaluateTransitions(currentState);
        if (next != currentState)
        {
            SwitchState(next);
        }

        switch (currentState)
        {
            case State.Patrol: PatrolState(); break;
            case State.Idle: IdleState(); break;
            case State.Vigilance: VigilanceState(); break;
            case State.Roar: RoarState(); break;
            case State.Chase: ChaseState(); break;
        }
    }

    void SwitchState(State newState)
    {
        // Chaseを抜けるときは追跡BGMと警告UIを止める
        if (currentState == State.Chase && newState != State.Chase)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
            }
            if (warningUIManager != null)
            {
                warningUIManager.HideWarning();
            }
        }

        // 現在の状態のリセット
        switch (currentState)
        {
            case State.Idle:
                idleTimer = 0f;
                playedIdleAnimation = false;
                break;

            case State.Roar:
                roarTimer = 0f;
                hasRoared = false;
                break;
        }

        currentState = newState;

        // 新しい状態の初期処理
        switch (newState)
        {
            case State.Patrol:
                SetSpeedForState(State.Patrol);
                if (patrolPoints.Length > 0)
                {
                    agent.isStopped = false;
                    agent.SetDestination(patrolPoints[patrolDest].position);
                }
                break;

            case State.Idle:
                idleTimer = 0f;
                playedIdleAnimation = false;
                agent.isStopped = true;
                break;

            case State.Vigilance:
                SetSpeedForState(State.Vigilance);
                agent.isStopped = false;
                break;

            case State.Roar:
                roarTimer = 0f;
                hasRoared = false;
                agent.isStopped = true;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBGM("ChaseBGM");
                }
                if (warningUIManager != null)
                {
                    warningUIManager.ShowWarning();
                }
                break;

            case State.Chase:
                SetSpeedForState(State.Chase);
                agent.isStopped = false;
                lostSightTimer = 0f;
                break;
        }
    }

    // 足音SEの切り替え。同じSEが鳴っている間は再生し直さない
    private void UpdateFootstepSE(string newSE)
    {
        if (AudioManager.Instance == null) return;

        // IdleやRoar中は足音を鳴らさない
        if (currentState == State.Idle || currentState == State.Roar)
        {
            if (!string.IsNullOrEmpty(currentSE))
            {
                AudioManager.Instance.DestroySE(currentSE, transform);
                currentSE = "";
            }
            return;
        }

        if (currentSE != newSE)
        {
            if (!string.IsNullOrEmpty(currentSE))
            {
                AudioManager.Instance.DestroySE(currentSE, transform);
            }

            if (!string.IsNullOrEmpty(newSE))
            {
                AudioManager.Instance.PlaySELoop(newSE, transform);
            }

            currentSE = newSE;
        }
    }

    // プレイヤーが隠れている場合の可視判定
    private bool CanSeePlayer()
    {
        if (playerScript == null) return false;

        // 隠れていなければ見える
        if (!playerScript.IsFounding) return true;

        // 隠れていても、入るところを見られていたら追跡を継続する
        if (playerScript.LookedHiding) return true;

        return false;
    }

    // 視野角チェック → Rayで遮蔽物チェック → 隠れ状態チェック の順で判定する
    private bool DetectPlayerByRay()
    {
        if (playerTransform == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f; // 目線の高さ
        Vector3 toPlayer = playerTransform.position - origin;
        toPlayer.y = 0f; // 高低差は無視して水平方向のみで判定

        Vector3 direction = toPlayer.normalized;

        // モデルの正面が -Z のため、transform.forward の逆を正面として扱う
        float angleToPlayer = Vector3.Angle(-transform.forward, direction);
        if (angleToPlayer > detectionAngle) return false;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == playerTransform)
            {
                return CanSeePlayer();
            }
        }

        return false;
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
        }
    }

    // 10?60秒おきにtrueを返す（呼ぶたびに次回時刻を更新する）
    bool RandomIdleTriggered()
    {
        if (Time.time >= nextIdleTime)
        {
            nextIdleTime = Time.time + Random.Range(10f, 60f);
            return true;
        }
        return false;
    }

    void PatrolState()
    {
        if (patrolPoints.Length == 0) return;

        UpdateFootstepSE(AudioDefine.Walk);
        animationManager?.PlayWalk();

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            patrolDest = (patrolDest + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolDest].position);
        }
    }

    void IdleState()
    {
        idleTimer += Time.deltaTime;

        // 首振り演出
        float rotationSpeed = 30f;
        transform.Rotate(0f, Mathf.Sin(Time.time * 2f) * rotationSpeed * Time.deltaTime, 0f);

        // Idleアニメは入場時に一度だけ再生（80%で通常、20%で匂いを嗅ぐ）
        if (!playedIdleAnimation)
        {
            if (Random.value < 0.8f && animationManager != null)
                animationManager.PlayIdle();
            else if (animationManager != null)
                animationManager.PlaySniff();

            playedIdleAnimation = true;
        }
    }

    // 警戒中はゆっくりプレイヤーへ近づく
    private void VigilanceState()
    {
        if (animationManager != null)
        {
            animationManager.PlayWalk();
        }

        if (playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }

    void RoarState()
    {
        roarTimer += Time.deltaTime; // Chaseへの遷移条件(roarDuration)に使う
        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        if (!hasRoared)
        {
            animationManager?.PlayRoar();
            AudioManager.Instance?.PlaySE("Rouring", transform.position);
            hasRoared = true;
        }
    }

    void ChaseState()
    {
        agent.isStopped = false;
        animationManager?.PlayRun();
        agent.SetDestination(playerTransform.position);
        UpdateFootstepSE(AudioDefine.Dash);
    }

    public bool IsFoundingPlayer()
    {
        return playerScript != null && playerScript.IsFounding;
    }

    // 視野範囲をシーンビューに描画(エディタ専用)
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, detectionRange);

        Vector3 front = -transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -detectionAngle, 0) * front;
        Vector3 rightDir = Quaternion.Euler(0, detectionAngle, 0) * front;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, leftDir * detectionRange);
        Gizmos.DrawRay(origin, rightDir * detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, front * detectionRange);
    }
}