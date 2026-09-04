using Eidren.AI;
using Eidren.Combat;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class WildlingStatusBars : MonoBehaviour
	{
		[SerializeField]
		private Canvas worldCanvas;

		[SerializeField]
		private Image healthFill;

		[SerializeField]
		private Image staggerFill;

		[SerializeField]
		private Text protectionLabel;

		[SerializeField]
		private Text nameLabel;

		private EnemyControllerBase _source;

		private IDamageable _damageable;

		private IStaggerable _staggerable;

		private Camera _camera;

		private float _lastProtection = -1f;

		public Canvas WorldCanvas => worldCanvas;

		public Image HealthFill => healthFill;

		public Image StaggerFill => staggerFill;

		public Text NameLabel => nameLabel;

		public void ConfigureReferences(Canvas canvas, Image health, Image stagger, Text protection = null, Text name = null)
		{
			worldCanvas = canvas;
			healthFill = health;
			staggerFill = stagger;
			protectionLabel = protection;
			// Nur setzen, wenn uebergeben — der alte 4er-Aufruf (T2-Builder)
			// darf ein verdrahtetes Namenslabel nicht wegnullen.
			if (name != null)
			{
				nameLabel = name;
			}
		}

		private void OnEnable()
		{
			Bind(GetComponentInParent<EnemyControllerBase>());
			_camera = Camera.main;
		}

		private void Bind(EnemyControllerBase source)
		{
			Unbind();
			_source = source;
			_damageable = source;
			_staggerable = source;
			if (_source == null)
			{
				SetVisible(visible: false);
				return;
			}
			_source.HealthChanged += RefreshHealth;
			_source.StaggerChanged += RefreshStagger;
			_source.Died += HandleDied;
			RefreshHealth(_source.CurrentHealth, _source.MaxHealth);
			RefreshStagger(_staggerable.CurrentStagger, _staggerable.MaxStagger);
			RefreshProtection();
			RefreshName();
		}

		private void RefreshName()
		{
			if (!(nameLabel == null))
			{
				string anzeigename = ((_source != null) ? _source.DisplayName : string.Empty);
				nameLabel.text = anzeigename;
				nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(anzeigename));
			}
		}

		private void RefreshProtection()
		{
			if (!(protectionLabel == null))
			{
				float num = ((_source != null) ? _source.Protection : 0f);
				_lastProtection = num;
				protectionLabel.text = ((num > 0f) ? $"SCHUTZ {num:P0}" : string.Empty);
				protectionLabel.gameObject.SetActive(num > 0f);
			}
		}

		private void RefreshHealth(float current, float maximum)
		{
			if (healthFill != null)
			{
				healthFill.fillAmount = Ratio(current, maximum);
			}
			SetVisible(_damageable != null && _damageable.IsAlive);
		}

		private void RefreshStagger(float current, float maximum)
		{
			if (staggerFill != null)
			{
				staggerFill.fillAmount = Ratio(current, maximum);
			}
		}

		private void HandleDied()
		{
			SetVisible(visible: false);
		}

		private void LateUpdate()
		{
			if (!(worldCanvas == null) && worldCanvas.gameObject.activeSelf && !(_camera == null))
			{
				worldCanvas.transform.rotation = _camera.transform.rotation;
			}
			// Der Schutz des Kernwaechters wechselt mit der Kernphase, ohne
			// dass ein Health-Event feuert — nur bei Wertaenderung neu
			// formatieren, damit im Takt keine Strings entstehen (§8).
			if (_source != null && protectionLabel != null && _source.Protection != _lastProtection)
			{
				RefreshProtection();
			}
		}

		private void SetVisible(bool visible)
		{
			if (worldCanvas != null && worldCanvas.gameObject.activeSelf != visible)
			{
				worldCanvas.gameObject.SetActive(visible);
			}
		}

		private void OnDisable()
		{
			Unbind();
			_camera = null;
		}

		private void Unbind()
		{
			if (_source != null)
			{
				_source.HealthChanged -= RefreshHealth;
				_source.StaggerChanged -= RefreshStagger;
				_source.Died -= HandleDied;
			}
			_source = null;
			_damageable = null;
			_staggerable = null;
		}

		private static float Ratio(float current, float maximum)
		{
			return (maximum <= 0f) ? 0f : Mathf.Clamp01(current / maximum);
		}
	}
}
