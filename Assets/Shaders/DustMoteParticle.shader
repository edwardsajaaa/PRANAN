Shader "PRANAN/DustMoteParticle"
{
    Properties
    {
        _BaseColor ("Base Spore Color", Color) = (0.9, 0.95, 1.0, 0.3)
        _LitColor ("Flashlight Lit Color", Color) = (1.0, 0.98, 0.85, 1.0)
        _AmbientIntensity ("Ambient Intensity", Range(0.01, 1.0)) = 0.15
        _ShimmerSpeed ("Shimmer Frequency", Float) = 3.0
        _DepthFadeDistance ("Soft Depth Fade Distance", Range(0.05, 2.0)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+120" 
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
            #pragma multi_compile_particles
            #include "UnityCG.cginc"

            sampler2D_float _CameraDepthTexture;

            fixed4 _BaseColor;
            fixed4 _LitColor;
            float _AmbientIntensity;
            float _ShimmerSpeed;
            float _DepthFadeDistance;

            // Flashlight Controller uniforms
            float4 _FlashlightPos;
            float4 _FlashlightForward;
            float _FlashlightCosInner;
            float _FlashlightCosOuter;
            float _FlashlightRange;
            float _FlashlightActive;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Procedural Soft Circular Disc
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float distFromCenter = length(centeredUV);
                if (distFromCenter > 1.0) discard;

                // Soft circular falloff dengan hotspot terang di tengah
                float circleAlpha = saturate(1.0 - distFromCenter);
                circleAlpha = pow(circleAlpha, 1.8);

                // 2. Shimmer & Glint berdenyut halus acak
                float shimmer = 0.75 + 0.25 * sin(i.worldPos.x * 12.0 + i.worldPos.y * 7.0 + _Time.y * _ShimmerSpeed);

                // 3. Deteksi pencahayaan kerucut FlashlightController
                float flashLit = 0.0;
                if (_FlashlightActive > 0.01)
                {
                    float3 toPart = i.worldPos - _FlashlightPos.xyz;
                    float distToFlash = length(toPart);
                    if (distToFlash < _FlashlightRange && distToFlash > 0.05)
                    {
                        float3 dir = toPart / distToFlash;
                        float cosAng = dot(dir, _FlashlightForward.xyz);

                        float spotAtten = smoothstep(_FlashlightCosOuter, _FlashlightCosInner, cosAng);
                        float distAtten = saturate(1.0 - (distToFlash / max(1.0, _FlashlightRange)));
                        distAtten = pow(distAtten, 1.2);

                        // Pantulan berkilau (glint) saat partikel berada tepat di sorot senter
                        float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                        float specularGlint = pow(saturate(dot(reflect(-dir, float3(0, 1, 0)), viewDir)), 3.0);

                        flashLit = spotAtten * distAtten * (3.8 + specularGlint * 2.5);
                    }
                }

                // 4. Soft Depth Fade
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

                // Warna gabungan: ambient di tempat gelap, memantulkan cahaya benderang saat kena sorot senter
                fixed4 finalColor = lerp(_BaseColor, _LitColor, saturate(flashLit));
                float totalIntensity = (_AmbientIntensity + flashLit) * shimmer * depthFade * i.color.a;

                fixed3 rgb = finalColor.rgb * totalIntensity;
                float alpha = circleAlpha * finalColor.a * saturate(totalIntensity);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
