using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Eidren.Input
{
	public sealed class InputDisplayFamilyMonitor : MonoBehaviour
	{
		private PlayerInputReader _reader;

		public void Initialize(PlayerInputReader reader)
		{
			_reader = reader;
		}

		private void OnEnable()
		{
			InputSystem.onEvent += new Action<InputEventPtr, InputDevice>(Observe);
		}

		private void OnDisable()
		{
			InputSystem.onEvent -= new Action<InputEventPtr, InputDevice>(Observe);
		}

		private void Observe(InputEventPtr inputEvent, InputDevice device)
		{
			if (!(_reader == null) && device != null)
			{
				if (device is Touchscreen)
				{
					_reader.SetDisplayFamily(InputDisplayFamily.Touch);
				}
				else
				{
					_reader.SetDisplayFamily((!(device is Gamepad)) ? InputDisplayFamily.KeyboardMouse : InputDisplayFamily.Gamepad);
				}
			}
		}
	}
}
