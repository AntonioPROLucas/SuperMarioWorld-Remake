Shader "UI/IrisHole"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Center ("Center Point", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Range(0, 1.5)) = 0.5
        _AspectRatio ("Aspect Ratio", Float) = 1.77
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            float4 _Color;
            float4 _Center;
            float _Radius;
            float _AspectRatio;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Ajusta a coordenada UV baseada na proporção da tela (para o círculo não virar um ovo)
                float2 pos = i.uv;
                pos.x = (pos.x - 0.5) * _AspectRatio + 0.5;
                float2 center = _Center.xy;
                center.x = (center.x - 0.5) * _AspectRatio + 0.5;

                // Calcula a distância do pixel atual até o centro (Mario)
                float dist = distance(pos, center);
                
                // Se estiver longe do centro, pinta de preto. Se estiver perto, fica transparente.
                // O smoothstep deixa a borda levemente suave (anti-serrilhado)
                float alpha = smoothstep(_Radius - 0.05, _Radius, dist);
                
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}