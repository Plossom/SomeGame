// A smooth Gaussian blur in one direction (_Direction: (1,0) across, (0,1) down), 9 taps a few texels
// apart. MapScreen runs it across and down a few times on a reduced copy of a map page while switching cups.
Shader "Hidden/SomeGame/CupBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _Direction;
            float _Spread;

            fixed4 frag (v2f_img i) : SV_Target
            {
                // Taps at 0, ±1, ±2, ±3, ±4 steps, Gaussian weights (sigma about 2 steps).
                const float weights[5] = { 0.2042, 0.1802, 0.1238, 0.0663, 0.0276 };
                float2 step = _Direction * _MainTex_TexelSize.xy * _Spread;
                fixed4 c = tex2D(_MainTex, i.uv) * weights[0];
                [unroll] for (int k = 1; k < 5; k++)
                {
                    c += tex2D(_MainTex, i.uv + step * k) * weights[k];
                    c += tex2D(_MainTex, i.uv - step * k) * weights[k];
                }
                return c;
            }
            ENDCG
        }
    }
}
