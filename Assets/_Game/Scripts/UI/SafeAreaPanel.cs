using UnityEngine;

namespace Eidren.UI
{
	[RequireComponent(typeof(RectTransform))]
	public sealed class SafeAreaPanel : MonoBehaviour
	{
		private RectTransform _rect;

		private Rect _lastSafeArea;

		private Vector2Int _lastScreenSize;

		private void Awake()
		{
			_rect = GetComponent<RectTransform>();
			Apply();
		}

		private void Update()
		{
			if (_lastSafeArea != Screen.safeArea || _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
			{
				Apply();
			}
		}

		private void Apply()
		{
			Rect safeArea = (_lastSafeArea = Screen.safeArea);
			_lastScreenSize = new Vector2Int(Screen.width, Screen.height);
			CalculateAnchors(safeArea, Screen.width, Screen.height, out var min, out var max);
			_rect.anchorMin = min;
			_rect.anchorMax = max;
			_rect.offsetMin = Vector2.zero;
			_rect.offsetMax = Vector2.zero;
		}

		public static void CalculateAnchors(Rect safeArea, int screenWidth, int screenHeight, out Vector2 min, out Vector2 max)
		{
			if (screenWidth <= 0 || screenHeight <= 0 || safeArea.width <= 0f || safeArea.height <= 0f)
			{
				min = Vector2.zero;
				max = Vector2.one;
				return;
			}
			float num = screenWidth;
			float num2 = screenHeight;
			min = new Vector2(Mathf.Clamp01(safeArea.xMin / num), Mathf.Clamp01(safeArea.yMin / num2));
			max = new Vector2(Mathf.Clamp01(safeArea.xMax / num), Mathf.Clamp01(safeArea.yMax / num2));
			if (max.x <= min.x || max.y <= min.y)
			{
				min = Vector2.zero;
				max = Vector2.one;
			}
		}
	}
}
