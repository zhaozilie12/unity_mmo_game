Shader "MMO/Grass"
{
	Properties
	{
		_GrassTex("草贴图", 2D) = "white" {}
		_Desaturate("饱和度", Range(0 , 1)) = 0
		_UVAO("UVAO", Range(0 , 1)) = 0.802663
		_TopColor("顶部颜色", Color) = (0.4858807,0.5849056,0.3062477,1)
		_DownColor("底部颜色", Color) = (0.3956266,0.4811321,0.2382966,1)
		_ShadowColor("阴影颜色", Color) = (0.6981132,0.6981132,0.6981132,1)
		_LightDir("灯光方向", Vector) = (0,1,1,0)

		[Space(40)]
		_DetailColorNoise("细节颜色蒙版", 2D) = "white" {}
		[HDR]_Detail01Color("色彩变化01", Color) = (1,0.9323361,0.5235849,1)
		[HDR]_Detail02Color("色彩变化02", Color) = (0.5801887,0.7201115,1,1)
		_DetailNoise01Tiling("细节颜色蒙版平铺值01", Float) = 0
		_DetailNoise02Tiling("细节颜色蒙版平铺值02", Float) = 0

		[Space(40)]
		_WindNoiseTex("风场噪波图", 2D) = "white" {}
		_WindNoiseTiling("风场噪波图平铺值", Float) = 25
		_WindIntensity("风场强度", Float) = 0
		_WindSpeed("风场速度", Float) = 0

	}
		CGINCLUDE
		#pragma target 3.0
		#include "UnityCG.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardBRDF.cginc"
		#include "UnityLightingCommon.cginc"
		#include "UnityStandardUtils.cginc"
		#include "Shadow.cginc"

		sampler2D _WindNoiseTex;
		half _WindNoiseTiling;
		half _WindSpeed;
		half _WindIntensity;
		half4 _ShadowColor;
		half4 _TopColor;
		half4 _DownColor;
		sampler2D _GrassTex;
		half _Desaturate;
		half _UVAO;
		half4 _Detail01Color;
		sampler2D _DetailColorNoise;
		half _DetailNoise01Tiling;
		half4 _Detail02Color;
		half _DetailNoise02Tiling;
		float _GrassLODFadeDistance;
		ENDCG
		SubShader
		{
			Tags
			{
				"Queue" = "Geometry" "RenderType" = "TransparentCutOut"
			}

			Blend Off
			AlphaToMask Off
			Cull Off

			Pass
			{
				Name "GRASS PreZ"
				Tags
				{
					"LightMode" = "ForwardBase"
				}
				ColorMask 0
				ZWrite On
				ZTest LEqual
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing

				struct a2v
				{
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 pos : SV_POSITION;
					float4 worldPos : TEXCOORD0;
					float2 uv : TEXCOORD1;
				};


				v2f vert(a2v v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					//细节颜色
					o.uv = v.texcoord;
					float4 detailWpos = mul(unity_ObjectToWorld, v.vertex);
					//风场
					float2 windUV = detailWpos.xz * rcp(_WindNoiseTiling) + _WindSpeed * _Time.y;
					//风场关联的高光蒙版
					float offset = tex2Dlod(_WindNoiseTex, float4(windUV, 0.0, 0.0));
					//顶点位移
					float vertMoveZ = (0.6 - offset) * _WindIntensity * smoothstep(0.2, 0.8, o.uv.y);
					v.vertex.z += vertMoveZ;

					o.pos = UnityObjectToClipPos(v.vertex);

					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					return o;
				}

				const float4x4 _thresholdMatrix =
				{ 1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
				  13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
				   4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
				  16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
				};
				const float4x4 _rowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };

				void ApplyDitherCrossFade(float2 vpos, float fade)
				{
					clip(fade - _thresholdMatrix[fmod(vpos.x, 4)] * _rowAccess[fmod(vpos.y, 4)]);
				}

				half4 frag(v2f i) : SV_Target
				{
					half4 BaseCol = tex2D(_GrassTex, i.uv);
					clip(BaseCol.a - 0.5);

					//fade				
					float dis = distance(i.worldPos, _WorldSpaceCameraPos);
					float fade = dis - _GrassLODFadeDistance;
					fade = step(fade, 0) + step(0, fade) * (1 - fade);
					//ApplyDitherCrossFade(i.pos.xy, fade);

					return 0;
				}
				ENDCG
			}
			Pass
			{
				Name "GRASS Forward"
				Tags
				{
					"LightMode" = "ForwardBase"
				}
				ColorMask RGB
				ZWrite Off
				ZTest Equal
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#pragma skip_variants SHADOWS_SCREEN

				#pragma multi_compile_instancing
				#pragma multi_compile_fwdbase
				#pragma multi_compile __ H3DSHADOWS_HARD H3DSHADOWS_SOFT
				#pragma multi_compile __ H3DSHADOWS_SPLIT_SPHERES
				#pragma multi_compile __ H3DSHADOWS_SINGLE_CASCADE H3DSHADOWS_DOUBLE_CASCADE

				struct a2v
				{
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 worldPos : TEXCOORD1;
					half2 detailMask:TEXCOORD2;
					UNITY_FOG_COORDS(3)
				};


				v2f vert(a2v v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.uv = v.texcoord;
					//细节颜色
					float4 detailWpos = mul(unity_ObjectToWorld, v.vertex);
					float2 DetailUV01 = detailWpos.xz * rcp(_DetailNoise01Tiling);
					float2 DetailUV02 = detailWpos.xz * rcp(_DetailNoise02Tiling);
					half2 detailmask = half2(1, 1);
					detailmask.x = tex2Dlod(_DetailColorNoise, float4(DetailUV01, 0.0, 0.0));
					half pow = 1.0 - tex2Dlod(_DetailColorNoise, float4(DetailUV02, 0.0, 0.0));
					half pow2 = pow * pow;
					detailmask.y = pow2 * pow2 * pow;
					o.detailMask = detailmask;
					//风场
					float2 windUV = detailWpos.xz * rcp(_WindNoiseTiling) + _WindSpeed * _Time.y;
					//风场关联的高光蒙版
					float offset = tex2Dlod(_WindNoiseTex, float4(windUV, 0.0, 0.0));
					//顶点位移
					float vertMoveZ = (0.6 - offset) * _WindIntensity * smoothstep(0.2, 0.8, v.texcoord.y);
					v.vertex.z += vertMoveZ;

					o.pos = UnityObjectToClipPos(v.vertex);
					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.worldPos = worldPos;


					UNITY_TRANSFER_FOG(o, o.vertex);
					return o;
				}

				half4 frag(v2f i) : SV_Target
				{
					float4 worldPos = i.worldPos;
					//固有色
					half4 BaseCol = tex2D(_GrassTex, i.uv);
					half3 Diffuse = BaseCol.rgb;
					//固有色
					half DesaturateCol = dot(Diffuse, half3(0.299, 0.587, 0.114));
					Diffuse = lerp(Diffuse, DesaturateCol.xxx, _Desaturate);
					Diffuse = lerp(Diffuse, i.uv.yyy, _UVAO);
					half3 MixCol = lerp(_TopColor * _DownColor, _TopColor, Diffuse);

					//色彩变化
					half3 DetailRange = _WorldSpaceCameraPos.xyz - worldPos.xyz;
					half DetailMask = 1.0 - saturate((abs(DetailRange.x) + abs(DetailRange.z)) * 0.005); // 近似处理
					half3 DetailColor01 = lerp(MixCol, MixCol * _Detail01Color, i.detailMask.x * DetailMask);
					half3 DetailColor02 = lerp(DetailColor01, MixCol * _Detail02Color, i.detailMask.y * DetailMask);

					half shadow = H3D_SHADOW_ATTENUATION(worldPos, half3(0, 1, 0));

					half3 FinCol = lerp(DetailColor02 * _ShadowColor, DetailColor02, shadow);
					UNITY_APPLY_FOG_COLOR(i.fogCoord, FinCol, unity_FogColor);
					return half4(FinCol, 1.0);
				}
				ENDCG
			}
		}
}
			