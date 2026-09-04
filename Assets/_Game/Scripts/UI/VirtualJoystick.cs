using Eidren.Input;
using UnityEngine.EventSystems;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IDragHandler
	{
		private PlayerInputReader _input;

		private RectTransform _base;

		private RectTransform _knob;

		private float _radius;

		public void Initialize(PlayerInputReader input, RectTransform joystickBase, RectTransform knob)
		{
			_input = input;
			_base = joystickBase;
			_knob = knob;
			_radius = _base.sizeDelta.x * 0.34f;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			OnDrag(eventData);
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_base, eventData.position, eventData.pressEventCamera, out var localPoint))
			{
				Vector2 vector = Vector2.ClampMagnitude(localPoint / _radius, 1f);
				_knob.anchoredPosition = vector * _radius;
				_input.SetVirtualMove(vector);
			}
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			_knob.anchoredPosition = Vector2.zero;
			_input.SetVirtualMove(Vector2.zero);
		}
	}
}
