Shader "H3D/Performance"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
	   Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

		Blend SrcAlpha OneMinusSrcAlpha

		ZWrite Off
		ZTest Off
		Pass {

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"


			struct appdata_t 
			{
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};

			sampler2D _MainTex; half4  _MainTex_ST;
			

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = v.color;
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				fixed4 col =1;
				/*for (int k = 0; k < 1; k++)
				{
					col = col + tex2D(_MainTex, i.texcoord.xy + half2(0.1, 0.1)*k);
					col = pow(dot(col.rgb, col.rgb), 4);
				}
				col = col/5;*/
				//col*= i.color;
				//col = half4(col.r, 0, 0, 0.5);
				col.a = 0;
				return  col;
			}
			ENDCG
		}
    }
}
