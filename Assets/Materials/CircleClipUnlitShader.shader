// Unity Shaders are written in a language called HLSL.
Shader "Custom/CircleClipUnlitShader"
{
    Properties
    {
        // Unity 6でのエラーを回避するため、Tooltip属性をすべて削除しました
        _Color ("Tint Color", Color) = (1,1,1,1)

        [MainTexture]
        _MainTex ("Texture", 2D) = "white" {}

        _Radius ("Radius", Range(0, 0.5)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            // --- Properties from above, now accessible in the shader code
            sampler2D _MainTex;
            float4 _MainTex_ST; // Unity uses this to handle tiling and offset
            fixed4 _Color;
            float _Radius;


            // Vertex Shader: Passes UVs to the fragment shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Correctly apply texture tiling and offset
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Fragment Shader: Determines the color of each pixel
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Calculate distance from center
                float dist = length(i.uv - float2(0.5, 0.5));

                // 2. Clip the pixel if it's outside the radius
                clip(_Radius - dist);

                // 3. Sample the color from the texture
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // 4. Multiply the texture color by the tint color
                texColor *= _Color;

                return texColor;
            }
            ENDCG
        }
    }
}