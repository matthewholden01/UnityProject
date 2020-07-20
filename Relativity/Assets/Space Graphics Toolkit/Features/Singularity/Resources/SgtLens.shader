Shader "Hidden/SgtLens"
{
	Properties
	{
	}
	SubShader
	{
		Tags
		{
			"Queue"           = "Transparent"
			"RenderType"      = "Opaque"
			"IgnoreProjector" = "True"
		}
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha, Zero One
			Cull Front
			Lighting Off
			ZWrite Off
			
			CGPROGRAM
				#pragma vertex Vert
				#pragma fragment Frag
				#include "UnityCG.cginc"
				
				samplerCUBE _Texture;
				float       _WarpOuter;
				float       _WarpStrength;
				float       _HoleSize;
				float       _HoleEdge;
				float       _FadeOuter;
				
				struct a2v
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
				};
				
				struct v2f
				{
					float4 vertex      : SV_POSITION;
					float3 worldNormal : TEXCOORD0;
					float3 worldRefl   : TEXCOORD1;
				};
				
				struct f2g
				{
					fixed4 color : SV_TARGET;
				};
				
				void Vert(a2v i, out v2f o)
				{
					o.vertex      = UnityObjectToClipPos(i.vertex);
					o.worldNormal = mul((float3x3)unity_ObjectToWorld, i.normal);
					o.worldRefl   = mul(unity_ObjectToWorld, i.vertex).xyz - _WorldSpaceCameraPos;
				}

				float3 AxisAngle(float3 p, float3 axis, float angle)
				{
					float3 c = cos(angle); return p * c + cross(axis, p) * sin(angle) + axis * dot(p, axis) * (1.0f - c);
				}
				
				void Frag(v2f i, out f2g o)
				{
					i.worldNormal = normalize(i.worldNormal);
					i.worldRefl   = normalize(i.worldRefl);

					float3 axis = cross(i.worldNormal, i.worldRefl);
					float d = saturate(dot(i.worldNormal, i.worldRefl));
					float f = d;
					d = pow(d, _WarpOuter);

					float3 normal = AxisAngle(i.worldRefl, axis, d * _WarpStrength);

					o.color = texCUBE(_Texture, normal);

					// Fade hole
					o.color.rgb *= saturate((_HoleSize - f) * _HoleEdge);

					// Fade edge
					o.color.a *= saturate(1.0f - pow(saturate(1.0f - f), _FadeOuter));
				}
			ENDCG
		} // Pass
	} // SubShader
} // Shader