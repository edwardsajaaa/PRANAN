Shader "Custom/LowPolyTerrain"
{
    Properties
    {
        _GrassColor ("Grass Low Color", Color) = (0.72, 0.79, 0.38, 1.0)
        _GrassHighColor ("Grass High Color", Color) = (0.82, 0.86, 0.45, 1.0)
        _RockColor ("Rock / Slope Color", Color) = (0.86, 0.84, 0.72, 1.0)
        _SoilColor ("Soil / Basin Color", Color) = (0.76, 0.70, 0.54, 1.0)
        _SlopeThreshold ("Slope Threshold", Range(0.0, 1.0)) = 0.55
        _SlopeSmoothness ("Slope Blend", Range(0.01, 0.5)) = 0.12
        _HeightMin ("Height Min", Float) = 0.0
        _HeightMax ("Height Max", Float) = 40.0
        _LightIntensity ("Light Boost", Range(0.5, 2.0)) = 1.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                LIGHTING_COORDS(1, 2)
            };

            fixed4 _GrassColor;
            fixed4 _GrassHighColor;
            fixed4 _RockColor;
            fixed4 _SoilColor;
            float _SlopeThreshold;
            float _SlopeSmoothness;
            float _HeightMin;
            float _HeightMax;
            float _LightIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 ddxPos = ddx(i.worldPos);
                float3 ddyPos = ddy(i.worldPos);
                float3 flatNormal = normalize(cross(ddyPos, ddxPos));

                float slope = 1.0 - saturate(dot(flatNormal, float3(0, 1, 0)));
                float slopeFactor = smoothstep(_SlopeThreshold - _SlopeSmoothness, _SlopeThreshold + _SlopeSmoothness, slope);

                float heightFactor = saturate((i.worldPos.y - _HeightMin) / max(_HeightMax - _HeightMin, 0.001));

                fixed3 grass = lerp(_GrassColor.rgb, _GrassHighColor.rgb, heightFactor);
                fixed3 land = lerp(_SoilColor.rgb, grass, smoothstep(0.0, 0.25, heightFactor));
                fixed3 baseAlbedo = lerp(land, _RockColor.rgb, slopeFactor);

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0.0, dot(flatNormal, lightDir));

                fixed3 ambient = ShadeSH9(float4(flatNormal, 1.0));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                fixed3 diffuse = _LightColor0.rgb * (NdotL * atten * _LightIntensity);
                fixed3 finalColor = baseAlbedo * (diffuse + ambient);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
