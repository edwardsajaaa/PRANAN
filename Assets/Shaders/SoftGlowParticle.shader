Shader "PRANAN/SoftGlowParticle"
{
    Properties
    {
        _Color ("Glow Color", Color) = (0.78, 0.98, 0.25, 1.0)
        _CoreSharpness ("Core Sharpness", Range(0.5, 4.0)) = 2.0
        _PulseSpeed ("Blink / Pulse Speed", Float) = 2.2
        _PulseAmount ("Pulse Amount", Range(0.0, 1.0)) = 0.4
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+110" 
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

            fixed4 _Color;
            float _CoreSharpness;
            float _PulseSpeed;
            float _PulseAmount;

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
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float dist = length(centeredUV);
                if (dist > 1.0) discard;

                // Soft radial core
                float glow = saturate(1.0 - dist);
                glow = pow(glow, _CoreSharpness);

                // Twinkle / Pulse denyut acak berdasarkan posisi di dunia
                float phase = i.worldPos.x * 3.5 + i.worldPos.y * 2.1 + i.worldPos.z * 4.3;
                float pulse = 1.0 - _PulseAmount + _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed + phase));

                float alpha = glow * i.color.a * _Color.a * pulse;
                fixed3 rgb = _Color.rgb * i.color.rgb * pulse;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
