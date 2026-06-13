Shader "Universal Render Pipeline/VoxelDitherBlend"
{
    Properties
    {
        _TextureArray ("Texture 2D Array (Albedo)", 2DArray) = "" {}
        _NormalArray ("Normal Map Array", 2DArray) = "" {}
        _TileScale ("Tile Scale", Float) = 1.0
        _DitherScale ("Dither Scale (World Space)", Float) = 32.0
        _BumpScale ("Bump Scale", Range(0, 2)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;     // r=Weight0, g=Weight1, b=Weight2, a=Weight3
                float4 uv1          : TEXCOORD1; // Unity maps mesh.uv2 to TEXCOORD1
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 color        : TEXCOORD3; // Weights (RGBA)
                float4 materialIDs  : TEXCOORD4; // Constant Material IDs (XYZW)
            };

            // Unity URP標準のマクロを使用したテクスチャ配列の宣言
            TEXTURE2D_ARRAY(_TextureArray);
            SAMPLER(sampler_TextureArray);

            TEXTURE2D_ARRAY(_NormalArray);
            SAMPLER(sampler_NormalArray);

            CBUFFER_START(UnityPerMaterial)
                float _TileScale;
                float _DitherScale;
                float _BumpScale;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(1.0, 1.0, 1.0, 1.0));

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.color = input.color;
                output.materialIDs = input.uv1; // IDsをピクセルシェーダーに伝達

                return output;
            }

            // 安定したワールド空間ディザ値を生成する
            float GetWorldSpaceDither(float3 positionWS, float scale)
            {
                float3 p = floor(positionWS * scale);
                float hash = frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453123);
                return hash;
            }

            // アルベドと法線マップのTriplanar（三方向投影）サンプリング
            void SampleTriplanar(float3 positionWS, float3 normalWS, float index, out float3 outAlbedo, out float3 outNormal)
            {
                // 法線の絶対値に基づく投影ブレンド比率の計算
                float3 blend = abs(normalWS);
                blend /= (blend.x + blend.y + blend.z + 0.0001);

                // 投影UVの計算
                float2 uvX = positionWS.zy * _TileScale;
                float2 uvY = positionWS.zx * _TileScale;
                float2 uvZ = positionWS.xy * _TileScale;

                // Unity標準マクロを使用してサンプリング
                float4 colX = SAMPLE_TEXTURE2D_ARRAY(_TextureArray, sampler_TextureArray, uvX, index);
                float4 colY = SAMPLE_TEXTURE2D_ARRAY(_TextureArray, sampler_TextureArray, uvY, index);
                float4 colZ = SAMPLE_TEXTURE2D_ARRAY(_TextureArray, sampler_TextureArray, uvZ, index);
                outAlbedo = (colX * blend.x + colY * blend.y + colZ * blend.z).rgb;

                // 法線アレイのサンプリング
                float4 rawNormalX = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, uvX, index);
                float4 rawNormalY = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, uvY, index);
                float4 rawNormalZ = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, uvZ, index);

                // URP組み込みの UnpackNormalScale を使用
                float3 tnormalX = UnpackNormalScale(rawNormalX, _BumpScale);
                float3 tnormalY = UnpackNormalScale(rawNormalY, _BumpScale);
                float3 tnormalZ = UnpackNormalScale(rawNormalZ, _BumpScale);

                // 面の法線向き（符号）に合わせて接法線を補正
                tnormalX = float3(tnormalX.xy * float2(sign(normalWS.x), 1.0), tnormalX.z);
                tnormalY = float3(tnormalY.xy * float2(sign(normalWS.y), 1.0), tnormalY.z);
                tnormalZ = float3(tnormalZ.xy * float2(sign(normalWS.z), 1.0), tnormalZ.z);

                // ワールド空間の法線に再配向
                float3 worldNormalX = float3(0.0, tnormalX.y, tnormalX.x) + float3(normalWS.x, 0.0, 0.0);
                float3 worldNormalY = float3(tnormalY.x, 0.0, tnormalY.y) + float3(0.0, normalWS.y, 0.0);
                float3 worldNormalZ = float3(tnormalZ.x, tnormalZ.y, 0.0) + float3(0.0, 0.0, normalWS.z);

                // 3方向の法線をブレンド
                outNormal = normalize(worldNormalX * blend.x + worldNormalY * blend.y + worldNormalZ * blend.z);
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 頂点間で補間されない定数IDをデコード (丸め処理で安全を保証)
                float id0 = round(input.materialIDs.x);
                float id1 = round(input.materialIDs.y);
                float id2 = round(input.materialIDs.z);
                float id3 = round(input.materialIDs.w);

                // 頂点カラーに格納された4スロット分の重み (RGBA)
                float w0 = input.color.r;
                float w1 = input.color.g;
                float w2 = input.color.b;
                float w3 = input.color.a;

                // 重みの正規化
                float totalW = w0 + w1 + w2 + w3 + 0.0001;
                w0 /= totalW;
                w1 /= totalW;
                w2 /= totalW;
                w3 /= totalW;

                // ワールド空間ディザの評価
                float ditherValue = GetWorldSpaceDither(input.positionWS, _DitherScale);

                // ディザリング選択（累積ブレンド比率に基づいて4つから1つを選択）
                float selectedIndex = id0;
                if (ditherValue < w0) {
                    selectedIndex = id0;
                } else if (ditherValue < w0 + w1) {
                    selectedIndex = id1;
                } else if (ditherValue < w0 + w1 + w2) {
                    selectedIndex = id2;
                } else {
                    selectedIndex = id3;
                }

                // アルベドと法線のTriplanarサンプルを実行
                float3 albedo;
                float3 normal;
                SampleTriplanar(input.positionWS, normalize(input.normalWS), selectedIndex, albedo, normal);

                // URPメインライトの計算
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                
                // 拡散反射光（Diffuse）の計算
                float diffuse = saturate(dot(normal, lightDir));
                
                // 鏡面反射光（Specular）の計算
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 halfDir = normalize(lightDir + viewDir);
                float specular = pow(saturate(dot(normal, halfDir)), 32.0) * 0.15; // 鏡面強度 0.15

                // 環境光とライトカラーの合成
                float3 ambient = 0.2 * mainLight.color;
                float3 color = albedo.rgb * (ambient + mainLight.color * diffuse) + mainLight.color * specular;

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
