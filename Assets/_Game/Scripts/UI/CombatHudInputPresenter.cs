using Eidren.Input;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CombatHudInputPresenter : MonoBehaviour
	{
		[SerializeField]
		private GameObject joystickRoot;

		[SerializeField]
		private VirtualJoystick joystick;

		[SerializeField]
		private RectTransform joystickBase;

		[SerializeField]
		private RectTransform joystickKnob;

		[SerializeField]
		private GameObject keyboardHints;

		[SerializeField]
		private GameObject gamepadHints;

		private PlayerInputReader _input;

		public GameObject JoystickRoot => joystickRoot;

		public GameObject KeyboardHints => keyboardHints;

		public GameObject GamepadHints => gamepadHints;

		public void ConfigureReferences(GameObject authoredJoystickRoot, VirtualJoystick authoredJoystick, RectTransform authoredBase, RectTransform authoredKnob, GameObject authoredKeyboardHints, GameObject authoredGamepadHints)
		{
			joystickRoot = authoredJoystickRoot;
			joystick = authoredJoystick;
			joystickBase = authoredBase;
			joystickKnob = authoredKnob;
			keyboardHints = authoredKeyboardHints;
			gamepadHints = authoredGamepadHints;
		}

		public void Bind(PlayerInputReader input)
		{
			Unbind();
			_input = input;
			joystick.Initialize(input, joystickBase, joystickKnob);
			_input.DisplayFamilyChanged += Refresh;
			Refresh(_input.DisplayFamily);
		}

		private void Refresh(InputDisplayFamily family)
		{
			joystickRoot.SetActive(family == InputDisplayFamily.Touch);
			keyboardHints.SetActive(family == InputDisplayFamily.KeyboardMouse);
			gamepadHints.SetActive(family == InputDisplayFamily.Gamepad);
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_input != null)
			{
				_input.DisplayFamilyChanged -= Refresh;
			}
			_input = null;
		}
	}
}
