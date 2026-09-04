using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Der Fade-Zwilling wurde nur per
	/// Shader.Find geholt und war in keiner Szene referenziert — Unity hat
	/// ihn aus jedem Player-Build GESTRIPPT. Im Editor (alle Tests!) war
	/// die Ausblendung deshalb gruen, in jeder ausgelieferten Exe wirkungslos.
	/// Ein Material unter Resources garantiert die Aufnahme in den Build.
	/// </summary>
	public sealed class OcclusionFadeTwinBundlingTests
	{
		[Test]
		public void FadeZwilling_LiegtAlsResourcesMaterialImBuild()
		{
			Material material = Resources.Load<Material>("Materials/OcclusionFadeTwin");
			Assert.That(material, Is.Not.Null,
				"Resources/Materials/OcclusionFadeTwin fehlt — ohne Referenz strippt Unity den Fade-Shader aus dem Build.");
			Assert.That(material.shader, Is.Not.Null);
			Assert.That(material.shader.name, Is.EqualTo("Eidren/World/VertexLitFade"));
		}
	}
}
