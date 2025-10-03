Shader "Unlit/SimpleVertexColor"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1) // インスペクタでの調整用（今回は使いません）
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
                fixed4 color : COLOR; // ← 頂点カラーを受け取る
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR; // ← フラグメントシェーダーに色を渡す
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color; // ← 頂点カラーをそのままパススルー
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color; // ← 受け取った色でピクセルを塗る
            }
            ENDCG
        }
    }
}