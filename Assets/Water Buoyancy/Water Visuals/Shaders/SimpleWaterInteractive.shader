Shader "Unlit/Simple Water Interactive"
{
    Properties
    {
        _Color("Tint", Color) = (1, 1, 1, .5)
        _FoamC("Foam", Color) = (1, 1, 1, .5)
        _MainTex ("Main Texture", 2D) = "white" {}
		 [HideInInspector]_MaskInt ("RenderTexture Mask", 2D) = "white" {}
        _TextureDistort("Texture Wobble", range(0,1)) = 0.1
        _NoiseTex("Wooble Noise Noise", 2D) = "white" {}
        _Foam("Foamline Thickness", Range(0,10)) = 8
        _FoamScale("Foam Scale", Range(0,1)) = 0.5
        _WaveHeight("Wave Height", Range(0,1)) = 0.1
        _WaveSpeed("Wave Speed", Range(0,2)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue" = "Transparent" }
        LOD 100
        Blend OneMinusDstColor One
        Cull Off
       
        GrabPass{
            Name "BASE"
            Tags{ "LightMode" = "Always" }
                }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
           
            #include "UnityCG.cginc"
 
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
 
            struct v2f
            {
                float2 uv : TEXCOORD3;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 scrPos : TEXCOORD2;//
                float4 worldPos : TEXCOORD4;//
            };
            float _TextureDistort;
            float4 _Color;
            sampler2D _CameraDepthTexture; //Depth Texture
            sampler2D _MainTex, _NoiseTex;//
            float4 _MainTex_ST;
            float _WaveSpeed, _WaveHeight, _Foam, _FoamScale;//
            float4 _FoamC;
			sampler2D _MaskInt;

            float3 _Position;
			sampler2D _GlobalEffectRT;
			float _OrthographicCamSize;
 
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                //float4 tex = tex2Dlod(_NoiseTex, float4(v.uv.xy, 0, 0));//extra noise tex
                v.vertex.y += sin(_Time.y * _WaveSpeed + v.vertex.x + v.vertex.y + v.vertex.z) * _WaveHeight / unity_ObjectToWorld[1].y; // Same algorithm that shader's
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
               
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.scrPos = ComputeScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);        
                return o;
            }
           
            fixed4 frag (v2f i) : SV_Target
            {
				// rendertexture UV
				float2 uv = i.worldPos.xz - _Position.xz;
				uv = uv/(_OrthographicCamSize *2);
				uv += 0.5;
				// Ripples
				float ripples = tex2D(_GlobalEffectRT, uv ).b;

				// mask to prevent bleeding
				float4 mask = tex2D(_MaskInt, uv);				
				ripples *= mask.a;


                fixed distortx = tex2D(_NoiseTex, (i.worldPos.xz * _FoamScale)  + (_Time.x * 2)).r ;// distortion 
				distortx +=  (ripples *2);
           
                half4 col = tex2D(_MainTex, (i.worldPos.xz * _FoamScale) - (distortx * _TextureDistort));// texture times tint;        
                half depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos ))); // depth
                half4 foamLine =1 - saturate(_Foam* (depth - i.scrPos.w ) ) ;// foam line by comparing depth and screenposition
                col *= _Color;
                col += (step(0.4 * distortx,foamLine) * _FoamC); // add the foam line and tint to the texture
                col = saturate(col) * col.a ;
               
			   ripples = step(0.99, ripples * 3);
			   float4 ripplesColored = ripples * _FoamC;
			   
               return   saturate(col + ripplesColored);
            }
            ENDCG
        }
    }
}