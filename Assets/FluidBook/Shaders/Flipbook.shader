Shader "FluidBook/Flipbook"
{
    Properties
    {
        _Flipbook ("Flipbook", 2D) = "white" {}
        //_Progress ("Progress", Range(0.0, 2.0)) = 0.0
        _Speed ("Speed Multiplier", Float) = 1.0
        _Columns ("Num Flipbook Columns", Float) = 8.0
        _Rows ("Num Flipbook Rows", Float) = 4.0
        _AnimationSpeed ("Animation Speed", Float) = 1.0

        [Toggle(USE_BLENDING)] _UseBlending("Use Blending", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM


            #pragma shader_feature USE_BLENDING

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

            sampler2D _Flipbook;
            float4 _Flipbook_ST;

            //float _Progress;
            float _Speed;
            float _Columns, _Rows;
            float _AnimationSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Flipbook);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float interFrameProgress = fmod(_Time.x * _AnimationSpeed, _Columns * _Rows);
                float currentIndex = floor(interFrameProgress);
                float coordX = floor(fmod(currentIndex, _Columns)) / _Columns;
                float coordY = floor(currentIndex / _Columns) / _Rows;

                
                float4 col = tex2D(_Flipbook, float2(i.uv.x / _Columns + coordX, i.uv.y / _Rows + coordY));

                #if defined(USE_BLENDING)
                    float nextIndex = floor(fmod(currentIndex + 1.0, _Columns * _Rows));
                    float nextCoordX = floor(fmod(nextIndex, _Columns)) / _Columns;
                    float nextCoordY = floor(nextIndex / _Columns) / _Rows;

                    float4 nextCol = tex2D(_Flipbook, float2(i.uv.x / _Columns + nextCoordX, i.uv.y / _Rows + nextCoordY));

                    float ratio = frac(interFrameProgress);

                    col = lerp(col, nextCol, ratio);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
