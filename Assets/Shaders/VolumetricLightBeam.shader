Shader "PRANAN/VolumetricLightBeam"
{
    Properties
    {
        _Color ("Beam Color", Color) = (1, 0.90, 0.70, 0.5)
        _Intensity ("Base Intensity", Range(0, 5)) = 1.0
        _FogMultiplier ("Fog Multiplier", Range(0, 5)) = 1.0
        _LengthFalloff ("Length Falloff Power", Range(0.2, 4)) = 1.2
        _RadialFalloff ("Radial Falloff Power", Range(0.2, 4)) = 1.8
        _NoiseSpeed ("Dust Noise Speed", Vector) = (0.04, 0.10, 0, 0)
        _NoiseScale ("Dust Noise Scale", Float) = 3.5
        _DepthFadeDistance ("Soft Depth Fade Distance", Range(0.05, 5)) = 1.2
        _CameraNearFade ("Camera Near Fade Distance", Range(0.05, 3)) = 0.4
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+100" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent" 
        }

        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D_float _CameraDepthTexture;

            fixed4 _Color;
            float _Intensity;
            float _FogMultiplier;
            float _LengthFalloff;
            float _RadialFalloff;
            float4 _NoiseSpeed;
            float _NoiseScale;
            float _DepthFadeDistance;
            float _CameraNearFade;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0; // x: radial (0 core -> 1 rim), y: longitudinal (0 source -> 1 tip)
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Radial falloff (bagian tengah/core paling terang, memudar lembut ke tepian luar)
                float radial = saturate(1.0 - i.uv.x);
                radial = pow(radial, _RadialFalloff);

                // 2. Longitudinal falloff (pangkal paling terang, memudar lembut seiring jarak)
                float longitudinal = saturate(1.0 - i.uv.y);
                longitudinal = pow(longitudinal, _LengthFalloff);

                // 3. Shimmer debu / partikel mikro hutan bergerak perlahan
                float dust = sin(i.worldPos.x * _NoiseScale + _Time.y * _NoiseSpeed.x)
                           * cos(i.worldPos.y * _NoiseScale + _Time.y * _NoiseSpeed.y)
                           * sin(i.worldPos.z * (_NoiseScale * 0.7) + _Time.y * 0.08);
                float dustMotes = 0.82 + 0.18 * dust;

                // 4. Camera near-plane soft fade (mencegah clipping tajam saat kamera menembus beam)
                float camDist = distance(_WorldSpaceCameraPos, i.worldPos);
                float camFade = saturate((camDist - _CameraNearFade) / 0.7);

                // 5. Soft Depth Fade terhadap terrain, batang pohon, dan bebatuan
                float depthFade = 1.0;
                #if defined(UNITY_REVERSED_Z)
                float rawDepth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
                #else
                float rawDepth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
                #endif
                if (rawDepth > 0.00001)
                {
                    float sceneEyeDepth = LinearEyeDepth(rawDepth);
                    float partEyeDepth = i.screenPos.w;
                    depthFade = saturate((sceneEyeDepth - partEyeDepth) / max(0.01, _DepthFadeDistance));
                }

                // Total opacity
                float finalAlpha = radial * longitudinal * dustMotes * camFade * depthFade * _Color.a;
                finalAlpha = saturate(finalAlpha);

                float finalIntensity = _Intensity * _FogMultiplier;
                fixed4 col = fixed4(_Color.rgb * finalIntensity, finalAlpha);

                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0));
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/VertexLit"
}

