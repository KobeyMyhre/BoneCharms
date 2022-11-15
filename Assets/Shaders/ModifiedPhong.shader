// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/ModifiedPhong"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _Shininess ("Shininess (n)", Range(1,1000)) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        
        #pragma multi_compile_instancing
        #pragma surface surf PhongModified fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input {
            float2 uv_MainTex;
        };

        half _Shininess;
        fixed4 _Color;

        inline void LightingPhongModified_GI (
            SurfaceOutput s,
            UnityGIInput data,
            inout UnityGI gi
        )
        {
            gi = UnityGlobalIllumination(data, 1.0, s.Normal);
        }

        inline fixed4 LightingPhongModified(SurfaceOutput s, half3 viewDir, UnityGI gi)
        {
            const float PI = 3.1415926;
            UnityLight light = gi.light;

            float nl = max(0.0f, dot(s.Normal, light.dir));
            float3 diffuseTerm = nl * s.Albedo.rgb * light.color;

            float norm = (_Shininess + 2) / (2 * PI);
            float3 reflectionDirection = reflect(-light.dir, s.Normal);
            float3 specularDot = max(0.0, dot(viewDir, reflectionDirection));
            float3 specular = norm * pow(specularDot, _Shininess);
            float3 specularTerm = specular * _SpecColor.rgb * light.color.rgb;

            float3 finalColor = diffuseTerm.rgb; //+ specularTerm;

            fixed4 c;
            c.rgb = finalColor;
            c.a = s.Alpha;

            #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
            c.rgb += s.Albedo * gi.indirect.diffuse;
            #endif

            

            return c;
        }

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Specular = _Shininess;
            o.Alpha = c.a;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
