using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests
{
public sealed class V02UiAssetTests
{
	[Test]
	public void SmithingMark_UsesProductionIcon()
	{
		ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/SmithingMark.asset");
		Assert.That<ItemDefinition>(itemDefinition, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Sprite>(itemDefinition.Icon, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<string>(AssetDatabase.GetAssetPath(itemDefinition.Icon), (IResolveConstraint)(object)Is.EqualTo((object)"Assets/_Game/Art/Items/ITEM_SmithingMark.png"));
		Assert.That<string>(AssetDatabase.GetAssetPath(itemDefinition.Icon), (IResolveConstraint)(object)Does.Not.Contain("TMP"));
	}

	[Test]
	public void IgnivarAbilities_UseDistinctProductionIcons()
	{
		AbilityData abilityData = AssetDatabase.LoadAssetAtPath<AbilityData>("Assets/_Game/Data/Abilities/Glutkreis.asset");
		AbilityData brand = AssetDatabase.LoadAssetAtPath<AbilityData>("Assets/_Game/Data/Abilities/Schmelzbrand.asset");
		Assert.That<AbilityData>(abilityData, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<AbilityData>(brand, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Sprite>(abilityData.Icon, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Sprite>(brand.Icon, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Sprite>(abilityData.Icon, (IResolveConstraint)(object)Is.Not.SameAs((object)brand.Icon));
		Assert.That<string>(AssetDatabase.GetAssetPath(abilityData.Icon), (IResolveConstraint)(object)Is.EqualTo((object)"Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_EmberCircle.png"));
		Assert.That<string>(AssetDatabase.GetAssetPath(brand.Icon), (IResolveConstraint)(object)Is.EqualTo((object)"Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_MoltenBrand.png"));
	}

	[TestCase("Assets/_Game/Art/Items/ITEM_SmithingMark.png")]
	[TestCase("Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_EmberCircle.png")]
	[TestCase("Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_MoltenBrand.png")]
	public void ProductionIcons_AreSquareReadableSprites(string path)
	{
		Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
		Assert.That<Texture2D>(texture2D, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		Assert.That<Sprite>(sprite, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		Assert.That<int>(texture2D.width, (IResolveConstraint)(object)Is.EqualTo((object)256), path, Array.Empty<object>());
		Assert.That<int>(texture2D.height, (IResolveConstraint)(object)Is.EqualTo((object)256), path, Array.Empty<object>());
	}
}
}
