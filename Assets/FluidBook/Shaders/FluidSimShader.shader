Shader "FluidBook/FluidSimShader" {
    Properties {
        _MainTex ("Texture", 2D) = "black" {}
        _SecondTex ("SecondTex", 2D) = "black" {}
        _NoiseTex ("NoiseTex", 2D) = "white" {}
        _VelocityInputTex ("VelocityInputTex", 2D) = "white" {}
    }
    SubShader {

        ZWrite Off

        CGINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        sampler2D _SecondTex;
        float4 _SecondTex_TexelSize;

        struct VertexInput {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct VertexOutput {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        VertexOutput VertexProgram(VertexInput v) {
            VertexOutput o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        ENDCG

        Pass {//Pass 0: Add density
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            //density1 is main
            //global in is second

            float _DensityFadingRatio;

            float4 FragmentProgram (VertexOutput i) : SV_Target {
                
                float4 main = tex2D(_MainTex, i.uv);
                float4 second = tex2D(_SecondTex, i.uv);

                float alphaRatio = second.a / (second.a + main.a);
                float4 sum = lerp(main, second, alphaRatio);

                sum = clamp(sum, 0, 1.0 - (0.0 * 0.5)) * _DensityFadingRatio;

                return sum;
            }
            ENDCG
        }

        Pass {//Pass 1: Add velocity
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram


            //velocity1 is main
            //global in is second
            //velocity input is third

            sampler2D _NoiseTex;
            float4 _NoiseTex_TexelSize;

            sampler2D _VelocityInputTex;
            float4 _VelocityInputTex_TexelSize;

            float _VelocityStrength;
            float2 _VelocityNoiseScrollSpeed;

            float4 FragmentProgram (VertexOutput i) : SV_Target {

                //float param1 is velocity strength

                
                float4 main = tex2D(_MainTex, i.uv);
                float4 second = tex2D(_SecondTex, i.uv);
                float4 third = tex2D(_VelocityInputTex, i.uv);
                float noise = tex2D(_NoiseTex, i.uv + _Time.x * _VelocityNoiseScrollSpeed).r;
                //float2 velocity = (third.rg * 2.0 - 1.0) * noise * _VelocityStrength;
                //float4 final = main + float4(second.a  * velocity.rg, 0.0, 0.0);

               float2 velocity = (third.rg * 2.0 - 1.0) * noise * _VelocityStrength;
               float4 final = float4((((main.rg * 2.0 - 1.0) + velocity * second.a) + 1.0) * 0.5, 0.0, 0.0);

                

                return final;
            }
            ENDCG
        }

        Pass {//Pass 2: Advect velocity
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            //velocity1 is main

            float _VelocityFadingRatio;

            float4 FragmentProgram (VertexOutput i) : SV_Target {
                
                float4 main = tex2D(_MainTex, i.uv);

                float2 uvs = i.uv.xy - main.rg * _MainTex_TexelSize.xy;
                //float2 uvs = i.uv.xy + (main.rg * 2.0 - 1.0) * _MainTex_TexelSize.xy;
                float4 advectedVelocity = tex2D(_MainTex, uvs);
                advectedVelocity *= _VelocityFadingRatio;

                return advectedVelocity;
            }
            ENDCG
        }

        Pass {//Pass 3: Advect density
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            //density2 is main
            //velocity1 is second

            float4 FragmentProgram (VertexOutput i) : SV_Target {

                float4 second = tex2D(_SecondTex, i.uv);

                float2 uvs = i.uv - second.rg * _SecondTex_TexelSize.xy;
                float4 advectedDensity = tex2D(_MainTex, uvs);
                advectedDensity *= 0.99;

                return advectedDensity;
            }
            ENDCG
        }

        Pass {//Pass 4: divergence
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            //velocity1 is main

            float4 FragmentProgram (VertexOutput i) : SV_Target {

                float2 uvRight = float2(i.uv.x + _MainTex_TexelSize.x, i.uv.y);
                float2 uvLeft = float2(i.uv.x - _MainTex_TexelSize.x, i.uv.y);
                float2 uvUp = float2(i.uv.x, i.uv.y + _MainTex_TexelSize.y);
                float2 uvDown = float2(i.uv.x, i.uv.y - _MainTex_TexelSize.y);
                
                float divX = tex2D(_MainTex, uvRight).r - tex2D(_MainTex, uvLeft).r;
                float divY = tex2D(_MainTex, uvUp).g - tex2D(_MainTex, uvDown).g;
                float div = (divX + divY) * 0.5;

                return float4(div, 0, 0, 0);
            }
            ENDCG
        }

        Pass {//Pass 5: pingpong
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            //ping : 
            //pressure is main
            //divergence is second

            //pong : 
            //pressuretemp is main
            //divergence is second

            float4 FragmentProgram (VertexOutput i) : SV_Target {

                float2 uvRight = float2(i.uv.x + _MainTex_TexelSize.x, i.uv.y);
                float2 uvLeft = float2(i.uv.x - _MainTex_TexelSize.x, i.uv.y);
                float2 uvUp = float2(i.uv.x, i.uv.y + _MainTex_TexelSize.y);
                float2 uvDown = float2(i.uv.x, i.uv.y - _MainTex_TexelSize.y);
                
                float4 sumX = tex2D(_MainTex, uvRight) + tex2D(_MainTex, uvLeft);
                float4 sumY = tex2D(_MainTex, uvUp) + tex2D(_MainTex, uvDown);
                float4 sum = sumX + sumY;

                sum -= tex2D(_SecondTex, i.uv);
                sum *= 0.25;

                return sum;
            }
            ENDCG
        }


        Pass {//Pass 6: gradient sub
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            
            //pressure is main
            //velocity1 is second

            float4 FragmentProgram (VertexOutput i) : SV_Target {

                float2 uvRight = float2(i.uv.x + _MainTex_TexelSize.x, i.uv.y);
                float2 uvLeft = float2(i.uv.x - _MainTex_TexelSize.x, i.uv.y);
                float2 uvUp = float2(i.uv.x, i.uv.y + _MainTex_TexelSize.y);
                float2 uvDown = float2(i.uv.x, i.uv.y - _MainTex_TexelSize.y);

                float4 second = tex2D(_SecondTex, i.uv);
                
                float subX = second.r - (tex2D(_MainTex, uvRight).r - tex2D(_MainTex, uvLeft).r) * 0.5;
                float subY = second.g - (tex2D(_MainTex, uvUp).r - tex2D(_MainTex, uvDown).r) * 0.5;
                float4 sum = float4(subX, subY, second.ba);

                return sum;
            }
            ENDCG
        }

        Pass {//Pass 7: -1 1 to 0 1
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            
            

            float4 FragmentProgram (VertexOutput i) : SV_Target {

                float4 main = tex2D(_MainTex, i.uv);
                main = (main + 1) * 0.5;

                return float4(main.rg, 0,0);
            }
            ENDCG
        }


        Pass {//Pass 8: add velocity for export
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram
            

            float4 FragmentProgram (VertexOutput i) : SV_Target {

                //float param1 is velocity strength

                
                float4 main = tex2D(_MainTex, i.uv);

                

                return (main + 1.0) * 0.5;;
            }

            
            ENDCG
        }
    }
}
