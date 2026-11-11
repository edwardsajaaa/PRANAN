Shader "PRANAN/DustMoteParticle"
{
    Properties
    {
        _BaseColor ("Base Spore Color", Color) = (0.75, 0.85, 0.95, 0.25)
        _LitColor ("Flashlight Lit Color", Color) = (1.0, 0.95, 0.75, 1.0)
        _AmbientIntensity ("Ambient Intensity", Range(0.01, 1.0)) = 0.18
        _ShimmerSpeed ("Shimmer Frequency", Float) = 2.5
        _DepthFadeDistance ("Soft Depth Fade Distance", Range(0.05, 2.0)) = 0.4
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+120" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent" 
        }

        Blend One One
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D_float _CameraDepthTexture;

            fixed4 _BaseColor;
            fixed4 _LitColor;
            float _AmbientIntensity;
            float _ShimmerSpeed;
            float _DepthFadeDistance;

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
                // Lingkaran bulat lembut (anti-aliased soft circular disc)
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float distSq = dot(centeredUV, centeredUV);
                if (distSq > 1.0) discard;

                float circleGlow = saturate(1.0 - distSq);
                circleGlow = circleGlow * circleGlow;

                // Shimmering micro particles
                float shimmer = 0.8 + 0.2 * sin(i.worldPos.x * 10.0 + i.worldPos.y * 6.0 + _Time.y * _ShimmerSpeed);

                // Flashlight detection
                float flashLit = 0.0;
                if (_FlashlightActive > 0.01)
                {
                    float3 toPart = i.worldPos - _FlashlightPos.xyz;
                    float dist = length(toPart);
                    if (dist < _FlashlightRange && dist > 0.05)
                    {
                        float3 dir = toPart / dist;
                        float cosAng = dot(dir, _FlashlightForward.xyz);

                        float spotAtten = smoothstep(_FlashlightCosOuter, _FlashlightCosInner, cosAng);
                        float distAtten = saturate(1.0 - (dist / max(1.0, _FlashlightRange)));
                        distAtten = pow(distAtten, 1.2);

                        float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                        float glint = pow(saturate(dot(reflect(-dir, float3(0, 1, 0)), viewDir)), 4.0);

                        flashLit = spotAtten * distAtten * (3.5 + glint * 2.5);
                    }
                }

                // Depth fade
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

                fixed4 finalColor = lerp(_BaseColor, _LitColor, saturate(flashLit));
                float intensity = (_AmbientIntensity * _BaseColor.a + flashLit) * shimmer * depthFade * i.color.a * circleGlow;

                fixed3 rgb = finalColor.rgb * intensity;
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Particles/Standard Unlit"
}
