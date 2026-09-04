using Eidren.Input;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class BuildingPlacementBar : MonoBehaviour
	{
		[SerializeField]
		private GameObject barRoot;

		[SerializeField]
		private Button confirmButton;

		[SerializeField]
		private Button cancelButton;

		[SerializeField]
		private Button rotateButton;

		[SerializeField]
		private GameObject keyboardHints;

		[SerializeField]
		private GameObject gamepadHints;

		private PlayerInputReader _input;

		public Button ConfirmButton => confirmButton;

		public Button CancelButton => cancelButton;

		public Button RotateButton => rotateButton;

		public GameObject KeyboardHints => keyboardHints;

		public GameObject GamepadHints => gamepadHints;

		public bool IsVisible => barRoot != null && barRoot.activeSelf;

		public void ConfigureReferences(GameObject authoredRoot, Button authoredConfirm, Button authoredCancel, Button authoredRotate, GameObject authoredKeyboardHints, GameObject authoredGamepadHints)
		{
			barRoot = authoredRoot;
			confirmButton = authoredConfirm;
			cancelButton = authoredCancel;
			rotateButton = authoredRotate;
			keyboardHints = authoredKeyboardHints;
			gamepadHints = authoredGamepadHints;
		}

		public void Bind(PlayerInputReader input)
		{
			Unbind();
			_input = input ?? throw new ArgumentNullException("input");
			confirmButton.onClick.AddListener(_input.PressInventoryUse);
			cancelButton.onClick.AddListener(_input.PressInventoryClose);
			rotateButton.onClick.AddListener(_input.PressBuildingRotate);
			_input.DisplayFamilyChanged += Refresh;
			Refresh(_input.DisplayFamily);
			SetVisible(visible: false);
		}

		public void SetVisible(bool visible)
		{
			if (barRoot != null)
			{
				barRoot.SetActive(visible);
			}
		}

		private void Refresh(InputDisplayFamily family)
		{
			if (keyboardHints != null)
			{
				keyboardHints.SetActive(family == InputDisplayFamily.KeyboardMouse);
			}
			if (gamepadHints != null)
			{
				gamepadHints.SetActive(family == InputDisplayFamily.Gamepad);
			}
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (!(_input == null))
			{
				confirmButton.onClick.RemoveListener(_input.PressInventoryUse);
				cancelButton.onClick.RemoveListener(_input.PressInventoryClose);
				rotateButton.onClick.RemoveListener(_input.PressBuildingRotate);
				_input.DisplayFamilyChanged -= Refresh;
				_input = null;
			}
		}
	}
}
