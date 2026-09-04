using Eidren.AI;
using Eidren.Composition;
using Eidren.Data;
using Eidren.UI;
using System.IO;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
public static class GaronEmberRuinsContentBuilder
{
	public const string GaronPrefabPath = "Assets/_Game/Prefabs/Bosses/Garon.prefab";

	public const string EncounterViewPrefabPath = "Assets/_Game/Resources/UI/BossEncounterView.prefab";

	public const string TelegraphMaterialPath = "Assets/_Game/Art/Enemies/Materials/EN_GaronTelegraph.mat";

	private static readonly Color Ink = new Color(0.018f, 0.03f, 0.042f, 0.98f);

	private static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f, 1f);

	private static readonly Color Ember = new Color(0.9f, 0.28f, 0.13f, 1f);

	private static readonly Color Gold = new Color(0.95f, 0.64f, 0.17f, 1f);

	private static Font _font;

	private static Sprite _uiSprite;

	[MenuItem("Eidren/Bosses/Build Garon in Ember Ruins")]
	public static void Build()
	{
		EnsureFolder("Assets/_Game/Prefabs", "Bosses");
		EnsureFolder("Assets/_Game/Art/Enemies", "Materials");
		EnsureFolder("Assets/_Game/Resources", "UI");
		BossData bossData = AssetDatabase.LoadAssetAtPath<BossData>("Assets/_Game/Data/Bosses/Garon.asset");
		if (bossData == null)
		{
			throw new FileNotFoundException("Garon.asset");
		}
		EnsureTelegraphMaterial();
		BuildGaronPrefab(bossData);
		BuildEncounterView();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EidrenSceneStructureBuilder.BuildMapExitScenes();
		Debug.Log("Eidren: Garon boss content and Ember Ruins encounter built.");
	}

	private static void BuildGaronPrefab(BossData data)
	{
		GameObject root = new GameObject("Garon");
		try
		{
			CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
			capsuleCollider.center = new Vector3(0f, 1.55f, 0f);
			capsuleCollider.height = 3.1f;
			capsuleCollider.radius = 1.25f;
			NavMeshAgent navMeshAgent = root.AddComponent<NavMeshAgent>();
			navMeshAgent.radius = data.Navigation.AgentRadius;
			navMeshAgent.height = data.Navigation.AgentHeight;
			navMeshAgent.speed = data.Navigation.MoveSpeed;
			navMeshAgent.acceleration = data.Navigation.Acceleration;
			navMeshAgent.angularSpeed = data.Navigation.AngularSpeed;
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/Prefabs/Visuals/Garon.prefab");
			if (gameObject == null)
			{
				throw new FileNotFoundException("Assets/_Game/Resources/Prefabs/Visuals/Garon.prefab");
			}
			GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(gameObject, root.transform);
			obj.name = "Garon_Visual";
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localRotation = Quaternion.identity;
			obj.transform.localScale = Vector3.one;
			root.AddComponent<BossController>();
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Bosses/Garon.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Prefabs/Bosses/Garon.prefab.");
			}
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	private static void BuildEncounterView()
	{
		_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		_uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
		GameObject root = new GameObject("BossEncounterView", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BossEncounterView));
		try
		{
			Canvas component = root.GetComponent<Canvas>();
			component.renderMode = RenderMode.ScreenSpaceOverlay;
			component.sortingOrder = 75;
			CanvasScaler component2 = root.GetComponent<CanvasScaler>();
			component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			component2.referenceResolution = new Vector2(1920f, 1080f);
			component2.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			component2.matchWidthOrHeight = 1f;
			RectTransform safe = Rect("SafeArea", root.transform);
			Stretch(safe);
			safe.gameObject.AddComponent<SafeAreaPanel>();
			RectTransform bossPanel = Rect("BossPanel", safe);
			Place(bossPanel, new Vector2(0f, -24f), new Vector2(760f, 130f), new Vector2(0.5f, 1f));
			Image image = bossPanel.gameObject.AddComponent<Image>();
			image.sprite = _uiSprite;
			image.type = Image.Type.Sliced;
			image.color = Ink;
			CanvasGroup bossGroup = bossPanel.gameObject.AddComponent<CanvasGroup>();
			Text name = TextElement("BossName", bossPanel, "GARON", 25, TextAnchor.MiddleCenter, Cream);
			Place(name.rectTransform, new Vector2(0f, -10f), new Vector2(700f, 36f), new Vector2(0.5f, 1f));
			Image hp = Bar("Health", bossPanel, new Vector2(0f, -55f), new Vector2(690f, 20f), Ember);
			Image stagger = Bar("Stagger", bossPanel, new Vector2(0f, -84f), new Vector2(690f, 12f), Gold);
			Text state = TextElement("State", bossPanel, "RUHEND", 13, TextAnchor.MiddleCenter, Gold);
			Place(state.rectTransform, new Vector2(0f, -101f), new Vector2(400f, 24f), new Vector2(0.5f, 1f));
			RectTransform result = Rect("VictoryResult", safe);
			Stretch(result);
			result.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.01f, 0.016f, 0.88f);
			CanvasGroup resultGroup = result.gameObject.AddComponent<CanvasGroup>();
			RectTransform card = Rect("VictoryCard", result);
			Place(card, Vector2.zero, new Vector2(760f, 440f), new Vector2(0.5f, 0.5f));
			Image image2 = card.gameObject.AddComponent<Image>();
			image2.sprite = _uiSprite;
			image2.type = Image.Type.Sliced;
			image2.color = Ink;
			Text resultTitle = TextElement("ResultTitle", card, "GARON BESIEGT", 44, TextAnchor.MiddleCenter, Cream);
			Place(resultTitle.rectTransform, new Vector2(0f, -58f), new Vector2(660f, 72f), new Vector2(0.5f, 1f));
			Text reward = TextElement("Reward", card, "BELOHNUNG", 23, TextAnchor.MiddleCenter, Gold);
			Place(reward.rectTransform, new Vector2(0f, -170f), new Vector2(650f, 100f), new Vector2(0.5f, 1f));
			Button close = ButtonElement("CloseButton", card, "WEITERSPIELEN");
			Place(close.GetComponent<RectTransform>(), new Vector2(0f, 55f), new Vector2(390f, 72f), new Vector2(0.5f, 0f));
			root.GetComponent<BossEncounterView>().ConfigureUi(bossGroup, name, hp, stagger, state, resultGroup, resultTitle, reward, close);
			bossGroup.alpha = 0f;
			bossGroup.interactable = false;
			bossGroup.blocksRaycasts = false;
			resultGroup.alpha = 0f;
			resultGroup.interactable = false;
			resultGroup.blocksRaycasts = false;
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/BossEncounterView.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Resources/UI/BossEncounterView.prefab.");
			}
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	private static Image Bar(string name, Transform parent, Vector2 position, Vector2 size, Color fillColor)
	{
		RectTransform background = Rect(name, parent);
		Place(background, position, size, new Vector2(0.5f, 1f));
		Image image = background.gameObject.AddComponent<Image>();
		image.sprite = _uiSprite;
		image.type = Image.Type.Sliced;
		image.color = new Color(0.12f, 0.15f, 0.16f, 1f);
		RectTransform rectTransform = Rect("Fill", background);
		Stretch(rectTransform, 3f);
		Image image2 = rectTransform.gameObject.AddComponent<Image>();
		image2.sprite = _uiSprite;
		image2.type = Image.Type.Filled;
		image2.fillMethod = Image.FillMethod.Horizontal;
		image2.color = fillColor;
		image2.raycastTarget = false;
		return image2;
	}

	private static Button ButtonElement(string name, Transform parent, string label)
	{
		RectTransform rect = Rect(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.sprite = _uiSprite;
		image.type = Image.Type.Sliced;
		image.color = Gold;
		Button result = rect.gameObject.AddComponent<Button>();
		Stretch(TextElement("Label", rect, label, 23, TextAnchor.MiddleCenter, Ink).rectTransform, 6f);
		return result;
	}

	private static Text TextElement(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color)
	{
		Text text = Rect(name, parent).gameObject.AddComponent<Text>();
		text.font = _font;
		text.text = value;
		text.fontSize = fontSize;
		text.fontStyle = FontStyle.Bold;
		text.alignment = alignment;
		text.color = color;
		text.raycastTarget = false;
		return text;
	}

	private static RectTransform Rect(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return gameObject.GetComponent<RectTransform>();
	}

	private static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
	{
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = anchor;
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private static void Stretch(RectTransform rect, float inset = 0f)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.one * inset;
		rect.offsetMax = Vector2.one * (0f - inset);
	}

	private static Material EnsureTelegraphMaterial()
	{
		Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Enemies/Materials/EN_GaronTelegraph.mat");
		if (material == null)
		{
			Material material2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Resources/EidrenRuntimeMaterial.mat");
			if (material2 == null)
			{
				throw new FileNotFoundException("EidrenRuntimeMaterial.mat");
			}
			material = new Material(material2)
			{
				name = "EN_GaronTelegraph"
			};
			AssetDatabase.CreateAsset(material, "Assets/_Game/Art/Enemies/Materials/EN_GaronTelegraph.mat");
		}
		material.color = new Color(0.95f, 0.22f, 0.1f, 0.62f);
		EditorUtility.SetDirty(material);
		return material;
	}

	private static void EnsureFolder(string parent, string child)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + child))
		{
			AssetDatabase.CreateFolder(parent, child);
		}
	}
}
}
