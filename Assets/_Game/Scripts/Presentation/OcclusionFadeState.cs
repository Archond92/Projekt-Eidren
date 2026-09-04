using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// Fade-Zustand eines einzelnen Verdeckers: tauscht die Materialien des
	/// Renderers gegen Fade-Zwillinge (Sprites faden über die Tint-Farbe),
	/// stellt beim Freiwerden die Originale zurück und entsorgt die
	/// erzeugten Zwillinge. Ausgelagert aus ActorOcclusionTransparency —
	/// dort lebt die Orchestrierung (wer verdeckt wen), hier der Zustand.
	/// </summary>
	internal sealed class OcclusionFadeState
	{
		private readonly Renderer _renderer;

		private readonly Material[] _originalMaterials;

		private Material[] _fadeMaterials;

		private readonly Color _spriteColor;

		public float Alpha { get; set; } = 1f;

		public bool Requested { get; set; }

		public OcclusionFadeState(Renderer renderer)
		{
			_renderer = renderer;
			_originalMaterials = renderer.sharedMaterials;
			_spriteColor = ((renderer is SpriteRenderer spriteRenderer) ? spriteRenderer.color : Color.white);
		}

		public void Apply()
		{
			if (_renderer is SpriteRenderer spriteRenderer)
			{
				Color spriteColor = _spriteColor;
				spriteColor.a *= Alpha;
				spriteRenderer.color = spriteColor;
				return;
			}
			EnsureFadeMaterials();
			for (int i = 0; i < _fadeMaterials.Length; i++)
			{
				Material material = _fadeMaterials[i];
				if (!(material == null))
				{
					if (material.HasProperty("_BaseColor"))
					{
						Color color = material.GetColor("_BaseColor");
						color.a = Alpha;
						material.SetColor("_BaseColor", color);
					}
					if (material.HasProperty("_Color"))
					{
						Color color2 = material.GetColor("_Color");
						color2.a = Alpha;
						material.SetColor("_Color", color2);
					}
				}
			}
		}

		public void Restore()
		{
			if (!(_renderer == null))
			{
				if (_renderer is SpriteRenderer spriteRenderer)
				{
					spriteRenderer.color = _spriteColor;
				}
				else
				{
					_renderer.sharedMaterials = _originalMaterials;
				}
			}
		}

		public void Dispose()
		{
			if (_fadeMaterials == null)
			{
				return;
			}
			Material[] fadeMaterials = _fadeMaterials;
			foreach (Material material in fadeMaterials)
			{
				if (material != null)
				{
					Object.Destroy(material);
				}
			}
			_fadeMaterials = null;
		}

		private void EnsureFadeMaterials()
		{
			if (_fadeMaterials != null)
			{
				return;
			}
			_fadeMaterials = new Material[_originalMaterials.Length];
			for (int i = 0; i < _originalMaterials.Length; i++)
			{
				Material material = _originalMaterials[i];
				if (!(material == null))
				{
					_fadeMaterials[i] = ActorOcclusionTransparency.CreateFadeMaterial(material);
				}
			}
			_renderer.sharedMaterials = _fadeMaterials;
		}

		internal static void ApplyTransparencySetup(Material material)
		{
			if (material.HasProperty("_Surface"))
			{
				material.SetFloat("_Surface", 1f);
			}
			if (material.HasProperty("_Blend"))
			{
				material.SetFloat("_Blend", 0f);
			}
			if (material.HasProperty("_SrcBlend"))
			{
				material.SetFloat("_SrcBlend", 5f);
			}
			if (material.HasProperty("_DstBlend"))
			{
				material.SetFloat("_DstBlend", 10f);
			}
			if (material.HasProperty("_ZWrite"))
			{
				material.SetFloat("_ZWrite", 0f);
			}
			material.SetOverrideTag("RenderType", "Transparent");
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
			material.EnableKeyword("_ALPHABLEND_ON");
		}
	}
}
