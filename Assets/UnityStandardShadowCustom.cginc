// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_STANDARD_SHADOW_INCLUDED
#define UNITY_STANDARD_SHADOW_INCLUDED

// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)


#define _ALPHATEST_ON 1


#if (defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
#define UNITY_STANDARD_USE_DITHER_MASK 1
#endif

// Need to output UVs in shadow caster, since we need to sample texture and do clip/dithering based on it
#if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define UNITY_STANDARD_USE_SHADOW_UVS 1
#endif

// Has a non-empty shadow caster output struct (it's an error to have empty structs on some platforms...)
#if !defined(V2F_SHADOW_CASTER_NOPOS_IS_EMPTY) || defined(UNITY_STANDARD_USE_SHADOW_UVS)
#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef UNITY_STEREO_INSTANCING_ENABLED
#define UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT 1
#endif


half4 _Color;
half _Cutoff;
sampler2D _MainTex;
float4 _MainTex_ST;
#ifdef UNITY_STANDARD_USE_DITHER_MASK
sampler3D   _DitherMaskLOD;
#endif

// Handle PremultipliedAlpha from Fade or Transparent shading mode
half4 _SpecColor;
half _Metallic;
#ifdef _SPECGLOSSMAP
sampler2D   _SpecGlossMap;
#endif
#ifdef _METALLICGLOSSMAP
sampler2D   _MetallicGlossMap;
#endif

#if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
sampler2D   _ParallaxMap;
half        _Parallax;
#endif

// SHADOW_ONEMINUSREFLECTIVITY(): workaround to get one minus reflectivity based on UNITY_SETUP_BRDF_INPUT
#define SHADOW_JOIN2(a, b) a##b
#define SHADOW_JOIN(a, b) SHADOW_JOIN2(a,b)
#define SHADOW_ONEMINUSREFLECTIVITY SHADOW_JOIN(UNITY_SETUP_BRDF_INPUT, _ShadowGetOneMinusReflectivity)


//#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster
{
    V2F_SHADOW_CASTER_NOPOS
    UNITY_POSITION(pos);

    float2 tex : TEXCOORD0;
    int matIndex : TEXCOORD1;
};
//#endif

#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
struct VertexOutputStereoShadowCaster
{
    UNITY_VERTEX_OUTPUT_STEREO
};
#endif



// We have to do these dances of outputting SV_POSITION separately from the vertex shader,
// and inputting VPOS in the pixel shader, since they both map to "POSITION" semantic on
// some platforms, and then things don't go well.

VertexOutputShadowCaster vertShadowCasterCustom(appdata_full v, uint unity_InstanceID : SV_InstanceID)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputShadowCaster o;
    
    UNITY_INITIALIZE_OUTPUT(VertexOutputShadowCaster, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    if (unity_ObjectToWorld._22 < SCALE_CUTOFF)
    {
        v.vertex.xyz = float3(NaN, NaN, NaN);
        return o;
    }
    
    v = VertexAnimation(v, unity_ObjectToWorld._14_24_34, unity_ObjectToWorld._44);
	v = VertexRotation(v);
		
    o.tex = v.texcoord.xy;
    
    o.pos = UnityObjectToClipPos(v.vertex);
    
    o.matIndex = GetMaterialID(seed);

    TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos)

    return o;
}


half4 fragShadowCaster(VertexOutputShadowCaster vs) : SV_Target
{
    //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(vs);
    half alpha = UNITY_SAMPLE_TEX2DARRAY(_grassAlbedo, float3(vs.tex.xy, vs.matIndex)).a;
    
    #if defined(_ALPHATEST_ON)
        clip(alpha - ALPHA_CUTOFF);
    #endif

    SHADOW_CASTER_FRAGMENT(vs)
    
}

#endif 
