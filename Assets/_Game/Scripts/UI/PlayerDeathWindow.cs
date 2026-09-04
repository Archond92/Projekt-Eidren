using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class PlayerDeathWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private Button reviveButton;

		[SerializeField]
		private Button mainMenuButton;

		[SerializeField]
		private Text areaText;

		public bool IsOpen { get; private set; }

		public Button ReviveButton => reviveButton;

		public Button MainMenuButton => mainMenuButton;

		public string AreaLabel => (areaText != null) ? areaText.text : string.Empty;

		public event Action ReviveRequested;

		public event Action MainMenuRequested;

		public void ConfigureReferences(GameObject configuredPanelRoot, Button configuredReviveButton, Button configuredMainMenuButton, Text configuredAreaText)
		{
			panelRoot = configuredPanelRoot;
			reviveButton = configuredReviveButton;
			mainMenuButton = configuredMainMenuButton;
			areaText = configuredAreaText;
		}

		public void Initialize()
		{
			ValidateReferences();
			reviveButton.onClick.RemoveAllListeners();
			mainMenuButton.onClick.RemoveAllListeners();
			reviveButton.onClick.AddListener(HandleRevive);
			mainMenuButton.onClick.AddListener(HandleMainMenu);
			panelRoot.SetActive(value: false);
			IsOpen = false;
		}

		public void Open(string areaName)
		{
			ValidateReferences();
			areaText.text = (string.IsNullOrWhiteSpace(areaName) ? string.Empty : ("Gebiet: " + areaName));
			panelRoot.SetActive(value: true);
			IsOpen = true;
			SetRequestPending(pending: false);
			EventSystem.current?.SetSelectedGameObject(reviveButton.gameObject);
		}

		public void Close()
		{
			if (panelRoot != null)
			{
				panelRoot.SetActive(value: false);
			}
			IsOpen = false;
			EventSystem.current?.SetSelectedGameObject(null);
		}

		public void SetRequestPending(bool pending)
		{
			if (reviveButton != null)
			{
				reviveButton.interactable = !pending;
			}
			if (mainMenuButton != null)
			{
				mainMenuButton.interactable = !pending;
			}
		}

		private void HandleRevive()
		{
			if (IsOpen && reviveButton.interactable)
			{
				this.ReviveRequested?.Invoke();
			}
		}

		private void HandleMainMenu()
		{
			if (IsOpen && mainMenuButton.interactable)
			{
				this.MainMenuRequested?.Invoke();
			}
		}

		private void ValidateReferences()
		{
			if (panelRoot == null || reviveButton == null || mainMenuButton == null || areaText == null)
			{
				throw new InvalidOperationException("PlayerDeathWindow prefab references are incomplete.");
			}
		}

		private void OnDestroy()
		{
			reviveButton?.onClick.RemoveListener(HandleRevive);
			mainMenuButton?.onClick.RemoveListener(HandleMainMenu);
		}
	}
}
