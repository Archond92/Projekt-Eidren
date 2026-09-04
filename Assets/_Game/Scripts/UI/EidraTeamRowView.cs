using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>
	/// F32-006: Eine Zeile im Eidra-Fenster — ein gefangenes Eidra mit zwei
	/// Knoepfen fuer die beiden aktiven Plaetze. Die Zeilen sind im Prefab
	/// autoriert und werden nur ein- und ausgeblendet (UI wird gebaut, nicht
	/// zur Laufzeit zusammengesteckt).
	/// </summary>
	public sealed class EidraTeamRowView : MonoBehaviour
	{
		[SerializeField]
		private Text nameLabel;

		[SerializeField]
		private Text stateLabel;

		[SerializeField]
		private Button firstSlotButton;

		[SerializeField]
		private Button secondSlotButton;

		private int _index;

		private Action<int, int> _assign;

		public Button FirstSlotButton => firstSlotButton;

		public void ConfigureReferences(Text configuredName, Text configuredState, Button configuredFirst, Button configuredSecond)
		{
			nameLabel = configuredName;
			stateLabel = configuredState;
			firstSlotButton = configuredFirst;
			secondSlotButton = configuredSecond;
		}

		public void Bind(int index, Action<int, int> assign)
		{
			_index = index;
			_assign = assign;
			firstSlotButton.onClick.RemoveAllListeners();
			secondSlotButton.onClick.RemoveAllListeners();
			firstSlotButton.onClick.AddListener(delegate
			{
				_assign?.Invoke(_index, 0);
			});
			secondSlotButton.onClick.AddListener(delegate
			{
				_assign?.Invoke(_index, 1);
			});
		}

		public void Show(string displayName, string state, bool secondSlotAvailable, bool onFirstSlot, bool onSecondSlot)
		{
			base.gameObject.SetActive(value: true);
			nameLabel.text = displayName ?? string.Empty;
			stateLabel.text = state ?? string.Empty;
			// Der Platz, auf dem das Eidra schon steht, braucht keinen Knopf —
			// so ist auf einen Blick klar, was belegt ist.
			firstSlotButton.gameObject.SetActive(!onFirstSlot);
			secondSlotButton.gameObject.SetActive(secondSlotAvailable && !onSecondSlot);
		}

		public void Hide()
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
