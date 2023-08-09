Shader "H3D/Scene/Homeland/Ground"
{
    Properties 
    {
//    	_MainTex("Mesh蒙版", 2D) = "white" {}
//    	_LightDir("灯光方向", vector) = (-0.48, 0.96, -0.05, 1)
		_MainTex("混合蒙版", 2D) = "black" {}
		_Weight("混合锐度", Range(0.01 , 0.5)) = 0.1
    	_DetailColorNoise("细节颜色蒙版", 2D) = "white" {}
    	_DetailNoise01Tiling("细节颜色蒙版平铺值01", Float) = 1
    	_DetailNoise02Tiling("细节颜色蒙版平铺值02", Float) = 1
    	[HDR]_DetailColor01("色彩变化01", Color) = (1,1,1,1)
    	[HDR]_DetailColor02("色彩变化02", Color) = (1,1,1,1)
    	_ShadowColor("阴影颜色", Color) = (0,0,0,0)
    	_DarkColor("暗部颜色", Color) = (0,0,0,0)
		[Space(40)]

		[HDR]_Material01Col("材质1颜色", Color) = (1,1,1,1)
		_Splat0("材质1颜色图", 2D) = "white" {}
    	_Material01Tiling("材质1平铺值", float) = 1
		_Material01Gloss("材质1粗糙度", float) = 0.5
		_SpecularCol01("材质1高光颜色", Color) = (0,0,0,0)
		[Space(40)]

		[HDR]_Material02Col("材质2颜色", Color) = (1,1,1,1)
		_Splat1("材质2颜色图", 2D) = "white" {}
    	_Material02Tiling("材质2平铺值", float) = 1
		_Material02Gloss("材质2粗糙度", float) = 0.5
		_SpecularCol02("材质2高光颜色", Color) = (0,0,0,0)
		[Space(40)]

		[HDR]_Material03Col("材质3颜色", Color) = (1,1,1,1)
		_Splat2("材质3颜色图", 2D) = "white" {}
    	_Material03Tiling("材质3平铺值", float) = 1
		_Material03Gloss("材质3粗糙度", float) = 0.5
		_SpecularCol03("材质3高光颜色", Color) = (0,0,0,0)
		[Space(40)]
		
		[HDR]_Material04Col("材质4颜色", Color) = (1,1,1,1)
		_Splat3("材质4颜色图", 2D) = "white" {}
    	_Material04Tiling("材质4平铺值", float) = 1
		_Material04Gloss("材质4粗糙度", float) = 0.5
		_SpecularCol04("材质4高光颜色", Color) = (0,0,0,0)

    	
    } 
	
	SubShader
	{
//		Tags {"Queue" = "Geometry-50" "RenderType" = "Opaque"}
		// LOD 100
		Pass
		{
			
//			Tags { "LightMode" = "ForwardBase" }
			Tags
            {
                "LightMode"="ForwardBase" "Queue"="Geometry-50" "RenderType"="Opaque"
            }
			// Blend One Zero
			//ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma skip_variants SHADOWS_SCREEN

			#pragma multi_compile_fwdbase
			#pragma multi_compile __ H3DSHADOWS_HARD H3DSHADOWS_SOFT
            #pragma multi_compile __ H3DSHADOWS_SPLIT_SPHERES
            #pragma multi_compile __ H3DSHADOWS_SINGLE_CASCADE H3DSHADOWS_DOUBLE_CASCADE
            
			// #pragma target 2.0
			
			#pragma multi_compile_fog
			
			// #pragma multi_complie_instancing
			#include "UnityCG.cginc"
			// #include "UnityStandardUtils.cginc"
			// #include "Lighting.cginc"
			#include "UnityLightingCommon.cginc"
            #include "Shadow.cginc"
			
			sampler2D _Splat0, _Splat1, _Splat2, _Splat3, _MainTex, _DetailColorNoise;
			half4 _Material01Col, _Material02Col, _Material03Col, _Material04Col, _LightDir;
			half4 _SpecularCol01, _SpecularCol02, _SpecularCol03, _SpecularCol04, _DetailColor01, _DetailColor02, _ShadowColor, _DarkColor;
			half _Material01Gloss, _Material02Gloss, _Material03Gloss, _Material04Gloss, _Weight, _DetailNoise01Tiling, _DetailNoise02Tiling, _Material01Tiling;
			half _Material02Tiling, _Material03Tiling, _Material04Tiling;

			struct a2v
            {
                float4 vertex : POSITION;
                half2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
                half4 tangent : TANGENT;
            };

            struct v2f
            {
            	float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
				half3 worldNormal : TEXCOORD2;
                UNITY_FOG_COORDS(3)
				float4 uv12 : TEXCOORD4;
				float4 uv34 : TEXCOORD5;
				float4 DetailUV12 : TEXCOORD6;
            };

			v2f vert (a2v v)
			{
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld,v.vertex);
				o.uv = v.texcoord;
                UNITY_TRANSFER_FOG(o, o.pos);
				o.uv12.xy = o.worldPos.xz * rcp(_Material01Tiling);
				o.uv12.zw = o.worldPos.xz * rcp(_Material02Tiling);
				o.uv34.xy = o.worldPos.xz * rcp(_Material03Tiling);
				o.uv34.zw = o.worldPos.xz * rcp(_Material04Tiling);
				o.DetailUV12.xy = o.worldPos.xz * rcp(_DetailNoise01Tiling);
				o.DetailUV12.zw = o.worldPos.xz * rcp(_DetailNoise02Tiling);
                return o;
            }
			
			half4 Blend(half4 mask, half M1, half M2, half M3, half M4)
			{
				half4 MaskBlend;
				MaskBlend.r = M1 * mask.r;
				MaskBlend.g = M2 * mask.g;
				MaskBlend.b = M3 * mask.b;
				MaskBlend.a = M4 * mask.a;
				half ma = max(MaskBlend.r, max(MaskBlend.g, max(MaskBlend.b, MaskBlend.a)));
				MaskBlend = max(MaskBlend - ma + _Weight, 0) * mask;
				return MaskBlend * rcp(MaskBlend.r + MaskBlend.g + MaskBlend.b + MaskBlend.a);
			}

			half4 frag(v2f i) : SV_Target
			{
				float4 worldPos = i.worldPos;
				float2 UV01 = i.uv12.xy;// worldPos.xz / _Material01Tiling;
				float2 UV02 = i.uv12.zw;// worldPos.xz / _Material02Tiling;
				float2 UV03 = i.uv34.xy;// worldPos.xz / _Material03Tiling;
				float2 UV04 = i.uv34.zw;// worldPos.xz / _Material04Tiling;
				// half3 worldLight = normalize(_LightDir.xyz);
				half3 worldLight = normalize(_WorldSpaceLightPos0);
				float3 DetailRange = UnityWorldSpaceViewDir(worldPos.xyz);
				half3 viewDir = normalize(DetailRange);
				half DetailMask = 1 - saturate((abs(DetailRange.x) + abs(DetailRange.z)) * 0.005); // 近似处理// saturate(length(DetailRange.xz) / 150);
				// return DetailMask;
				
				//混合蒙版
				half4 Mask = tex2D(_MainTex, i.uv);
				half4 diffuse01 = tex2D(_Splat0, UV01) * _Material01Col;
				half4 diffuse02 = tex2D(_Splat1, UV02) * _Material02Col;
				half4 diffuse03 = tex2D(_Splat2, UV03) * _Material03Col;
				half4 diffuse04 = tex2D(_Splat3, UV04) * _Material04Col;
				
				half4 blend = Blend(Mask, diffuse01.a, diffuse02.a, diffuse03.a, diffuse04.a);
				// return blend;
				//草地纹理色彩变化
				float2 DetailUV01 = i.DetailUV12.xy;// worldPos.xz / _DetailNoise01Tiling;
				float2 DetailUV02 = i.DetailUV12.zw;// worldPos.xz / _DetailNoise02Tiling;
				half DetailMask01 = tex2D(_DetailColorNoise, DetailUV01);
				half DetailMask02 = pow(1.0 - tex2D(_DetailColorNoise, DetailUV02), 5);
				DetailMask01 = smoothstep(0.0, 0.7, DetailMask01);
				diffuse01 = lerp(diffuse01, diffuse01 * _DetailColor01, DetailMask01 * DetailMask);
				diffuse01 = lerp(diffuse01, diffuse01 * _DetailColor02, DetailMask02 * DetailMask);
				//  固有色
				half3 diffuseCol = diffuse01 * blend.r + diffuse02 * blend.g + diffuse03 * blend.b + diffuse04 * blend.a;

				
				//光照和阴影
				i.worldNormal = normalize(i.worldNormal);
				half shadow = H3D_SHADOW_ATTENUATION(worldPos, i.worldNormal);
				// return shadow;
				
				// float3 SHlight = min(ShadeSHPerVertex(i.worldNormal, 1), shadow);
				// float3 SHlight = ShadeSH9(float4(i.worldNormal, 1));
				// return float4(SHlight, 1);
				half3 halfDir = normalize(viewDir + worldLight);
				half NdotH = saturate(dot(normalize(i.worldNormal), halfDir));
				half Lambert = dot(normalize(i.worldNormal), worldLight);
				// Lambert = pow(Lambert * 1.5, 3);
				// return pow(Lambert * 1.5, 3);
				half3 specular01 = pow(NdotH, _Material01Gloss) * _SpecularCol01 * blend.r;
				half3 specular02 = pow(NdotH, _Material02Gloss) * _SpecularCol02 * blend.g;
				half3 specular03 = pow(NdotH, _Material03Gloss) * _SpecularCol03 * blend.b;
				half3 specular04 = pow(NdotH, _Material04Gloss) * _SpecularCol04 * blend.a;
				half3 specularAll = specular01 + specular02 + specular03 + specular04;
				// return float4(specularAll, 1);
				half3 FinCol = diffuseCol + specularAll * _LightColor0 * shadow * saturate(Lambert)/*  + SHlight */;
				half3 darkCol = _DarkColor * FinCol;
				half3 shadowCol = _ShadowColor * FinCol;
				FinCol = lerp(darkCol, FinCol, Lambert);
				FinCol = lerp(shadowCol, FinCol, shadow);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, FinCol, unity_FogColor);
				return half4(FinCol, 1);
			}
			ENDCG
		} 

		Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            #include "Shadow.cginc"

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
            #ifdef H3D_USE_RECEIVER_PLANE_BIAS
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 wpos = mul(unity_ObjectToWorld, v.vertex);
                half3 wnormal = UnityObjectToWorldNormal(v.normal);
                half3 lightDir = normalize(UnityWorldSpaceLightDir(wpos));
                o.pos = H3DApplyLinearShadowBias(o.pos, wnormal, lightDir);
                // o.pos = UnityApplyLinearShadowBias(o.pos);
            #else
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
            #endif
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
		
	}
        //CustomEditor "CustomGroundShaderGUI"
}