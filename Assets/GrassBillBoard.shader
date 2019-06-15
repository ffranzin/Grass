// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Nature/GrassBillBoard" 
{
	Properties 
	{
		_alphaCutoff("alphaCutoff", float) = 0.2
		_albedo("Albedo", 2D) = "white"

		[Toggle] _ShowLODDebug("_ShowLODDebug", int) = 0
		[Toggle] _ShowCollisionDebug("_ShowCollisionDebug", int) = 0
	}
	
	SubShader
	{ 
		Tags 
		{ 
			"Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
		}

		
		
		// ----------------------------------------------------------------------//
		// Shadow Caster
		// ----------------------------------------------------------------------//
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull off
			
			CGPROGRAM
			#pragma target 3.0
			 
			// -------------------------------------

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _PARALLAXMAP
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
		    #pragma instancing_options procedural:setup

			#pragma vertex vertShadowCasterCustom
			#pragma fragment fragShadowCaster


			#include "UnityCG.cginc"
			#include "UnityStandardConfig.cginc"
			#include "UnityStandardUtils.cginc"

			#include "GrassCommon.cginc"
			#include "UnityStandardShadowCustom.cginc"

			ENDCG
		}
		

		// ----------------------------------------------------------------------
		// Deferred
		// ----------------------------------------------------------------------
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" "Queue"="Geometry" "RenderType"="Opaque" }

			ZWrite On ZTest LEqual Cull off

			CGPROGRAM
			#pragma target 5.0
			#pragma exclude_renderers gles3 metal d3d11_9x xbox360 xboxone ps3 ps4 psp2 nomrt
            #pragma only_renderers d3d11

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile_prepassfinal
			#pragma multi_compile_instancing
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
		    #pragma instancing_options procedural:setup

			#pragma vertex MyVertexProgram
			#pragma fragment MyFragmentProgram

			#define _TANGENT_TO_WORLD

			#include "UnityStandardCore.cginc"
		    #include "UnityCG.cginc"
			#include "UnityPBSLighting.cginc"

			#include "GrassCommon.cginc"
			
			int _ShowLODDebug;
			int _ShowCollisionDebug;
			sampler _albedo;

			struct StructureVS
			{
				 UNITY_POSITION(pos);
				 float3 normal		: TEXCOORD1;
				 float4 debug		: TEXCOORD2;
				 float4 tex			: TEXCOORD3;
				 half3 eyeVec		: TEXCOORD4;
				 int matIndex		: TEXCOORD5;
				 float colorInf		: TEXCOORD11;
				 
				 half4 ambientOrLightmapUV           : TEXCOORD6;

				 half4 tangentToWorldAndPackedData[3]: TEXCOORD7;

				 UNITY_VERTEX_INPUT_INSTANCE_ID 
				 UNITY_VERTEX_OUTPUT_STEREO
			};

			StructureVS MyVertexProgram(appdata_full v, uint unity_InstanceID : SV_InstanceID)
			{
				StructureVS vs;
				UNITY_INITIALIZE_OUTPUT(StructureVS, vs);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(vs);
				UNITY_SETUP_INSTANCE_ID(v);

				if(setCulled)
				{
					v.vertex = float4(NaN,NaN,NaN,NaN);
					return vs;
				}

				float4 posWorld = mul(unity_ObjectToWorld, v.vertex);

				//v = VertexAnimation(v, unity_ObjectToWorld._14_24_34, unity_ObjectToWorld._44);
				v = VertexRotation(v);

				vs.colorInf = unity_ObjectToWorld._11;

				vs.pos = UnityObjectToClipPos(v.vertex);
				
				vs.tex = float4(v.texcoord.xy, 0, 0 );
				vs.eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos.xyz);
				vs.ambientOrLightmapUV = 0;

				vs.debug = collisionSampleDebug;

				vs.normal = UnityObjectToWorldNormal(v.normal) ;
				vs.normal *= (dot(vs.eyeVec, vs.normal) > 0 ? -1 : 1);
				//float3 forward = UNITY_MATRIX_IT_MV[2].xyz;

				//float teste = dot(UnityObjectToWorldNormal(v.normal), forward);

				//if(teste > 0.95) vs.debug = float4(1,0,0,1);

				//vs.normal = UnityObjectToWorldNormal(v.normal) ;
				//vs.normal *= (dot(vs.eyeVec, vs.normal) > 0 ? -1 : 1);

				//vs.normal = UnityObjectToWorldNormal(float3(0,1,0));//  * (dot(vs.eyeVec, normalWorld) > 0 ? -1 : 1);;
				//vs.normal =  float3(0, 1, 0);// * (dot(vs.eyeVec, vs.normal) > 0 ? -1 : 1);;

				//----------------------------------------------------------------------------------------
				// TBN definition 
				//----------------------------------------------------------------------------------------
				
				//require world pos in fragment shader and pack worldPos along tangent
				#if UNITY_REQUIRE_FRAG_WORLDPOS && UNITY_PACK_WORLDPOS_WITH_TANGENT
					vs.tangentToWorldAndPackedData[0].w = posWorld.x;
					vs.tangentToWorldAndPackedData[1].w = posWorld.y;
					vs.tangentToWorldAndPackedData[2].w = posWorld.z;
				#else
					//vs.worldPos = posWorld.xyz;
				#endif
				
				
				#ifdef _TANGENT_TO_WORLD
					float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
					float3x3 tangentToWorld = CreateTangentToWorldPerVertex(vs.normal, tangentWorld.xyz, tangentWorld.w);

					vs.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
					vs.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
					vs.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
				#else
					vs.tangentToWorldAndPackedData[0].xyz = 0;
					vs.tangentToWorldAndPackedData[1].xyz = 0;
					vs.tangentToWorldAndPackedData[2].xyz = vs.normal;
				#endif
				

				/*
				#ifdef LIGHTMAP_ON
					vs.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#elif UNITY_SHOULD_SAMPLE_SH
					vs.ambientOrLightmapUV.rgb = ShadeSHPerVertex (vs.normal, vs.ambientOrLightmapUV.rgb);
				#endif
				#ifdef DYNAMICLIGHTMAP_ON
					vs.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif
				*/
				return vs;
			}
			
			
			half3 GetWorldNormal(float2 i_tex, uint matIndex, half4 tangentToWorld[3])
			{
				float3 tangent	= tangentToWorld[0].xyz;
				float3 binormal = tangentToWorld[1].xyz;
				float3 normal	= tangentToWorld[2].xyz;

				#if UNITY_TANGENT_ORTHONORMALIZE
					normal  = NormalizePerPixelNormal(normal);
					tangent = normalize (tangent - normal * dot(tangent, normal)); // ortho-normalize Tangent

					half3 newB = cross(normal, tangent); // recalculate Binormal
					binormal = newB * sign (dot (newB, binormal));
				#endif

				float3 normalTangent = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_grassNormal, float3(i_tex, matIndex)));
				float3 normalWorld = normalize(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z);

				return normalWorld;
			}

			void MyFragmentProgram (StructureVS vs,
						out half4 outGBuffer0 : SV_Target0,
						out half4 outGBuffer1 : SV_Target1,
						out half4 outGBuffer2 : SV_Target2,
						out half4 outEmission : SV_Target3
						#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
						,out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
						#endif
						)
			{
				//UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(vs);
				//----------------------------------------------------------------------------------------
				// CutOff
				//----------------------------------------------------------------------------------------
				half4 albedo = tex2D(_albedo, vs.tex.xy * 0.98);//  UNITY_SAMPLE_TEX2DARRAY(_grassAlbedo, float3(vs.tex.xy, vs.matIndex));
				
				clip(albedo.a - ALPHA_CUTOFF);

				albedo *= clamp(2 - vs.colorInf, 0.6, 0.8);
				
				//----------------------------------------------------------------------------------------
				// Albedo
				//----------------------------------------------------------------------------------------

				FragmentCommonData s = (FragmentCommonData)0;
				s.eyeVec = vs.eyeVec;
				s.normalWorld = vs.normal; //GetWorldNormal(vs.tex.xy, 0, vs.tangentToWorldAndPackedData);
				s.diffColor = albedo;
				s.specColor = 0;
				s.oneMinusReflectivity = 1;
				s.smoothness = 0;

				//----------------------------------------------------------------------------------------
				// Lighting
				//----------------------------------------------------------------------------------------
				half occlusion = 1;
				half atten = 1;
				
				UnityLight dummyLight = DummyLight();

				#if UNITY_ENABLE_REFLECTION_BUFFERS
					bool sampleReflectionsInDeferred = false;
				#else
					bool sampleReflectionsInDeferred = true;
				#endif

				UnityGI gi = FragmentGI(s, occlusion, vs.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

				s.diffColor = PreMultiplyAlpha (s.diffColor, 1, s.oneMinusReflectivity, /*out*/ s.alpha);

				half3 emissiveColor = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

				#ifdef _EMISSION
					emissiveColor += Emission(vs.tex.xy);
				#endif

				#ifndef UNITY_HDR_ON
					emissiveColor.rgb = exp2(-emissiveColor.rgb);
				#endif

				if(_ShowCollisionDebug)
					s.diffColor = vs.debug;

				if(_ShowLODDebug)
				{
					if(_CurrentLOD == 0)
						s.diffColor = float4(1, 0, 0, 1);
					else if(_CurrentLOD == 1)
						s.diffColor = float4(0, 1, 0, 1);
					else if(_CurrentLOD == 2)
						s.diffColor = float4(0, 0, 1, 1);
				}
				
				//----------------------------------------------------------------------------------------
				// Set G-buffers
				//----------------------------------------------------------------------------------------
				UnityStandardData data;
				data.diffuseColor   = s.diffColor;
				data.occlusion      = occlusion;
				data.specularColor  = s.specColor;
				data.smoothness     = s.smoothness;
				data.normalWorld    = s.normalWorld;

				UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);
				outGBuffer2.a = 0;

				// Baked direct lighting occlusion if any
				#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
					outShadowMask = UnityGetRawBakedOcclusions(vs.ambientOrLightmapUV.xy, IN_WORLDPOS(vs));
				#endif

				outEmission = half4(emissiveColor,0);
			}
			
			ENDCG
		}
	}
	FallBack "Diffuse"
}
