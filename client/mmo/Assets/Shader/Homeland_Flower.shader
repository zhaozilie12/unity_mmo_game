Shader "H3D/Scene/Homeland/Flower"
{
    Properties
    {
        [Header(Opation)]
        [Toggle] _OffsetOff ("去除顶点动画", float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode ("剔除模式", Float) = 0
        _Clip ("剔除范围", Range(0, 1)) = 0.5
        [Header(MainTex)]
        [HDR]_MainColor ("主颜色（暗部颜色）", Color) = (1, 1, 1, 1)
        [NoScaleOffset]_MainTex ("颜色图", 2D) = "white" { }
        _Desaturate ("去色", Range(0, 1)) = 0
        _Non_AO ("Non_AO", Range(0, 1)) = 0
        [HDR]_ShadowColor ("阴影颜色", Color) = (0.4834016, 0.5806451, 0.6698113, 1)
        [Header(Wind)]
        _WindIntensity ("风场强度", Float) = 0.5
        _WindNoiseTiling3D ("风场扰动平铺值", Float) = 1
        _WindSpeed ("风场扰动速度", Float) = 0.3
        [Space(40)]
        [HideInInspector]_ColChange("边缘光溶解",Range(0, 1)) = 0
        [HideInInspector][HDR]_ColChangeCol("边缘光颜色", Color) = (0,0,0,1)
        [HideInInspector]AlphaValue("AlphaValue",Range(0,1)) = 1
        [HideInInspector]DitherAlphaValue("DitherAlphaValue",Range(0,1)) = 1
    }
    CGINCLUDE
    #pragma target 3.0

    float3 mod3D289(float3 x)
    {
        return x - floor(x * 0.0034602076124567) * 289.0;
    }

    float4 mod3D289(float4 x)
    {
        return x - floor(x * 0.0034602076124567) * 289.0;
    }

    float4 permute(float4 x)
    {
        return mod3D289((x * 34.0 + 1.0) * x);
    }

    float4 taylorInvSqrt(float4 r)
    {
        return 1.79284291400159 - r * 0.85373472095314;
    }

    float snoise(float3 v)
    {
        const float2 C = float2(0.1666666666666667, 0.3333333333333333);
        float3 i = floor(v + dot(v, C.yyy));
        float3 x0 = v - i + dot(i, C.xxx);
        float3 g = step(x0.yzx, x0.xyz);
        float3 l = 1.0 - g;
        float3 i1 = min(g.xyz, l.zxy);
        float3 i2 = max(g.xyz, l.zxy);
        float3 x1 = x0 - i1 + C.xxx;
        float3 x2 = x0 - i2 + C.yyy;
        float3 x3 = x0 - 0.5;
        i = mod3D289(i);
        float4 p = permute(
            permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x +
            float4(0.0, i1.x, i2.x, 1.0));
        float4 j = p - 49.0 * floor(p * 0.0204081632653061); // mod(p,7*7)
        float4 x_ = floor(j * 0.1428571428571429);
        float4 y_ = floor(j - 7.0 * x_); // mod(j,N)
        float4 x = (x_ * 2.0 + 0.5) * 0.1428571428571429 - 1.0;
        float4 y = (y_ * 2.0 + 0.5) * 0.1428571428571429 - 1.0;
        float4 h = 1.0 - abs(x) - abs(y);
        float4 b0 = float4(x.xy, y.xy);
        float4 b1 = float4(x.zw, y.zw);
        float4 s0 = floor(b0) * 2.0 + 1.0;
        float4 s1 = floor(b1) * 2.0 + 1.0;
        float4 sh = -step(h, 0.0);
        float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
        float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
        float3 g0 = float3(a0.xy, h.x);
        float3 g1 = float3(a0.zw, h.y);
        float3 g2 = float3(a1.xy, h.z);
        float3 g3 = float3(a1.zw, h.w);
        float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
        g0 *= norm.x;
        g1 *= norm.y;
        g2 *= norm.z;
        g3 *= norm.w;
        float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
        m = m * m;
        m = m * m;
        float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
        return 42.0 * dot(m, px);
    }

    float3 RotateAroundAxis(float3 center, float3 original, float3 u, float angle)
    {
        original -= center;
        float C = cos(angle);
        float S = sin(angle);
        float t = 1 - C;
        float m00 = t * u.x * u.x + C;
        float m01 = t * u.x * u.y - S * u.z;
        float m02 = t * u.x * u.z + S * u.y;
        float m10 = t * u.x * u.y + S * u.z;
        float m11 = t * u.y * u.y + C;
        float m12 = t * u.y * u.z - S * u.x;
        float m20 = t * u.x * u.z - S * u.y;
        float m21 = t * u.y * u.z + S * u.x;
        float m22 = t * u.z * u.z + C;
        float3x3 finalMatrix = float3x3(m00, m01, m02, m10, m11, m12, m20, m21, m22);
        return mul(finalMatrix, original) + center;
    }

    #include "UnityCG.cginc"
    #include "UnityShaderVariables.cginc"
    #include "UnityStandardBRDF.cginc"
    #include "UnityLightingCommon.cginc"
    #include "UnityStandardUtils.cginc"
	#include "Shadow.cginc"
 
    half _OffsetOff;
    half _WindIntensity, _Desaturate, _Non_AO;
    half _WindSpeed, _WindNoiseTiling3D;
    half  _NormalLerp,  _Clip, _ColChange;
    sampler2D_half _MainTex, _EdgeMask;
    half4 _MainColor, _ShadowColor, _ColChangeCol;
    float _GrassLODFadeDistance;
    ENDCG
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-100" "RenderType" = "TransparentCutOut"
        }
        LOD 300

        Blend Off
        AlphaToMask Off
        Cull [_CullMode]
        Pass
        {
            Name "Flower PreZ"
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
                float4 vertex: POSITION;
                float2 texcoord: TEXCOORD0;
                half4 VC: COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;
                float4 worldPos :TEXCOORD1;
                //H3D_SCREENPOS_COORDS(2)
            };


            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.uv = v.texcoord;
                float3 worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float wind = snoise(worldpos * _WindNoiseTiling3D + _Time.y * _WindSpeed);
                wind = wind * 0.5 + 0.5;
                float3 rotated = RotateAroundAxis(float3(0, -0.5, 0), float3(0, 0, 0), float3(0, 1, 1),
                                                  ((wind - 0.5) * 6.0));
                float3 vertOffset = rotated * _WindIntensity * v.VC.b;
                float3 vertexValue = lerp(vertOffset, float3(0, 0, 0), _OffsetOff);

                v.vertex.xyz += vertexValue;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            half4 frag(v2f i): SV_Target
            {
                ////////////////////////////////////颜色
                half4 diffuse = tex2D(_MainTex, i.uv);
                clip((diffuse.a) - _Clip);

                //fade				
                float dis = distance(i.worldPos, _WorldSpaceCameraPos);
                float fade = (dis - _GrassLODFadeDistance + 2) * 0.5f;
                fade = step(fade, 0) + step(0, fade) * (1 - fade);
                //ApplyDitherCrossFade(i.pos.xy, fade);
                return 0;
            }
            ENDCG
        }
        Pass
        {
            Name "Flower FORWARD"
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

            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile __ H3DSHADOWS_HARD H3DSHADOWS_SOFT
            #pragma multi_compile __ H3DSHADOWS_SPLIT_SPHERES
            #pragma multi_compile __ H3DSHADOWS_SINGLE_CASCADE H3DSHADOWS_DOUBLE_CASCADE
            #pragma multi_compile __ H3D_DITHER_ALPHA

            struct a2v
            {
                float4 vertex: POSITION;
                float2 texcoord: TEXCOORD0;
                half4 VC: COLOR;
                half3 normal: NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;// 与顶点相关的属性和无关的属性拆开 https://developer.arm.com/documentation/102546/0100/Index-Driven-Geometry-Pipeline
                float4 worldPos: TEXCOORD1; //w为fog系数
                half3 worldN: TEXCOORD2;
                half3 colChange:TEXCOORD3;
            };


            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.uv = v.texcoord;
                float3 worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;

             
                float wind = snoise(worldpos * _WindNoiseTiling3D + _Time.y * _WindSpeed);
                wind = wind * 0.5 + 0.5;
                float3 rotated = RotateAroundAxis(float3(0, -0.5, 0), float3(0, 0, 0), float3(0, 1, 1),
                                                  ((wind - 0.5) * 6.0));

                float3 vertOffset = rotated * _WindIntensity * v.VC.b;
                float3 vertexValue = lerp(vertOffset, float3(0, 0, 0), _OffsetOff);

                v.vertex.xyz += vertexValue;
                o.pos = UnityObjectToClipPos(v.vertex);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                half3 worldN = lerp(worldNormal, half3(0, 1, 0), _NormalLerp);
                o.worldN = worldN;
               
                //dotResult
                half VdotN = dot(viewDir, worldN);
            
                o.worldPos = worldPos;
                //颜色切换菲涅尔效果
                half ColChangeMask = 1 - saturate(VdotN + _ColChange);
                ColChangeMask *= ColChangeMask * ColChangeMask * ColChangeMask * ColChangeMask;
                o.colChange = _ColChangeCol.rgb * saturate(ColChangeMask) * _ColChangeCol.a;

                UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o, o.pos);
                return o;
            }

            half4 frag(v2f i): SV_Target
            {
                ////////////////////////////////////颜色
                float2 uv = i.uv ;
            
                half4 diffuse = tex2D(_MainTex, uv);
                half diffuseLow = dot(diffuse.rgb, half3(0.299, 0.587, 0.114));
                half3 diffuseFin = lerp(diffuse.rgb, diffuseLow.xxx, _Desaturate);
                diffuseFin = lerp(diffuseFin, half3(1, 1, 1), _Non_AO) * _MainColor;
                ////////////////////////////////////阴影
                ///////世界坐标
                float4 worldPos = float4(i.worldPos.xyz, 1); //w为fog系数
                half shadow = H3D_SHADOW_ATTENUATION(worldPos, i.worldN);
                shadow = saturate(shadow);
                half4 FinCol = half4(1, 1, 1, 1);

                ////////////////////////////////////变体simpleShade简单版
                half3 simpleShadeCol = lerp(diffuseFin * _ShadowColor, diffuseFin, shadow);
                FinCol.rgb = simpleShadeCol;

                UNITY_EXTRACT_FOG_FROM_WORLD_POS(i);
                UNITY_APPLY_FOG_COLOR(_unity_fogCoord, FinCol, unity_FogColor);
    
                FinCol.rgb += i.colChange;

                return FinCol;
            }
            ENDCG
        }

    }
     SubShader
    {
        Tags
        {
            "Queue" = "Geometry-100" "RenderType" = "TransparentCutOut"
        }
        LOD 200

        Blend Off
        AlphaToMask Off
        Cull [_CullMode]
        Pass
        {
            Name "Flower FORWARD"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile __ H3DSHADOWS_HARD H3DSHADOWS_SOFT
            #pragma multi_compile __ H3DSHADOWS_SPLIT_SPHERES
            #pragma multi_compile __ H3DSHADOWS_SINGLE_CASCADE H3DSHADOWS_DOUBLE_CASCADE
            #pragma multi_compile __ H3D_DITHER_ALPHA

            struct a2v
            {
                float4 vertex: POSITION;
                float2 texcoord: TEXCOORD0;
                half4 VC: COLOR;
                half3 normal: NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0; //xy=uv
                float4 worldPos: TEXCOORD1; //w为fog系数
                half3 worldN: TEXCOORD2;
                half3 colChange:TEXCOORD3;
            };


            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.uv = v.texcoord;
                float3 worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float wind = snoise(worldpos * _WindNoiseTiling3D + _Time.y * _WindSpeed);
                wind = wind * 0.5 + 0.5;
                float3 rotated = RotateAroundAxis(float3(0, -0.5, 0), float3(0, 0, 0), float3(0, 1, 1),
                                                  ((wind - 0.5) * 6.0));

                float3 vertOffset = rotated * _WindIntensity * v.VC.b;
                float3 vertexValue = lerp(vertOffset, float3(0, 0, 0), _OffsetOff);

                v.vertex.xyz += vertexValue;
                o.pos = UnityObjectToClipPos(v.vertex);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                half3 worldN = lerp(worldNormal, half3(0, 1, 0), _NormalLerp);
                o.worldN = worldN;
                //dotResult
                half VdotN = dot(viewDir, worldN);
                o.worldPos = worldPos;
                //颜色切换菲涅尔效果
                half ColChangeMask = 1 - saturate(VdotN + _ColChange);
                ColChangeMask *= ColChangeMask * ColChangeMask * ColChangeMask * ColChangeMask;
                o.colChange = _ColChangeCol.rgb * saturate(ColChangeMask) * _ColChangeCol.a;

                UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o, o.pos);
                return o;
            }

            half4 frag(v2f i): SV_Target
            {
                ////////////////////////////////////颜色
                float2 uv = i.uv;
                half4 diffuse = tex2D(_MainTex, uv);
                clip((diffuse.a) - _Clip);
                half diffuseLow = dot(diffuse.rgb, half3(0.299, 0.587, 0.114));
                half3 diffuseFin = lerp(diffuse.rgb, diffuseLow.xxx, _Desaturate);
                diffuseFin = lerp(diffuseFin, half3(1, 1, 1), _Non_AO) * _MainColor;
                ////////////////////////////////////阴影
                ///////世界坐标
                float4 worldPos = float4(i.worldPos.xyz, 1); //w为fog系数
                half shadow = H3D_SHADOW_ATTENUATION(worldPos, i.worldN);
                shadow = saturate(shadow);
                half4 FinCol = half4(1, 1, 1, 1);

                ////////////////////////////////////变体simpleShade简单版
                half3 simpleShadeCol = lerp(diffuseFin * _ShadowColor, diffuseFin, shadow);
                FinCol.rgb = simpleShadeCol;

                UNITY_EXTRACT_FOG_FROM_WORLD_POS(i);
                UNITY_APPLY_FOG_COLOR(_unity_fogCoord, FinCol, unity_FogColor);
           
                FinCol.rgb += i.colChange;

                return FinCol;
            }
            ENDCG
        }

    }
   
}