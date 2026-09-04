using Eidren.Data;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapConnectionView : MonoBehaviour
	{
		[SerializeField]
		private Image line;

		[SerializeField]
		private Vector2 start;

		[SerializeField]
		private Vector2 end;

		[Min(2f)]
		[SerializeField]
		private float thickness = 6f;

		public void ConfigureUi(Image lineImage)
		{
			line = lineImage;
		}

		public void SetEndpoints(Vector2 from, Vector2 to)
		{
			start = from;
			end = to;
			RefreshLayout();
		}

		public void ApplyTheme(WorldMapThemeData theme)
		{
			if (line == null)
			{
				throw new InvalidOperationException("Connection view '" + base.name + "' has no line image.");
			}
			Color energyGlow = theme.EnergyGlow;
			energyGlow.a = 0.72f;
			line.color = energyGlow;
		}

		public void RefreshLayout()
		{
			if (!(line == null))
			{
				RectTransform rectTransform = base.transform.parent as RectTransform;
				RectTransform rectTransform2 = line.rectTransform;
				if (!(rectTransform == null))
				{
					Vector2 size = rectTransform.rect.size;
					Vector2 vector = new Vector2((end.x - start.x) * size.x, (end.y - start.y) * size.y);
					Vector2 anchorMax = (rectTransform2.anchorMin = (start + end) * 0.5f);
					rectTransform2.anchorMax = anchorMax;
					rectTransform2.anchoredPosition = Vector2.zero;
					rectTransform2.sizeDelta = new Vector2(vector.magnitude, thickness);
					rectTransform2.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vector.y, vector.x) * 57.29578f);
				}
			}
		}

		private void OnRectTransformDimensionsChange()
		{
			RefreshLayout();
		}
	}
}
