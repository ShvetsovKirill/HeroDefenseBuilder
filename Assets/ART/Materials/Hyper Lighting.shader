Shader "Core/Hyper Lighting"
{
  Properties
  {
    _LightDirection ("Light Direction", Vector) = (0.25,1,-0.25,1)
    _Color ("Color", Color) = (1,1,1,1)
    _Brightness ("brightness", Range(0,1)) = 0.4
    _shadow ("shadow strength", Range(0,1)) = 0.4
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
  }

  SubShader
  {
    Tags { "RenderType" = "Opaque" }
    LOD 200

    //Blend SrcAlpha OneMinusSrcAlpha 
    Pass
    {

      CGPROGRAM

      // Define name of vertex shader
      #pragma vertex vert
      // Define name of fragment shader
      #pragma fragment frag

      // Include some common helper functions, such as UnityObjectToClipPos
       #pragma multi_compile_fog
      #include "UnityCG.cginc"

      float4 _Color;
      float _shadow;
      float _Brightness;

      // Color Diffuse Map
      sampler2D _MainTex;
      // Tiling/Offset for _MainTex, used by TRANSFORM_TEX in vertex shader
      float4 _MainTex_ST;

      fixed4 _LightDirection;

      // This is the vertex shader input: position, UV0, UV1, normal
      struct appdata
      {
        float4 vertex   : POSITION;
        float2 texcoord : TEXCOORD0;
        float3 normal: NORMAL;
      };

      // This is the data passed from the vertex to fragment shader
      struct v2f
      {
        float4 pos  : SV_POSITION;
        float2 txuv : TEXCOORD0;
         UNITY_FOG_COORDS(1)
        float3 normalDir : TEXCOORD2;
      };

      // This is the vertex shader
      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.txuv = TRANSFORM_TEX(v.texcoord.xy,_MainTex);

        // Calculating normal so it can be used for fake lighting
        // in the fragment shader
        float4x4 modelMatrixInverse = unity_WorldToObject;
        o.normalDir = normalize(mul(float4(v.normal, 0.0), modelMatrixInverse).xyz);

        UNITY_TRANSFER_FOG(o,o.pos);
        return o;
      }

      // This is the fragment shader
      half4 frag(v2f i) : COLOR
      {
        // Reading color from diffuse texture
        half4 col = tex2D(_MainTex, i.txuv.xy);

        // Using hard-coded light direction for fake lighting
        half3 th = normalize(half3(_LightDirection.x, _LightDirection.y, _LightDirection.z));
        // Fake lighting
        float lightVal = dot (i.normalDir, th);

        // Add in a general brightness (similar to ambient/gamma) and then
        // calculate the final color of the pixel
        //col.rgb = col.rgb * _Color + lightVal * _Brightness;
        col.rgb = col.rgb * _Color * max(lerp(1.5,0,_shadow),lightVal) + lightVal * _Brightness;//- _Brightness;//_IlluminationBright * globalVal;//;max(_IlluminationBright,lightVal);

        UNITY_APPLY_FOG(i.fogCoord, col);
        return col;
      }

      ENDCG
    }
  }
}