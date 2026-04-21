using System.Collections;
using UnityEngine;
using Vuforia;

public class BattleManager : MonoBehaviour
{
    [Header("Image Targets")]
    [Tooltip("Arrastra aquí el GameObject del primer Image Target")]
    public ObserverBehaviour fighter1Target;

    [Tooltip("Arrastra aquí el GameObject del segundo Image Target")]
    public ObserverBehaviour fighter2Target;

    [Header("Animadores de los personajes 3D")]
    [Tooltip("Animator del personaje hijo del Fighter 1")]
    public Animator fighter1Animator;

    [Tooltip("Animator del personaje hijo del Fighter 2")]
    public Animator fighter2Animator;

    [Header("Nombres exactos de los estados en el Animator")]
    [Tooltip("Nombre exacto del estado Idle en el Animator Controller")]
    public string idleStateName = "Breathing Idle";

    [Header("Configuración de animaciones")]
    [Tooltip("Nombre del trigger de ataque Fighter 1")]
    public string fighter1AttackTrigger = "Attack";

    [Tooltip("Nombre del trigger de ataque Fighter 2")]
    public string fighter2AttackTrigger = "Attack";

    [Tooltip("Nombre del trigger de victoria")]
    public string victoryTrigger = "Victory";

    [Tooltip("Nombre del trigger de derrota")]
    public string defeatTrigger = "Defeat";

    [Header("Configuración de pelea")]
    [Tooltip("Tiempo en segundos entre cada intercambio de golpes")]
    public float attackInterval = 2.0f;

    [Tooltip("Número de rondas antes de decidir un ganador")]
    public int totalRounds = 3;

    // Estado interno
    private bool isBattleActive = false;
    private bool fighter1Tracked = false;
    private bool fighter2Tracked = false;
    private int currentRound = 0;
    private Coroutine battleCoroutine;

    void Start()
    {
        if (fighter1Target != null)
            fighter1Target.OnTargetStatusChanged += OnFighter1StatusChanged;
        else
            Debug.LogError("BattleManager: fighter1Target no está asignado.");

        if (fighter2Target != null)
            fighter2Target.OnTargetStatusChanged += OnFighter2StatusChanged;
        else
            Debug.LogError("BattleManager: fighter2Target no está asignado.");
    }

    void OnDestroy()
    {
        if (fighter1Target != null)
            fighter1Target.OnTargetStatusChanged -= OnFighter1StatusChanged;

        if (fighter2Target != null)
            fighter2Target.OnTargetStatusChanged -= OnFighter2StatusChanged;
    }

    // ─── Callbacks de Vuforia ────────────────────────────────────────────────

    private void OnFighter1StatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        fighter1Tracked = (status.Status == Status.TRACKED ||
                           status.Status == Status.EXTENDED_TRACKED);
        Debug.Log($"Fighter 1 tracking: {fighter1Tracked}");
        EvaluateBattleState();
    }

    private void OnFighter2StatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        fighter2Tracked = (status.Status == Status.TRACKED ||
                           status.Status == Status.EXTENDED_TRACKED);
        Debug.Log($"Fighter 2 tracking: {fighter2Tracked}");
        EvaluateBattleState();
    }

    // ─── Lógica principal ────────────────────────────────────────────────────

    private void EvaluateBattleState()
    {
        bool bothTracked = fighter1Tracked && fighter2Tracked;

        if (bothTracked && !isBattleActive)
            StartBattle();
        else if (!bothTracked && isBattleActive)
            StopBattle();
    }

    private void StartBattle()
    {
        isBattleActive = true;
        currentRound = 0;
        Debug.Log("¡BATALLA INICIADA!");

        ResetAnimators();

        if (battleCoroutine != null)
            StopCoroutine(battleCoroutine);

        battleCoroutine = StartCoroutine(BattleSequence());
    }

    private void StopBattle()
    {
        isBattleActive = false;
        Debug.Log("Batalla detenida.");

        if (battleCoroutine != null)
        {
            StopCoroutine(battleCoroutine);
            battleCoroutine = null;
        }

        ResetAnimators();
    }

    // ─── Secuencia de pelea ──────────────────────────────────────────────────

    private IEnumerator BattleSequence()
    {
        yield return new WaitForSeconds(1.0f);

        while (isBattleActive && currentRound < totalRounds)
        {
            currentRound++;
            Debug.Log($"Ronda {currentRound} de {totalRounds}");

            // Fighter 1 ataca
            TriggerAttack(fighter1Animator, fighter1AttackTrigger, "Fighter 1");
            yield return new WaitForSeconds(attackInterval * 0.5f);

            // Fighter 2 contraataca
            TriggerAttack(fighter2Animator, fighter2AttackTrigger, "Fighter 2");
            yield return new WaitForSeconds(attackInterval * 0.5f);
        }

        if (isBattleActive)
        {
            yield return new WaitForSeconds(0.5f);
            DecideWinner();
        }
    }

    private void TriggerAttack(Animator animator, string trigger, string fighterName)
    {
        if (animator == null) return;

        animator.ResetTrigger(victoryTrigger);
        animator.ResetTrigger(defeatTrigger);
        animator.SetTrigger(trigger);

        Debug.Log($"{fighterName} ataca!");
    }

    private void DecideWinner()
    {
        bool fighter1Wins = Random.value > 0.5f;

        if (fighter1Wins)
        {
            fighter1Animator?.SetTrigger(victoryTrigger);
            fighter2Animator?.SetTrigger(defeatTrigger);
            Debug.Log("¡Fighter 1 gana!");
        }
        else
        {
            fighter2Animator?.SetTrigger(victoryTrigger);
            fighter1Animator?.SetTrigger(defeatTrigger);
            Debug.Log("¡Fighter 2 gana!");
        }

        isBattleActive = false;

        // Volver al Idle después de 3 segundos
        StartCoroutine(ReturnToIdleAfterVictory());
    }

    private IEnumerator ReturnToIdleAfterVictory()
    {
        yield return new WaitForSeconds(3.0f);
        ResetAnimators();
    }

    // ─── Reset ───────────────────────────────────────────────────────────────

    private void ResetAnimators()
    {
        if (fighter1Animator != null)
        {
            fighter1Animator.ResetTrigger(fighter1AttackTrigger);
            fighter1Animator.ResetTrigger(victoryTrigger);
            fighter1Animator.ResetTrigger(defeatTrigger);
            fighter1Animator.Play(idleStateName);
        }

        if (fighter2Animator != null)
        {
            fighter2Animator.ResetTrigger(fighter2AttackTrigger);
            fighter2Animator.ResetTrigger(victoryTrigger);
            fighter2Animator.ResetTrigger(defeatTrigger);
            fighter2Animator.Play(idleStateName);
        }
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    public void ManualAttackFighter1()
    {
        if (fighter1Animator != null)
            TriggerAttack(fighter1Animator, fighter1AttackTrigger, "Fighter 1 (manual)");
    }

    public void ManualAttackFighter2()
    {
        if (fighter2Animator != null)
            TriggerAttack(fighter2Animator, fighter2AttackTrigger, "Fighter 2 (manual)");
    }
}