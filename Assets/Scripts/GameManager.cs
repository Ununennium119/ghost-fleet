using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    private enum Phase {
        Placement1,
        Placement2,
        Attack1,
        Attack2,
        GameOver,
    }

    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Button nextPhaseButton;
    [SerializeField] private Button gameOverButton;

    private Phase _currentPhase = Phase.Placement1;

    private void Awake() {
        nextPhaseButton.onClick.AddListener(NextPhase);
        gameOverButton.onClick.AddListener(GameOver);

        UpdatePhaseText();
    }

    private void NextPhase() {
        _currentPhase = _currentPhase switch {
            Phase.Placement1 => Phase.Placement2,
            Phase.Placement2 => Phase.Attack1,
            Phase.Attack1 => Phase.Attack2,
            Phase.Attack2 => Phase.Attack1,
            Phase.GameOver => Phase.GameOver,
            _ => throw new ArgumentOutOfRangeException()
        };
        UpdatePhaseText();
    }

    private void GameOver() {
        _currentPhase = Phase.GameOver;
        UpdatePhaseText();
    }

    private void UpdatePhaseText() {
        phaseText.text = _currentPhase.ToString();
    }
}
