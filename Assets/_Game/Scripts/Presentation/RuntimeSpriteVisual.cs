using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace Eidren.Presentation
{
	public sealed class RuntimeSpriteVisual : MonoBehaviour
	{
		private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

		private static readonly Dictionary<string, Sprite[]> AtlasCache = new Dictionary<string, Sprite[]>();

		private SpriteRenderer _renderer;

		private Camera _camera;

		private Vector3 _poseOffset;

		private Vector3 _poseScale = Vector3.one;

		private float _poseRoll;

		private bool _poseEnabled;

		public SpriteRenderer SpriteRenderer => _renderer;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetCaches()
		{
			SpriteCache.Clear();
			AtlasCache.Clear();
		}

		public void SetSprite(Sprite sprite, int sortingOrder = 0)
		{
			if (_renderer == null)
			{
				_renderer = GetComponent<SpriteRenderer>();
			}
			if (_renderer == null)
			{
				_renderer = base.gameObject.AddComponent<SpriteRenderer>();
			}
			_renderer.sprite = sprite;
			_renderer.color = Color.white;
			_renderer.sortingOrder = sortingOrder;
			_renderer.shadowCastingMode = ShadowCastingMode.Off;
			_renderer.receiveShadows = false;
		}

		public bool SetArt(string resourcePath, float worldHeight, int sortingOrder = 0, float pivotY = 0.04f)
		{
			if (!SpriteCache.TryGetValue(resourcePath, out var value))
			{
				Texture2D texture2D = Resources.Load<Texture2D>(resourcePath);
				if (texture2D == null)
				{
					return false;
				}
				float pixelsPerUnit = (float)texture2D.height / Mathf.Max(0.1f, worldHeight);
				value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, pivotY), pixelsPerUnit, 0u, SpriteMeshType.FullRect);
				value.name = resourcePath.Replace('/', '_');
				value.hideFlags = HideFlags.DontSave;
				SpriteCache[resourcePath] = value;
			}
			if (_renderer == null)
			{
				_renderer = base.gameObject.AddComponent<SpriteRenderer>();
			}
			_renderer.sprite = value;
			_renderer.color = Color.white;
			_renderer.sortingOrder = sortingOrder;
			_renderer.shadowCastingMode = ShadowCastingMode.Off;
			_renderer.receiveShadows = false;
			return true;
		}

		public void SetPose(Vector3 localOffset, Vector3 localScale, float roll)
		{
			_poseEnabled = true;
			_poseOffset = localOffset;
			_poseScale = localScale;
			_poseRoll = roll;
		}

		public void SetTint(Color color)
		{
			if (_renderer != null)
			{
				_renderer.color = color;
			}
		}

		public bool SetAtlasCell(string resourcePath, int columns, int rows, int index, float cellWorldHeight, int sortingOrder = 0, float pivotY = 0.04f)
		{
			string key = $"{resourcePath}:{columns}:{rows}:{cellWorldHeight:0.###}:{pivotY:0.###}";
			if (!AtlasCache.TryGetValue(key, out var value))
			{
				Texture2D texture2D = Resources.Load<Texture2D>(resourcePath);
				if (texture2D == null)
				{
					return false;
				}
				int num = texture2D.width / columns;
				int num2 = texture2D.height / rows;
				float pixelsPerUnit = (float)num2 / Mathf.Max(0.1f, cellWorldHeight);
				value = new Sprite[columns * rows];
				for (int i = 0; i < rows; i++)
				{
					for (int j = 0; j < columns; j++)
					{
						Rect rect = new Rect(j * num, texture2D.height - (i + 1) * num2, num, num2);
						Sprite sprite = Sprite.Create(texture2D, rect, new Vector2(0.5f, pivotY), pixelsPerUnit, 0u, SpriteMeshType.FullRect);
						sprite.name = $"{resourcePath}_{i}_{j}";
						sprite.hideFlags = HideFlags.DontSave;
						value[i * columns + j] = sprite;
					}
				}
				AtlasCache[key] = value;
			}
			if (index < 0 || index >= value.Length)
			{
				return false;
			}
			if (_renderer == null)
			{
				_renderer = base.gameObject.AddComponent<SpriteRenderer>();
			}
			_renderer.sprite = value[index];
			_renderer.color = Color.white;
			_renderer.sortingOrder = sortingOrder;
			_renderer.shadowCastingMode = ShadowCastingMode.Off;
			_renderer.receiveShadows = false;
			return true;
		}

		private void LateUpdate()
		{
			if (_camera == null)
			{
				_camera = Camera.main;
			}
			if (_camera != null)
			{
				base.transform.rotation = _camera.transform.rotation * Quaternion.Euler(0f, 0f, _poseRoll);
			}
			if (_poseEnabled)
			{
				base.transform.localPosition = _poseOffset;
				base.transform.localScale = _poseScale;
			}
		}
	}
}
