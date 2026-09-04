using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class MenuInputService : MonoBehaviour
	{
		private readonly struct BackHandler
		{
			public Func<bool> Handler { get; }

			public int Priority { get; }

			public long Order { get; }

			public BackHandler(Func<bool> handler, int priority, long order)
			{
				Handler = handler;
				Priority = priority;
				Order = order;
			}
		}

		private readonly List<BackHandler> _backHandlers = new List<BackHandler>();

		private readonly List<BackHandler> _routingSnapshot = new List<BackHandler>();

		private InputAction _backAction;

		private long _registrationOrder;

		public event Action BackPressed;

		public void PressBack()
		{
			RouteBack();
		}

		public void RegisterBackHandler(Func<bool> handler, int priority = 0)
		{
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}
			UnregisterBackHandler(handler);
			_backHandlers.Add(new BackHandler(handler, priority, ++_registrationOrder));
		}

		public void UnregisterBackHandler(Func<bool> handler)
		{
			if (handler != null)
			{
				_backHandlers.RemoveAll((BackHandler candidate) => candidate.Handler == handler);
			}
		}

		private void Awake()
		{
			CreateBackAction();
		}

		private void CreateBackAction()
		{
			if (_backAction != null)
			{
				_backAction.performed -= HandleBackPerformed;
				_backAction.Dispose();
			}
			_backAction = new InputAction("MenuBack", InputActionType.Button);
			_backAction.AddBinding("<Keyboard>/escape");
			_backAction.AddBinding("<Gamepad>/start");
			_backAction.performed += HandleBackPerformed;
		}

		private void OnEnable()
		{
			InputSystem.onDeviceChange += HandleDeviceChange;
			SceneManager.sceneLoaded += HandleSceneLoaded;
			_backAction?.Enable();
		}

		private void OnDisable()
		{
			InputSystem.onDeviceChange -= HandleDeviceChange;
			SceneManager.sceneLoaded -= HandleSceneLoaded;
			_backAction?.Disable();
		}

		private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			CreateBackAction();
			if (base.isActiveAndEnabled)
			{
				_backAction.Enable();
			}
		}

		private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
		{
			if (_backAction != null && change != InputDeviceChange.UsageChanged)
			{
				_backAction.Disable();
				_backAction.Enable();
			}
		}

		private void HandleBackPerformed(InputAction.CallbackContext context)
		{
			RouteBack();
		}

		private void RouteBack()
		{
			_backHandlers.Sort(CompareHandlers);
			_routingSnapshot.Clear();
			_routingSnapshot.AddRange(_backHandlers);
			foreach (BackHandler item in _routingSnapshot)
			{
				if (item.Handler())
				{
					return;
				}
			}
			this.BackPressed?.Invoke();
		}

		private static int CompareHandlers(BackHandler left, BackHandler right)
		{
			int num = right.Priority.CompareTo(left.Priority);
			return (num != 0) ? num : right.Order.CompareTo(left.Order);
		}

		private void OnDestroy()
		{
			_backHandlers.Clear();
			_routingSnapshot.Clear();
			if (_backAction != null)
			{
				_backAction.performed -= HandleBackPerformed;
				_backAction.Dispose();
			}
		}
	}
}
