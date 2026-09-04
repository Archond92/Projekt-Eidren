using Eidren.Gameplay.Flow;
using Eidren.Input;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CombatHudResultPresenter : MonoBehaviour
	{
		[SerializeField]
		private GameObject root;

		[SerializeField]
		private Text resultText;

		[SerializeField]
		private Button restartButton;

		private PlayerInputReader _input;

		private GameFlowController _flow;

		public void ConfigureReferences(GameObject authoredRoot, Text authoredText, Button authoredRestart)
		{
			root = authoredRoot;
			resultText = authoredText;
			restartButton = authoredRestart;
		}

		public void Bind(PlayerInputReader input, GameFlowController flow)
		{
			Unbind();
			_input = input;
			_flow = flow;
			restartButton.onClick.AddListener(_input.PressRestart);
			if (_flow != null)
			{
				_flow.GameEnded += ShowResult;
			}
			root.SetActive(value: false);
		}

		private void ShowResult(bool victory, float duration, int staggers)
		{
			root.SetActive(value: true);
			resultText.text = (victory ? ("GARON BEZWUNGEN\n\n" + $"Kampfzeit  {TimeSpan.FromSeconds(duration):m\\:ss}\n" + $"Stagger-Phasen  {staggers}") : "DU BIST GEFALLEN\n\nNutze Ausweichen und Steinhaut.");
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (restartButton != null && _input != null)
			{
				restartButton.onClick.RemoveListener(_input.PressRestart);
			}
			if (_flow != null)
			{
				_flow.GameEnded -= ShowResult;
			}
			_input = null;
			_flow = null;
		}
	}
}
