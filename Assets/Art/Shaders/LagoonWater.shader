// Animated tropical water for the Water Cup: the base colour picture wobbles gently, two layers of caustic light
// drift across it in different directions (where they meet, bright light lines dance like sun on a sandy
// bottom), and a few glints sparkle. Unlit, tinted by the material and the mesh's vertex colours (so a ground
// mesh can carry its light and dark patches).
Shader "SomeGame/LagoonWater"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _Caustics ("Caustics", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _CausticColor ("Caustic light", Color) = (1,1,1,0.5)
        _CausticScale ("Caustic scale", Float) = 1.6
        _Flow ("Flow (base xy, caustics zw)", Vector) = (0.01, 0.006, 0.035, 0.02)
        _Wobble ("Wobble", Float) = 0.012
        _Glint ("Glints", Float) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _Caustics;
            float4 _MainTex_ST;
            fixed4 _Tint, _CausticColor;
            float _CausticScale, _Wobble, _Glint;
            float _LagoonCalm; // global: 1 while the track's picture is taken (no light play in the preview)
            float4 _Flow;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                float2 wob = float2(sin(uv.y * 6.2 + t * 0.9), cos(uv.x * 5.3 + t * 0.75)) * _Wobble;
                fixed4 col = tex2D(_MainTex, uv + wob + t * _Flow.xy) * _Tint * i.color;
                float2 cuv = uv * _CausticScale;
                float a = tex2D(_Caustics, cuv + wob * 2.0 + t * _Flow.zw).r;
                float b = tex2D(_Caustics, cuv * 1.13 + float2(0.37, 0.61) - wob * 1.5 - t * _Flow.zw * 0.83).r;
                float caustic = pow(saturate(a * b * 1.6), 1.4);
                col.rgb += _CausticColor.rgb * caustic * _CausticColor.a * (1.0 - 0.85 * _LagoonCalm);
                // Glints: where both layers peak, a short bright flash.
                float glint = saturate((a * b - 0.8) * 8.0) * pow(0.5 + 0.5 * sin(t * 2.6 + (a - b) * 14.0), 6.0);
                col.rgb += glint * _Glint * (1.0 - _LagoonCalm);
                return col;
            }
            ENDCG
        }
    }
}
