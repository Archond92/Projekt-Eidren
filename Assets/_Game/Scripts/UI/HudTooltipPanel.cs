using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>
	/// N04-003: Das Tooltip-Feld im Kampf-HUD. Eines fuer alle Knoepfe — die
	/// Faehigkeits-, Waffen- und Verbrauchsknoepfe liegen ohnehin in derselben
	/// Leiste, und zwei Tooltips gleichzeitig will niemand sehen.
	/// </summary>
	public sealed class HudTooltipPanel : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private Text label;

		public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

		public string CurrentText => (label != null) ? label.text : string.Empty;

		public void ConfigureReferences(GameObject configuredRoot, Text configuredLabel)
		{
			panelRoot = configuredRoot;
			label = configuredLabel;
		}

		public void Show(string text)
		{
			if (panelRoot == null || label == null || string.IsNullOrWhiteSpace(text))
			{
				Hide();
				return;
			}
			label.text = text;
			panelRoot.SetActive(value: true);
		}

		public void Hide()
		{
			if (panelRoot != null)
			{
				panelRoot.SetActive(value: false);
			}
		}
	}
}
