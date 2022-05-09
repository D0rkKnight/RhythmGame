Shader "Sprites/ColButtonShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Intensity("Intensity", float) = 1.0
        _Center("Center", Vector) = (0, 0, 0, 1) // In screen coords
        _RampSlope("Ramp Slope", float) = 50
    }
        SubShader
        {
            Tags {
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

                    float2 worldVPos : TEXCOORD1;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float _Intensity;
                float4 _Center;
                float _RampSlope;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.col = v.col; // Pass on vertex color
                    o.worldVPos = v.vertex;

                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // sample the texture
                    // And mix
                    float4 texCol = tex2D(_MainTex, i.uv);
                    fixed4 col = lerp(texCol, i.col, 0.5);

                    // Pyramid ramp
                    float2 delta = i.worldVPos - _Center;
                    float dist = max(abs(delta.x), abs(delta.y));
                    float mag = (dist/_RampSlope) * 2 - 1 + _Intensity;

                    // Wobble
                    float wob = sin(mag * mag * 10) / 2;
                    mag += wob;

                    mag *= texCol.a; // Texture mask
                    mag = min(1, mag); // Clamp

                    // Color ramp
                    float4 o = float4(0, 0, 0, 0);
                    if (mag > 0.5) o = col;

                    return o;
                }
                ENDCG
            }
        }
}