using Eidren.Input;
using UnityEngine.EventSystems;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class InteractionButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerExitHandler
	{
		private PlayerInputReader _input;

		private bool _available;

		public void Initialize(PlayerInputReader input)
		{
			_input = input;
		}

		public void SetAvailable(bool available)
		{
			_available = available;
			if (!available && _input != null)
			{
				_input.SetVirtualInteract(held: false);
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (_available && _input != null)
			{
				_input.SetVirtualInteract(held: true);
			}
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (_input != null)
			{
				_input.SetVirtualInteract(held: false);
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (_input != null)
			{
				_input.SetVirtualInteract(held: false);
			}
		}

		private void OnDisable()
		{
			if (_input != null)
			{
				_input.SetVirtualInteract(held: false);
			}
		}
	}
}
