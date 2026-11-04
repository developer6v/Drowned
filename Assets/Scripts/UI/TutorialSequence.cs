using System;
using System.Collections.Generic;

namespace Drowned.UI
{
    /// <summary>O que faz um passo do tutorial avançar.</summary>
    public enum TutorialTrigger
    {
        /// <summary>O jogador clica em "Próximo".</summary>
        Acknowledge = 0,
        /// <summary>Qualquer estrutura nova foi construída.</summary>
        StructureBuilt = 1,
        /// <summary>A velocidade do jogo mudou (pausa, 1x, 2x, 3x).</summary>
        SpeedChanged = 2,
    }

    /// <summary>Um passo do tutorial: texto mostrado e o gatilho que leva ao próximo.</summary>
    public readonly struct TutorialStep
    {
        public string Text { get; }
        public TutorialTrigger Trigger { get; }

        public TutorialStep(string text, TutorialTrigger trigger)
        {
            Text = text ?? "";
            Trigger = trigger;
        }
    }

    /// <summary>
    /// Sequência de dicas da primeira partida (SCRUM-193). Lógica pura: só avança quando o gatilho do passo
    /// atual acontece. A view (<see cref="TutorialView"/>) mostra o passo e repassa os eventos do jogo.
    /// </summary>
    public sealed class TutorialSequence
    {
        /// <summary>Disparado ao entrar em um passo (inclusive o primeiro, ao chamar <see cref="Start"/>).</summary>
        public event Action<int, TutorialStep> OnStepChanged;

        /// <summary>Disparado uma vez, ao terminar o último passo ou ao pular.</summary>
        public event Action OnFinished;

        private readonly List<TutorialStep> _steps;

        public int Count => _steps.Count;

        /// <summary>Índice do passo atual; -1 antes de <see cref="Start"/>.</summary>
        public int Index { get; private set; } = -1;

        public bool IsRunning => Index >= 0 && !IsFinished;
        public bool IsFinished { get; private set; }

        public TutorialStep Current => IsRunning ? _steps[Index] : default;

        public TutorialSequence(IEnumerable<TutorialStep> steps)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            _steps = new List<TutorialStep>(steps);
        }

        /// <summary>Começa no primeiro passo. Sem passos, termina na hora.</summary>
        public void Start()
        {
            if (Index >= 0 || IsFinished) return;
            if (_steps.Count == 0)
            {
                Finish();
                return;
            }
            Index = 0;
            OnStepChanged?.Invoke(Index, _steps[Index]);
        }

        /// <summary>Informa que algo aconteceu no jogo. Avança se for o gatilho do passo atual. Devolve true se avançou.</summary>
        public bool Notify(TutorialTrigger trigger)
        {
            if (!IsRunning || _steps[Index].Trigger != trigger) return false;
            Index++;
            if (Index >= _steps.Count)
            {
                Index = _steps.Count - 1;
                Finish();
            }
            else OnStepChanged?.Invoke(Index, _steps[Index]);
            return true;
        }

        /// <summary>Encerra o tutorial de vez.</summary>
        public void Skip()
        {
            if (IsFinished) return;
            Finish();
        }

        private void Finish()
        {
            IsFinished = true;
            OnFinished?.Invoke();
        }
    }
}
