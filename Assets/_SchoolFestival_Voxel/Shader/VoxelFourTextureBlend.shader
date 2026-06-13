Shader "Custom/VoxelFourTextureBlend"
{
    Properties
    {
        _Tex0 ("Texture 0 (Grass) [R]", 2D) = "white" {}
        _Tex1 ("Texture 1 (Rock) [G]", 2D) = "white" {}
        _Tex2 ("Texture 2 (Soil) [B]", 2D) = "white" {}
        _Tex3 ("Texture 3 (Sand) [A]", 2D) = "white" {}
        _Tiling ("Tiling Factor", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                fixed4 color : COLOR;
            };

            sampler2D _Tex0;
            sampler2D _Tex1;
            sampler2D _Tex2;
            sampler2D _Tex3;
            float _Tiling;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 法線の正規化
                float3 normal = normalize(i.worldNormal);
                
                // 三面投影 (Triplanar Mapping) のウェイト計算
                // 各軸の法線の絶対値をウェイトとし、合計が 1 になるように正規化します
                float3 blendWeights = abs(normal);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                // 各軸からの投影UV (ワールド座標をテクスチャ座標として利用)
                float2 uvX = i.worldPos.zy * _Tiling;
                float2 uvY = i.worldPos.xz * _Tiling;
                float2 uvZ = i.worldPos.xy * _Tiling;

                // テクスチャ0 (Rチャネル用) の三面投影サンプリング
                fixed4 col0 = tex2D(_Tex0, uvX) * blendWeights.x +
                              tex2D(_Tex0, uvY) * blendWeights.y +
                              tex2D(_Tex0, uvZ) * blendWeights.z;

                // テクスチャ1 (Gチャネル用) の三面投影サンプリング
                fixed4 col1 = tex2D(_Tex1, uvX) * blendWeights.x +
                              tex2D(_Tex1, uvY) * blendWeights.y +
                              tex2D(_Tex1, uvZ) * blendWeights.z;

                // テクスチャ2 (Bチャネル用) の三面投影サンプリング
                fixed4 col2 = tex2D(_Tex2, uvX) * blendWeights.x +
                              tex2D(_Tex2, uvY) * blendWeights.y +
                              tex2D(_Tex2, uvZ) * blendWeights.z;

                // テクスチャ3 (Aチャネル用) の三面投影サンプリング
                fixed4 col3 = tex2D(_Tex3, uvX) * blendWeights.x +
                              tex2D(_Tex3, uvY) * blendWeights.y +
                              tex2D(_Tex3, uvZ) * blendWeights.z;

                // 頂点カラー (R, G, B, A) を使ってテクスチャカラーをブレンド
                // 頂点カラーはGPUによってピクセル間で滑らかに補間されます
                fixed4 blendedTexColor = col0 * i.color.r + 
                                         col1 * i.color.g + 
                                         col2 * i.color.b + 
                                         col3 * i.color.a;

                // 簡単なディフューズライティング + アンビエント
                float ndotl = saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                fixed3 lightColor = _LightColor0.rgb;
                fixed3 ambient = ShadeSH9(float4(normal, 1.0));
                
                fixed3 finalColor = blendedTexColor.rgb * (ndotl * lightColor + ambient);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
