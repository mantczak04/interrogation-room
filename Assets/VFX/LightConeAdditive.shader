Shader "InterrogationRoom/LightConeAdditive"
{
    Properties
    {
        [HDR] _Color ("Beam Colour", Color) = (1, 0.72, 0.42, 1)
        _Intensity ("Intensity", Range(0, 8)) = 1.4
        _SoftEdge ("Soft Edge", Range(0, 1)) = 0.85
        _SoftEdgePower ("Soft Edge Power", Range(0.5, 8)) = 1.8
        _TopFade ("Top Fade", Range(0, 1)) = 0.12
        _BottomFade ("Bottom Fade", Range(0, 1)) = 0.55
        _BottomFadePower ("Bottom Fade Power", Range(0.5, 6)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LightConeAdditive"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _SoftEdge;
                half _SoftEdgePower;
                half _TopFade;
                half _BottomFade;
                half _BottomFadePower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positions.positionCS;
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                output.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // uv.y runs 0 at the apex (the bulb) to 1 at the open base.
                half alongBeam = saturate(input.uv.y);

                // Fade in just under the fitting so the mesh never shows a hard apex,
                // and dissolve before the base so the beam ends in air, not on a disc.
                half topFade = smoothstep(0.0h, max(_TopFade, 1e-3h), alongBeam);
                half bottomFade = 1.0h - saturate((alongBeam - (1.0h - _BottomFade)) / max(_BottomFade, 1e-3h));
                bottomFade = pow(bottomFade, _BottomFadePower);

                // Rebuild the cone normal per pixel from the object-space position. The mesh is a
                // triangle fan with a duplicated apex, so every apex vertex carries a slightly
                // different normal and interpolating them creates a crease along each triangle
                // edge — visible as hard wedges radiating down the beam.
                // The apex is at the origin and the base sits at -y, so the surface slope
                // radius/height is just |p.xz| / -p.y, constant over the whole cone.
                float2 radialOS = input.positionOS.xz;
                float radiusOS = max(length(radialOS), 1e-4);
                float2 aroundOS = radialOS / radiusOS;
                float slope = radiusOS / max(-input.positionOS.y, 1e-4);
                float3 normalOS = normalize(float3(aroundOS.x, slope, aroundOS.y));

                half3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));
                half3 viewDirWS = normalize(input.viewDirWS);
                half facing = saturate(abs(dot(normalWS, viewDirWS)));
                half core = pow(facing, _SoftEdgePower);
                half radial = lerp(1.0h, core, _SoftEdge);

                half amount = topFade * bottomFade * radial * _Intensity;
                return half4(_Color.rgb * _Color.a * amount, 0.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
