Shader "Custom/CleanArcWithCaps"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Angle ("Arc Angle", Range(0, 360)) = 90
        _Thickness ("Thickness", Range(0, 1)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0.001, 0.1)) = 0.001
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Angle;
            float _Thickness;
            float _Smoothness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2.0 - 1.0; // [-1,1] UV space
                return o;
            }

            // Smooth step function for antialiasing
            float smoothEdge(float edge, float value, float smoothness)
            {
                return smoothstep(edge - smoothness, edge + smoothness, value);
            }

            // Smooth step function for distance-based antialiasing
            float smoothDistance(float distance, float radius, float smoothness)
            {
                return 1.0 - smoothstep(radius - smoothness, radius + smoothness, distance);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float dist = length(uv);
                float angle = atan2(uv.y, uv.x); // [-pi, pi]
                angle = degrees(angle);
                if (angle < 0) angle += 360;

                float radius = 0.5;
                float halfThickness = _Thickness * 0.5;
                float inner = radius - halfThickness;
                float outer = radius + halfThickness;

                // ----- Antialiased arc band -----
                // Smooth transitions for inner and outer edges
                float innerMask = smoothEdge(inner, dist, _Smoothness);
                float outerMask = 1.0 - smoothEdge(outer, dist, _Smoothness);
                float inRing = innerMask * outerMask;

                // Smooth angle cutoff with special handling for wraparound
                float angleSmooth = _Smoothness * 5.0; // Make angle smoothing more pronounced
                float inAngle;
                
                if (_Angle >= 360.0 - angleSmooth)
                {
                    // Full circle case - no angle cutoff
                    inAngle = 1.0;
                }
                else
                {
                    // Regular angle cutoff with smoothing
                    inAngle = 1.0 - smoothEdge(_Angle, angle, angleSmooth);
                }

                float arcMask = inRing * inAngle;

                // ----- Antialiased rounded caps (only if angle > 0) -----
                float showCaps = smoothEdge(0.001, _Angle, 0.001);

                float2 dirStart = float2(cos(radians(0)), sin(radians(0)));
                float2 dirEnd = float2(cos(radians(_Angle)), sin(radians(_Angle)));

                float2 capStartPos = dirStart * radius;
                float2 capEndPos = dirEnd * radius;

                // Smooth distance-based caps
                float startCapDist = length(uv - capStartPos);
                float endCapDist = length(uv - capEndPos);

                float startCap = smoothDistance(startCapDist, halfThickness, _Smoothness) * showCaps;
                float endCap = smoothDistance(endCapDist, halfThickness, _Smoothness) * showCaps;

                // For full circles, don't show the end cap to avoid overlap
                float hideEndCap = smoothEdge(359.5, _Angle, 0.5);
                endCap *= (1.0 - hideEndCap);

                // Combine all masks and clamp to 1.0 to avoid overbright
                float finalMask = saturate(arcMask + startCap + endCap);

                // Apply additional smoothing based on pixel derivatives for screen-space antialiasing
                float2 pixelSize = fwidth(uv);
                float screenSpaceSmooth = max(pixelSize.x, pixelSize.y) * 2.0;
                float adaptiveSmooth = max(_Smoothness, screenSpaceSmooth);

                // Re-apply smoothing with adaptive smoothness for better screen-space antialiasing
                if (finalMask > 0.0 && finalMask < 1.0)
                {
                    // For pixels at the edge, apply additional smoothing
                    float edgeFactor = 1.0 - abs(finalMask * 2.0 - 1.0); // 0 at center/outside, 1 at edges
                    finalMask = lerp(finalMask, smoothstep(0.0, adaptiveSmooth * 2.0, finalMask), edgeFactor * 0.5);
                }

                return fixed4(_Color.rgb, _Color.a * finalMask);
            }
            ENDCG
        }
    }
}