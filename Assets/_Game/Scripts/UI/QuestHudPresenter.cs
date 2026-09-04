using Eidren.Core.Services;
using Eidren.Data;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>
	/// F31-006: Zeigt den aktuellen Schritt der Tutorial-Questkette im HUD.
	/// Den Dienst reicht die Composition-Schicht über Configure herein (UI
	/// kennt den ServiceRoot nicht). Der Text wird nur bei Änderung neu
	/// gebaut (kein Aufbau pro Frame); nach dem Kettenabschluss
	/// verabschiedet sich das Label mit einer kurzen Meldung und blendet
	/// dauerhaft aus.
	/// </summary>
	public sealed class QuestHudPresenter : MonoBehaviour
	{
		private const float CompletionDisplaySeconds = 6f;

		[SerializeField]
		private Text label;

		private QuestProgressService _quest;

		private int _shownStepIndex = int.MinValue;

		private int _shownProgress = int.MinValue;

		private float _hideCompletionAt;

		public void ConfigureReferences(Text questLabel)
		{
			label = questLabel;
		}

		public void Configure(QuestProgressService quest)
		{
			if (_quest != null)
			{
				_quest.ChainCompleted -= HandleChainCompleted;
			}
			_quest = quest;
			_shownStepIndex = int.MinValue;
			_shownProgress = int.MinValue;
			_hideCompletionAt = 0f;
			if (_quest != null)
			{
				_quest.ChainCompleted += HandleChainCompleted;
			}
			if (label != null)
			{
				label.enabled = _quest != null && !_quest.IsCompleted;
			}
		}

		private void OnDestroy()
		{
			if (_quest != null)
			{
				_quest.ChainCompleted -= HandleChainCompleted;
				_quest = null;
			}
		}

		private void Update()
		{
			if (label == null || _quest == null)
			{
				return;
			}
			if (_hideCompletionAt > 0f)
			{
				if (Time.unscaledTime >= _hideCompletionAt)
				{
					_hideCompletionAt = 0f;
					label.enabled = false;
				}
				return;
			}
			if (!_quest.TryGetCurrentStep(out QuestStepData step))
			{
				return;
			}
			if (_quest.CurrentStepIndex == _shownStepIndex && _quest.CurrentStepProgress == _shownProgress)
			{
				return;
			}
			_shownStepIndex = _quest.CurrentStepIndex;
			_shownProgress = _quest.CurrentStepProgress;
			label.enabled = true;
			label.text = (step.TargetCount > 1)
				? $"AUFGABE: {step.DisplayText} ({_shownProgress}/{step.TargetCount})"
				: ("AUFGABE: " + step.DisplayText);
		}

		private void HandleChainCompleted()
		{
			if (label != null)
			{
				label.enabled = true;
				label.text = "TUTORIAL ABGESCHLOSSEN — der erste Eidra ist gefangen!";
				_hideCompletionAt = Time.unscaledTime + CompletionDisplaySeconds;
			}
		}
	}
}
