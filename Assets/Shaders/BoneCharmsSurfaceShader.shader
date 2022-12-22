Shader "Custom/BoneCharmsSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _GlyphTex01("Glyph Atlas", 2D) = "white" {}
        _GlyphTex02("Glyph Atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #include "UnityCG.cginc"
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        //float4 _MainTex_ST;

        sampler2D _GlyphTex01;
        //float4 _GlyphTex01_ST;
        sampler2D _GlyphTex02;
        //float4 _GlyphTex02_ST;
        
        struct Input
        {
            float2 uv_MainTex : TEXCOORD0;
            float2 uv_GlyphTex01 : TEXCOORD1;
            float2 uv_GlyphTex02 : TEXCOORD2;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);


            //o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
            //o.uv_GlyphTex01 = TRANSFORM_TEX(v.texcoord1, _GlyphTex01);
            //o.uv_GlyphTex02 = TRANSFORM_TEX(v.texcoord2, _GlyphTex02);
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            fixed4 g1 = tex2D(_GlyphTex01, IN.uv_GlyphTex01);
            fixed4 g2 = tex2D(_GlyphTex02, IN.uv_GlyphTex02);

            o.Albedo = c.rgb + g1.rgb + g2.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
