using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class BattleManager : MonoBehaviour
{
    [System.Serializable]
    public class Fighter
    {
        public string name;
        public ObserverBehaviour target;
        public Animator animator;
        [HideInInspector] public bool isTracked = false;
    }

    [Header("Configura los 4 personajes")]
    public Fighter[] fighters = new Fighter[4];

    [Header("Configuración de batalla")]
    [Tooltip("Segundos entre cada ataque")]
    public float attackInterval = 2.0f;

    [Tooltip("Número de rondas antes de decidir ganador")]
    public int totalRounds = 3;

    // Triggers — iguales en todos los Animators
    private const string TRIGGER_ATTACK = "Attack";
    private const string TRIGGER_VICTORY = "Victory";
    private const string TRIGGER_DEFEAT = "Defeat";
    private const string TRIGGER_IDLE = "Idle";
    private const string STATE_IDLE = "Breathing Idle";

    private bool isBattleActive = false;
    private Coroutine battleCoroutine;

    // ─── INIT ────────────────────────────────────────────────────────────────

    void Start()
    {
        foreach (var f in fighters)
        {
            if (f.target != null)
            {
                var captured = f; // closure
                f.target.OnTargetStatusChanged += (behaviour, status) =>
                {
                    captured.isTracked = (status.Status == Status.TRACKED ||
                                          status.Status == Status.EXTENDED_TRACKED);
                    Debug.Log($"{captured.name} tracked: {captured.isTracked}");
                    EvaluateBattleState();
                };
            }
            else
            {
                Debug.LogWarning($"BattleManager: target de '{f.name}' no está asignado.");
            }
        }
    }

    // ─── LÓGICA PRINCIPAL ────────────────────────────────────────────────────

    private void EvaluateBattleState()
    {
        List<Fighter> visibles = GetTrackedFighters();

        if (visibles.Count >= 2 && !isBattleActive)
        {
            StartBattle(visibles);
        }
        else if (visibles.Count < 2 && isBattleActive)
        {
            StopBattle();
        }
    }

    private List<Fighter> GetTrackedFighters()
    {
        List<Fighter> list = new List<Fighter>();
        foreach (var f in fighters) 
            if (f.isTracked) list.Add(f);
        return list;
    }

    // ─── BATALLA ────────────────────────────────────────────────────────────

    private void StartBattle(List<Fighter> visibles)
    {
        isBattleActive = true;
        Debug.Log($"¡BATALLA INICIADA! {visibles[0].name} vs {visibles[1].name}");

        // Resetear todos a idle
        foreach (var f in fighters)
            ResetToIdle(f);

        if (battleCoroutine != null)
            StopCoroutine(battleCoroutine);

        // Solo pelean los 2 primeros visibles
        battleCoroutine = StartCoroutine(BattleSequence(visibles[0], visibles[1]));
    }

    private void StopBattle()
    {
        isBattleActive = false;
        Debug.Log("Batalla detenida — menos de 2 tarjetas visibles.");

        if (battleCoroutine != null)
        {
            StopCoroutine(battleCoroutine);
            battleCoroutine = null;
        }

        foreach (var f in fighters)
            ResetToIdle(f);
    }

    private IEnumerator BattleSequence(Fighter f1, Fighter f2)
    {
        yield return new WaitForSeconds(1.0f);

        for (int round = 1; round <= totalRounds; round++)
        {
            if (!isBattleActive) yield break;

            Debug.Log($"Ronda {round}/{totalRounds}");

            // F1 ataca
            TriggerAnim(f1, TRIGGER_ATTACK);
            yield return new WaitForSeconds(attackInterval * 0.5f);

            // F2 contraataca
            TriggerAnim(f2, TRIGGER_ATTACK);
            yield return new WaitForSeconds(attackInterval * 0.5f);
        }

        if (isBattleActive)
        {
            yield return new WaitForSeconds(0.5f);
            DecideWinner(f1, f2);
        }
    }

    private void DecideWinner(Fighter f1, Fighter f2)
    {
        bool f1Wins = Random.value > 0.5f;
        Fighter winner = f1Wins ? f1 : f2;
        Fighter loser = f1Wins ? f2 : f1;

        TriggerAnim(winner, TRIGGER_VICTORY);
        TriggerAnim(loser, TRIGGER_DEFEAT);

        Debug.Log($"¡{winner.name} GANA!");

        isBattleActive = false;
        StartCoroutine(ReturnToIdleAfterBattle());
    }

    private IEnumerator ReturnToIdleAfterBattle()
    {
        yield return new WaitForSeconds(3.0f);
        foreach (var f in fighters)
            ResetToIdle(f);
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private void TriggerAnim(Fighter f, string trigger)
    {
        if (f.animator == null) return;
        f.animator.ResetTrigger(TRIGGER_ATTACK);
        f.animator.ResetTrigger(TRIGGER_VICTORY);
        f.animator.ResetTrigger(TRIGGER_DEFEAT);
        f.animator.SetTrigger(trigger);
        Debug.Log($"{f.name} → {trigger}");
    }

    private void ResetToIdle(Fighter f)
    {
        if (f.animator == null) return;
        f.animator.ResetTrigger(TRIGGER_ATTACK);
        f.animator.ResetTrigger(TRIGGER_VICTORY);
        f.animator.ResetTrigger(TRIGGER_DEFEAT);
        f.animator.ResetTrigger(TRIGGER_IDLE);
        f.animator.Play(STATE_IDLE);
    }
}