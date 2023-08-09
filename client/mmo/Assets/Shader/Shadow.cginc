// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef H3DSHADOW_INCLUDED
#define H3DSHADOW_INCLUDED

#if !defined (SHADOWS_SCREEN) && !defined(SHADOWS_CUBE)
    UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
    float4 _ShadowMapTexture_TexelSize;
    #define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
#endif
    sampler2D _CloudTex;
    float4 _CloudData;

    // Configuration


    // Should receiver plane bias be used? This estimates receiver slope using derivatives,
    // and tries to tilt the PCF kernel along it. However, since we're doing it in screenspace
    // from the depth texture, the derivatives are wrong on edges or intersections of objects,
    // leading to possible shadow artifacts. So it's disabled by default.
    // See also UnityGetReceiverPlaneDepthBias in UnityShadowLibrary.cginc.
    // #define UNITY_USE_RECEIVER_PLANE_BIAS
#if defined (H3DSHADOWS_SPLIT_SPHERES)
    #define H3D_USE_RECEIVER_PLANE_BIAS
#endif

    #include "UnityShadowLibrary.cginc"

    // Blend between shadow cascades to hide the transition seams?
    #define UNITY_USE_CASCADE_BLENDING 0
    #define UNITY_CASCADE_BLEND_DISTANCE 0.1

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------
    UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
    // sizes of cascade projections, relative to first one
    float4 unity_ShadowCascadeScales;

    //
    // Keywords based defines
    //
#if defined (H3DSHADOWS_SPLIT_SPHERES)
#define GET_CASCADE_WEIGHTS(wpos, z)    getCascadeWeights_splitSpheres(wpos)
#else
#define GET_CASCADE_WEIGHTS(wpos, z)    getCascadeWeights( wpos, z )
#endif

#if defined (H3DSHADOWS_SINGLE_CASCADE)
#define GET_SHADOW_COORDINATES(wpos,cascadeWeights) getShadowCoord_SingleCascade(wpos)
#else
#define GET_SHADOW_COORDINATES(wpos,cascadeWeights) getShadowCoord(wpos,cascadeWeights)
#endif

/**
 * Gets the cascade weights based on the world position of the fragment.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
    inline fixed4 getCascadeWeights(float3 wpos, float z)
    {
        fixed4 zNear = float4(z >= _LightSplitsNear);
        fixed4 zFar = float4(z < _LightSplitsFar);
        fixed4 weights = zNear * zFar;
#if defined (H3DSHADOWS_DOUBLE_CASCADE)
        return fixed4(weights.xy, 0, 0);
#else
        return weights;
#endif
    }

    /**
     * Gets the cascade weights based on the world position of the fragment and the poisitions of the split spheres for each cascade.
     * Returns a float4 with only one component set that corresponds to the appropriate cascade.
     */
    inline fixed4 getCascadeWeights_splitSpheres(float3 wpos)
    {
        float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
        float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
        float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
        float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
        float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));
        fixed4 weights = float4(distances2 < unity_ShadowSplitSqRadii);
        weights.yzw = saturate(weights.yzw - weights.xyz);
#if defined (H3DSHADOWS_DOUBLE_CASCADE)
        return fixed4(weights.xy, 0, 0);
#else
        return weights;
#endif
    }

    /**
     * Returns the shadowmap coordinates for the given fragment based on the world position and z-depth.
     * These coordinates belong to the shadowmap atlas that contains the maps for all cascades.
     */
    inline float4 getShadowCoord(float4 wpos, fixed4 cascadeWeights)
    {
        float3 sc0 = mul(unity_WorldToShadow[0], wpos).xyz;
        float3 sc1 = mul(unity_WorldToShadow[1], wpos).xyz;
        float3 sc2 = mul(unity_WorldToShadow[2], wpos).xyz;
        float3 sc3 = mul(unity_WorldToShadow[3], wpos).xyz;
#if defined (H3DSHADOWS_DOUBLE_CASCADE)
        float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1], 1);
#else
        float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
#endif
#if defined(UNITY_REVERSED_Z)
        float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
        shadowMapCoordinate.z += noCascadeWeights;
