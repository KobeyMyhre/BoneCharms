Shader "Custom/BoneCharmslitShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Ambient("Ambient", Range(0,1)) = 0.25
        _SpecColor("Specular Color", Color) = (1,1,1,1)
        _Shininess("Shininess", Range(0,10)) = 0.5 
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap("Bump Map", 2D) = "white" {}
        _GlyphTex01("Glyph Atlas", 2D) = "white" {}
        _GlyphTex02("Glyph Atlas", 2D) = "white" {}
        _CharmMask("Charm Mask", 2D) = "white" {}
        
    }
    SubShader
    {
        //Tags { "RenderType"="Opaque" }
        //LOD 100
        Pass
        {
            Tags {"LightMode" = "ForwardBase"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
                float4 vertexClip : SV_POSITION;
                float4 vertexWorld : TEXCOORD3;
                float3 worldNormal : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            sampler2D _CharmMask;
            float4 _CharmMask_ST;

            sampler2D _GlyphTex01;
            float4 _GlyphTex01_ST;
            sampler2D _GlyphTex02;
            float4 _GlyphTex02_ST;

            float4 _Color;
            float _Ambient;
            float _Shininess;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertexClip = UnityObjectToClipPos(v.vertex);
                o.vertexWorld = mul(unity_ObjectToWorld, v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal); 

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = TRANSFORM_TEX(v.uv2, _GlyphTex01);
                o.uv3 = TRANSFORM_TEX(v.uv3, _GlyphTex02);
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normalDirection = UnpackNormal(tex2D(_NormalMap, i.uv));//normalize(i.worldNormal);
                float3 viewDirection = normalize(UnityWorldSpaceViewDir(i.vertexWorld));
                float3 lightDirection = normalize(UnityWorldSpaceLightDir(i.vertexWorld));
                
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 glyphCol1 = tex2D(_GlyphTex01, i.uv2);
                fixed4 glyphCol2 = tex2D(_GlyphTex02, i.uv3);
                fixed4 finalColor = col;// + glyphCol1 + glyphCol2;

                float nl = max(_Ambient, dot(normalDirection, lightDirection));
                float4 diffuseTerm = nl * _Color * finalColor * _LightColor0;
                float3 reflectionDirection = reflect(lightDirection, normalDirection);
                float3 specularDot = max(0.0, dot(viewDirection, reflectionDirection));
                float3 specular = pow(specularDot, _Shininess);
                float4 specularTerm = float4(specular, 1) * _SpecColor * _LightColor0;

                float4 lightedColor = diffuseTerm + specularTerm + glyphCol1 + glyphCol2;
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return lightedColor;
                //col *= glyphCol;
                // apply fog
                //return col + glyphCol1 + glyphCol2;
            }
            ENDCG
        }
        Pass
        {
            Tags {"LightMode" = "ForwardAdd"}
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

             struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
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
                float4 vertexClip : SV_POSITION;
                float4 vertexWorld : TEXCOORD3;
                float3 worldNormal : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            sampler2D _CharmMask;
            float4 _CharmMask_ST;

            sampler2D _GlyphTex01;
            float4 _GlyphTex01_ST;
            sampler2D _GlyphTex02;
            float4 _GlyphTex02_ST;

            float4 _Color;
            float _Ambient;
            float _Shininess;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertexClip = UnityObjectToClipPos(v.vertex);
                o.vertexWorld = mul(unity_ObjectToWorld, v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal); 

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = TRANSFORM_TEX(v.uv2, _GlyphTex01);
                o.uv3 = TRANSFORM_TEX(v.uv3, _GlyphTex02);
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 normalDirection = UnpackNormal(tex2D(_NormalMap, i.uv));//normalize(i.worldNormal);
                float3 viewDirection = normalize(UnityWorldSpaceViewDir(i.vertexWorld));
                float3 lightDirection = normalize(UnityWorldSpaceLightDir(i.vertexWorld));
                
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 glyphCol1 = tex2D(_GlyphTex01, i.uv2);
                fixed4 glyphCol2 = tex2D(_GlyphTex02, i.uv3);
                fixed4 finalColor = col;// + glyphCol1 + glyphCol2;

                float nl = max(_Ambient, dot(normalDirection, lightDirection));
                float4 diffuseTerm = nl * _Color * finalColor * _LightColor0;
                float3 reflectionDirection = reflect(lightDirection, normalDirection);
                float3 specularDot = max(0.0, dot(viewDirection, reflectionDirection));
                float3 specular = pow(specularDot, _Shininess);
                float4 specularTerm = float4(specular, 1) * _SpecColor * _LightColor0;

                float4 lightedColor = diffuseTerm + specularTerm + glyphCol1 + glyphCol2;
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return lightedColor;
                //col *= glyphCol;
                // apply fog
                //return col + glyphCol1 + glyphCol2;
            }
            ENDCG
        }
    }
}
