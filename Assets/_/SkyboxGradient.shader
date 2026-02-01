Shader "Custom/SkyboxGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.5, 0.7, 1, 1)
        _BottomColor ("Bottom Color", Color) = (1, 0.5, 0.3, 1)
        _GradientOffset ("Gradient Offset", Range(-1, 1)) = 0
        _GradientPower ("Gradient Power", Range(0.1, 5)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        
        Pass
        {
            Cull Front // Render only backfaces
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            
            float4 _TopColor;
            float4 _BottomColor;
            float _GradientOffset;
            float _GradientPower;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate gradient based on vertical position
                float gradient = normalize(i.worldPos).y;
                
                // Apply offset and power
                gradient = saturate((gradient + _GradientOffset) / 2.0);
                gradient = pow(gradient, _GradientPower);
                
                // Lerp between colors
                fixed4 color = lerp(_BottomColor, _TopColor, gradient);
                
                return color;
            }
            ENDCG
        }
    }
}
