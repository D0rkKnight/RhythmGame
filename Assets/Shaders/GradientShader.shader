Shader "Sprites/GradientShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", float) = 1.0
    }
    SubShader
    {
        Tags { 
            //"Queue" = "Transparent"
			//"RenderType" = "Transparent"

        "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        LOD 100
		Cull Off
		Lighting Off
		Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"



            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 col : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 col : COLOR0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.col; // Pass on vertex color

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                // And mix
                float4 texCol = tex2D(_MainTex, i.uv);
                fixed4 col = lerp(texCol, i.col, 0.5);
                col.a = min(texCol.a, i.col.a);

                float alphaRamp = 1 - i.uv.y;
                alphaRamp *= alphaRamp;
                col.a = min(col.a, alphaRamp);
                col.a += _Intensity-1;
                col.a = max(col.a, 0);

                col.rgb *= col.a; // Coords are homogenous

                return col;
            }
            ENDCG
        }
    }
}