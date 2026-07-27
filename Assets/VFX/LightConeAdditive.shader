Shader "InterrogationRoom/LightConeAdditive"
{
    Properties
    {
        [HDR] _Color ("Beam Colour", Color) = (1, 0.72, 0.42, 1)
        _Intensity ("Intensity", Range(0, 8)) = 1.4
        _SoftEdge ("Soft Edge", Range(0, 1)) = 0.85
        _SoftEdgePower ("Soft Edge Power", Range(0.5, 8)) = 1.8
        _TopFade ("Top Fade", Range(0, 1)) = 0.08
        _BottomFade ("End Fade", Range(0, 1)) = 0.12
        _BottomFadePower ("End Fade Power", Range(0.5, 6)) = 1.5
        _Falloff ("Distance Falloff", Range(0, 1)) = 0.6
        _FalloffPower ("Distance Falloff Power", Range(0.5, 4)) = 1.4
        _ContactFade ("Surface Contact Fade", Range(0, 2)) = 0.35
        _NearFade ("Camera Proximity Fade", Range(0, 2)) = 0.6
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float  eyeDepth   : TEXCOORD3;
                float  coneSlope  : TEXCOORD4;
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
                half _Falloff;
                half _FalloffPower;
                half _ContactFade;
                half _NearFade;
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
                // Every mesh normal shares the same slope, so recovering it from the y component
                // gives the fragment an exact value to rebuild the analytic normal with.
                float slopeY = input.normalOS.y;
                output.coneSlope = slopeY * rsqrt(max(1.0 - slopeY * slopeY, 1e-6));
                output.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                output.eyeDepth = -positions.positionVS.z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // uv.y runs 0 at the fitting aperture to 1 at the open base.
                half alongBeam = saturate(input.uv.y);

                // Fade in right under the fitting and hide the base ring, so neither open end
                // of the mesh reads as a hard edge.
                half topFade = smoothstep(0.0h, max(_TopFade, 1e-3h), alongBeam);
                half endFade = 1.0h - saturate((alongBeam - (1.0h - _BottomFade)) / max(_BottomFade, 1e-3h));
                endFade = pow(endFade, _BottomFadePower);

                // Airborne haze scatters less the further it sits from the bulb.
                half distanceFade = lerp(1.0h, saturate(1.0h - _Falloff), pow(alongBeam, _FalloffPower));

                // Interpolating the ring normals follows the chords rather than the circle, which
                // reads as faint spokes running down the beam, so rebuild the normal per pixel:
                // the slope is constant over the frustum and the radial direction is exact.
                float2 radialOS = input.positionOS.xz;
                float2 aroundOS = radialOS * rsqrt(max(dot(radialOS, radialOS), 1e-8));
                float3 normalOS = normalize(float3(aroundOS.x, input.coneSlope, aroundOS.y));

                half3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));
                half3 viewDirWS = normalize(input.viewDirWS);
                half facing = saturate(abs(dot(normalWS, viewDirWS)));
                half core = pow(facing, _SoftEdgePower);
                half radial = lerp(1.0h, core, _SoftEdge);

                // Dissolve where the shell meets solid geometry, otherwise the intersection with
                // the table and floor cuts a hard bright ellipse through the beam.
                half contact = 1.0h;
                if (unity_OrthoParams.w < 0.5h)
                {
                    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                    contact = saturate((sceneEyeDepth - input.eyeDepth) / max(_ContactFade, 1e-3h));
                }

                // Walking through the beam should not wash the whole screen out.
                half nearFade = saturate(input.eyeDepth / max(_NearFade, 1e-3h));

                half amount = topFade * endFade * distanceFade * radial * contact * nearFade * _Intensity;
                return half4(_Color.rgb * _Color.a * amount, 0.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
