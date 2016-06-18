namespace UnityVC
{
	using UnityEngine;
	using System;

	public enum WorkflowMode
	{
		Specular,
		Metallic,
		Dielectric
	}

	public class Util
	{
				
		public static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
		{
			// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
			// (MaterialProperty value might come from renderer material property block)
			SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
			if (workflowMode == WorkflowMode.Specular && material.HasProperty("_SpecGlossMap"))
				SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
			else if (workflowMode == WorkflowMode.Metallic && material.HasProperty("_MetallicGlossMap"))
				SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
			SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
			SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

			bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(material.GetColor("_EmissionColor"));
			SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

			// Setup lightmap emissive flags
			MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
			if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
			{
				flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
				if (!shouldEmissionBeEnabled)
					flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

				material.globalIlluminationFlags = flags;
			}

			SetKeyword(material, "_VERTEXCOLOR", material.GetFloat("_IntensityVC") > 0f);
		}

		public static bool ShouldEmissionBeEnabled(Color color)
		{
			return color.grayscale > (0.1f / 255.0f);
		}

		public static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword(keyword);
			else
				m.DisableKeyword(keyword);
		}
	}
}

