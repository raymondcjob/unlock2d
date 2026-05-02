Shader "Custom/TileHintScan"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Sweep ("Sweep", Float) = -1
        _Width ("Width", Float) = 0.18
        _Softness ("Softness", Float) = 0.08
        _Reverse ("Reverse", Float) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Sweep;
            float _Width;
            float _Softness;
            float _Reverse;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;
                OUT.color = IN.color * _Color;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.uv);

                float diag = _Reverse > 0.5
                    ? ((1.0 - IN.uv.x) + IN.uv.y)
                    : (IN.uv.x + IN.uv.y);

                float halfBand = max(0.0001, _Width * 0.5);
                float soft = max(0.0001, _Softness);

                float dist = abs(diag - _Sweep);
                float band = 1.0 - smoothstep(halfBand, halfBand + soft, dist);

                fixed4 result = IN.color;
                result.rgb *= band;
                result.a *= tex.a * band;

                return result;
            }
            ENDCG
        }
    }
}
