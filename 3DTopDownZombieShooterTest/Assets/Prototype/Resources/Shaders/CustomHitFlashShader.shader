Shader"Custom/ZombieHitFlash"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _HitFlash ("Hit Flash", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM
        #pragma surface surf Standard

sampler2D _MainTex;
fixed4 _Color;
float _HitFlash;

struct Input
{
    float2 uv_MainTex;
};

void surf(Input IN, inout SurfaceOutputStandard o)
{
    fixed4 color = tex2D(_MainTex, IN.uv_MainTex);
    color *= _Color;

    o.Albedo = lerp(
                color.rgb,
                fixed3(1, 1, 1),
                _HitFlash
            );
}

        ENDCG
    }

FallBack"Standard"
}