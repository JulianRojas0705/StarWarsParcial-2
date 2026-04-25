using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using TMPro;

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
    [Tooltip("Duración total de la batalla en segundos")]
    public float battleDuration = 10f;

    [Tooltip("Segundos entre cada ataque")]
    public float attackInterval = 2.0f;

    [Header("Panel de victoria")]
    public GameObject victoryPanel;
    public TMP_Text victoryText;

    // Triggers
    private const string TRIGGER_ATTACK = "Attack";
    private const string TRIGGER_VICTORY = "Victory";
    private const string TRIGGER_DEFEAT = "Defeat";
    private const string STATE_IDLE = "Breathing Idle";

    private bool isBattleActive = false;
    private Coroutine battleCoroutine;

    // ─── INIT ────────────────────────────────────────────────────────────────

    void Start()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        foreach (var f in fighters)
        {
            if (f.target != null)
            {
                var captured = f;
                f.target.OnTargetStatusChanged += (behaviour, status) =>
                {
                    captured.isTracked = (status.Status == Status.TRACKED ||
                                          status.Status == Status.EXTENDED_TRACKED);
                    EvaluateBattleState();
                };
            }
        }
    }

    // ─── LÓGICA PRINCIPAL ────────────────────────────────────────────────────

    private void EvaluateBattleState()
    {
        List<Fighter> visibles = GetTrackedFighters();

        if (visibles.Count >= 2 && !isBattleActive)
            StartBattle(visibles);
        else if (visibles.Count < 2 && isBattleActive)
            StopBattle();
    }

    private List<Fighter> GetTrackedFighters()
    {
        List<Fighter> list = new List<Fighter>();
        foreach (var f in fighters)
            if (f.isTracked) list.Add(f);
        return list;
    }

    // ─── BATALLA ─────────────────────────────────────────────────────────────

    private void StartBattle(List<Fighter> visibles)
    {
        isBattleActive = true;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        foreach (var f in fighters)
            ResetToIdle(f);

        if (battleCoroutine != null)
            StopCoroutine(battleCoroutine);

        battleCoroutine = StartCoroutine(BattleSequence(visibles[0], visibles[1]));
        Debug.Log($"¡BATALLA! {visibles[0].name} vs {visibles[1].name} — {battleDuration}s");
    }

    private void StopBattle()
    {
        isBattleActive = false;

        if (battleCoroutine != null)
        {
            StopCoroutine(battleCoroutine);
            battleCoroutine = null;
        }

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        foreach (var f in fighters)
            ResetToIdle(f);

        Debug.Log("Batalla detenida.");
    }

    private IEnumerator BattleSequence(Fighter f1, Fighter f2)
    {
        yield return new WaitForSeconds(1.0f);

        float elapsed = 0f;

        while (elapsed < battleDuration && isBattleActive)
        {
            TriggerAnim(f1, TRIGGER_ATTACK);
            yield return new WaitForSeconds(attackInterval * 0.5f);
            elapsed += attackInterval * 0.5f;

            if (!isBattleActive) yield break;

            TriggerAnim(f2, TRIGGER_ATTACK);
            yield return new WaitForSeconds(attackInterval * 0.5f);
            elapsed += attackInterval * 0.5f;
        }

        if (isBattleActive)
            DecideWinner(f1, f2);
    }

    private void DecideWinner(Fighter f1, Fighter f2)
    {
        bool f1Wins = Random.value > 0.5f;
        Fighter winner = f1Wins ? f1 : f2;
        Fighter loser = f1Wins ? f2 : f1;

        TriggerAnim(winner, TRIGGER_VICTORY);
        TriggerAnim(loser, TRIGGER_DEFEAT);

        isBattleActive = false;
        ShowVictoryPanel(winner.name, loser.name);
        StartCoroutine(HidePanelAfterSeconds(5f));

        Debug.Log($"¡{winner.name} GANA!");
    }

    // ─── PANEL DE VICTORIA ────────────────────────────────────────────────────

    private void ShowVictoryPanel(string winnerName, string loserName)
    {
        if (victoryPanel == null) return;
        victoryPanel.SetActive(true);

        if (victoryText != null)
            victoryText.text = $"⚔ VICTORIA ⚔\n\n{winnerName}\nvence a\n{loserName}";
    }

    private IEnumerator HidePanelAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
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
    }

    private void ResetToIdle(Fighter f)
    {
        if (f.animator == null) return;
        f.animator.ResetTrigger(TRIGGER_ATTACK);
        f.animator.ResetTrigger(TRIGGER_VICTORY);
        f.animator.ResetTrigger(TRIGGER_DEFEAT);
        f.animator.Play(STATE_IDLE);
    }
}