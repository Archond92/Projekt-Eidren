using Eidren.Eidra;
using UnityEngine.EventSystems;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class AbilityRangePreview : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerExitHandler
	{
		private EidraTeamController _eidra;

		private int _skillIndex;

		public void Initialize(EidraTeamController eidra, int skillIndex)
		{
			_eidra = eidra;
			_skillIndex = skillIndex;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			_eidra?.SetRangePreview(_skillIndex, visible: true);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			_eidra?.SetRangePreview(_skillIndex, visible: false);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_eidra?.SetRangePreview(_skillIndex, visible: false);
		}

		private void OnDisable()
		{
			_eidra?.SetRangePreview(_skillIndex, visible: false);
		}
	}
}
