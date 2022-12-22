Shader "Custom/BoneCharmsUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlyphTex01("Glyph Atlas", 2D) = "white" {}
        _GlyphTex02("Glyph Atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _GlyphTex01;
            float4 _GlyphTex01_ST;
            sampler2D _GlyphTex02;
            float4 _GlyphTex02_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = TRANSFORM_TEX(v.uv2, _GlyphTex01);
                o.uv3 = TRANSFORM_TEX(v.uv3, _GlyphTex02);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 glyphCol1 = tex2D(_GlyphTex01, i.uv2);
                fixed4 glyphCol2 = tex2D(_GlyphTex02, i.uv3);
                //col *= glyphCol;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col + glyphCol1 + glyphCol2;
            }
            ENDCG
        }
    }
}
