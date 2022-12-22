Shader "Custom/BoneCharmsShader"
{
    Properties
    {
        _MainTex ("Charm Wrap", 2D) = "white" {}
        _GlyphTex01("Glyph Atlas", 2D) = "white" {}
        _GlyphTex02("Glyph Atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard 

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _GlyphTex01;
        sampler2D _GlyphTex02;

        struct Input {
            float2 uv_MainTex : TEXCOORD0;
            float2 uv2_GlyphTex01 : TEXCOORD1;
            float2 uv3_GlyphTex02 : TEXCOORD2;
        };
        

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half4 c = tex2D(_MainTex, IN.uv_MainTex.xy);
            half4 g1 = tex2D(_GlyphTex01, IN.uv2_GlyphTex01.xy);
            half4 g2 = tex2D(_GlyphTex01, IN.uv3_GlyphTex02.xy); 

            o.Albedo = c.rgb + g1.rgb + g2.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