#endif
        return shadowMapCoordinate;
    }

    /**
     * Same as the getShadowCoord; but optimized for single cascade
     */
    inline float4 getShadowCoord_SingleCascade(float4 wpos)
    {
        return float4(mul(unity_WorldToShadow[0], wpos).xyz, 0);
    }

    // -----------------------------
    //  Shadow helpers (5.6+ version)
    // -----------------------------
    // This version depends on having worldPos available in the fragment shader and using that to compute light coordinates.
    // if also supports ShadowMask (separately baked shadows for lightmapped objects)

    half H3D_SHADOW_ATTENUATION_BASE(float4 worldPos, half3 worldNormal)
    {
        half3 worldLightDir = normalize(UnityWorldSpaceLightDir(worldPos));

        half realtimeShadowAttenuation = 1.0f;

        float4 wpos = worldPos;
        float3 vpos = mul(unity_WorldToCamera, wpos).xyz;

        fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z);

        #ifdef H3D_USE_RECEIVER_PLANE_BIAS
            float H3DbiasMultiply = dot(cascadeWeights, unity_ShadowCascadeScales);
            float shadowCos = dot(normalize(worldNormal), worldLightDir);
            float shadowSine = 1 - shadowCos;// sqrt(1 - shadowCos * shadowCos);
            float normalBias = unity_LightShadowBias.z * shadowSine;
            wpos.xyz += H3DbiasMultiply * normalBias * normalize(worldNormal);

            vpos = mul(unity_WorldToCamera, wpos).xyz;
            cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z);
        #endif

        float4 coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);

        //directional realtime shadow
        #if defined (H3DSHADOWS_HARD)
            //1 tap hard shadow
            realtimeShadowAttenuation = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord);
            realtimeShadowAttenuation = lerp(_LightShadowData.r, 1.0, realtimeShadowAttenuation);
        #elif defined (H3DSHADOWS_SOFT)
            float3 receiverPlaneDepthBias = 0.0;
            #ifdef UNITY_USE_RECEIVER_PLANE_BIAS
                // Reveiver plane depth bias: need to calculate it based on shadow coordinate
                // as it would be in first cascade; otherwise derivatives
                // at cascade boundaries will be all wrong. So compute
                // it from cascade 0 UV, and scale based on which cascade we're in.
                float3 coordCascade0 = getShadowCoord_SingleCascade(wpos);
                float biasMultiply = dot(cascadeWeights, unity_ShadowCascadeScales);
                receiverPlaneDepthBias = UnityGetReceiverPlaneDepthBias(coordCascade0.xyz, biasMultiply);
            #endif

            // #if defined(SHADER_API_MOBILE)
            realtimeShadowAttenuation = UnitySampleShadowmap_PCF3x3(coord, receiverPlaneDepthBias);
            realtimeShadowAttenuation = lerp(_LightShadowData.r, 1.0f, realtimeShadowAttenuation);

            // Blend between shadow cascades if enabled
            //
            // Not working yet with split spheres, and no need when 1 cascade
            #if UNITY_USE_CASCADE_BLENDING && !defined(H3DSHADOWS_SPLIT_SPHERES) && !defined(H3DSHADOWS_SINGLE_CASCADE)
                half4 z4 = (float4(vpos.z,vpos.z,vpos.z,vpos.z) - _LightSplitsNear) / (_LightSplitsFar - _LightSplitsNear);
                half alpha = dot(z4 * cascadeWeights, half4(1,1,1,1));

                UNITY_BRANCH
                    if (alpha > 1 - UNITY_CASCADE_BLEND_DISTANCE)
                    {
                        // get alpha to 0..1 range over the blend distance
                        alpha = (alpha - (1 - UNITY_CASCADE_BLEND_DISTANCE)) / UNITY_CASCADE_BLEND_DISTANCE;

                        // sample next cascade
                        cascadeWeights = fixed4(0, cascadeWeights.xyz);
                        coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);

            #ifdef UNITY_USE_RECEIVER_PLANE_BIAS
                        biasMultiply = dot(cascadeWeights,unity_ShadowCascadeScales);
                        receiverPlaneDepthBias = UnityGetReceiverPlaneDepthBias(coordCascade0.xyz, biasMultiply);
            #endif

                        half shadowNextCascade = UnitySampleShadowmap_PCF3x3(coord, receiverPlaneDepthBias);
                        shadowNextCascade = lerp(_LightShadowData.r, 1.0f, shadowNextCascade);
                        realtimeShadowAttenuation = lerp(realtimeShadowAttenuation, shadowNextCascade, alpha);
                    }
            #endif
        #endif

        return realtimeShadowAttenuation;
    }


    half H3D_SHADOW_ATTENUATION(float4 worldPos, half3 worldNormal)
    {
        half realtimeShadowAttenuation = H3D_SHADOW_ATTENUATION_BASE(worldPos, worldNormal);
#if defined (H3DSHADOWS_HARD) || defined (H3DSHADOWS_SOFT)
        half cloud = tex2D(_CloudTex, worldPos.xz * _CloudData.z + _CloudData.xy * _Time.x).r;
        cloud = lerp(1.0, cloud, _CloudData.w);
        realtimeShadowAttenuation = min(realtimeShadowAttenuation, cloud);
#endif
        return realtimeShadowAttenuation;
    }

    float4 H3DApplyLinearShadowBias(float4 clipPos, half3 worldNormal, half3 lightDir)
    {
        // For point lights that support depth cube map, the bias is applied in the fragment shader sampling the shadow map.
        // This is because the legacy behaviour for point light shadow map cannot be implemented by offseting the vertex position
        // in the vertex shader generating the shadow map.
        #if !(defined(SHADOWS_CUBE) && defined(SHADOWS_CUBE_IN_DEPTH_TEX))
            #if defined(UNITY_REVERSED_Z)
                // We use max/min instead of clamp to ensure proper handling of the rare case
                // where both numerator and denominator are zero and the fraction becomes NaN.
                clipPos.z += max(-1, min(unity_LightShadowBias.x / clipPos.w, 0)) * saturate(1.0 - dot(worldNormal, lightDir));
            #else
                clipPos.z += saturate(unity_LightShadowBias.x / clipPos.w) * saturate(1.0 - dot(worldNormal, lightDir));
            #endif
        #endif

        #if defined(UNITY_REVERSED_Z)
            float clamped = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
        #else
            float clamped = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
        #endif
        clipPos.z = lerp(clipPos.z, clamped, unity_LightShadowBias.y);
        return clipPos;
    }
#endif
