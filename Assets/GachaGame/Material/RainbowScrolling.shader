Shader "Unlit/RainbowScrolling"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _GradientTex ("Gradient (1D)", 2D) = "white" {}
        _Tiling ("Tiling", Vector) = (1,1,0,0)
        _ScrollSpeed ("Scroll Speed", Float) = 0.2
        _GradientRange ("Gradient Range (min, max)", Vector) = (0,1,0,0)
        _Blend ("Blend Gradient", Range(0,1)) = 1.0
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _GradientTex; // expects gradient horizontally
            float4 _Tiling; // x,y = tiling, z,w unused
            float _ScrollSpeed;
            float4 _GradientRange;
            float _Blend;
            float _AlphaCutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // apply texture ST (Unity uses _MainTex_ST for tiling/offset); plus our extra tiling
                float2 main_st = v.uv * _Tiling.xy;
                o.uv = main_st;
                return o;
            }

            fixed4 SampleGradient(float t)
            {
                // sample gradient horizontally at v = 0.5
                return tex2D(_GradientTex, float2(t, 0.5));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float scroll = _Time.y * _ScrollSpeed;
                float2 uv = i.uv + float2(scroll, 0);

                fixed4 baseCol = tex2D(_MainTex, uv);

                float minR = _GradientRange.x;
                float maxR = _GradientRange.y;
                float range = max(maxR - minR, 0.0001);
                float t = saturate((uv.x - minR) / range);

                fixed4 gradCol = SampleGradient(t);

                fixed3 colored = baseCol.rgb * gradCol.rgb;
                fixed3 finalRGB = lerp(baseCol.rgb, colored, _Blend);
                fixed finalA = baseCol.a * gradCol.a;

                if (finalA <= _AlphaCutoff) discard;

                return fixed4(finalRGB, finalA);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Texture"
}
