Shader "Terrain/QuadTree/TerrainRender" 
{
	Properties 
    {
        _Color("Color Tint",Color) = (1,1,1,1)
		_MainTex ("Main Tex", 2D) = "white" {}
        _DetailTex("Detail Tex",2D) = "white"{}
	}
	SubShader 
    {
    
		Tags { "LightMode"="ForwardBase" }

        Cull Off
        
        
		
        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma vertex vert 
            #pragma fragment frag

            fixed4 _Color ; 
            sampler2D _MainTex ; 
            float4 _MainTex_ST ; 
            sampler2D _DetailTex ;
            float4 _DetailTex_ST ; 
            
            
            struct a2v
            {
                float4  vertex : POSITION ; 
                float4  normal : NORMAL ;
                float4  texcoord : TEXCOORD0; 
            } ; 
            
            struct v2f
            {
                float4 pos : SV_POSITION ; 
                float3 worldNormal : TEXCOORD0 ;
                float3 worldPos : TEXCOORD1 ;
                float2 uv : TEXCOORD2 ;
            
            } ;
            
            v2f vert(a2v v)
            {
                v2f o ; 
                o.pos = mul(UNITY_MATRIX_MVP,v.vertex) ; 
                o.worldNormal =  mul( v.normal,(float3x3)_World2Object) ; 
                o.worldPos = mul(_Object2World,v.vertex).xyz ; 
                o.uv = v.texcoord.xy  * _MainTex_ST.xy + _MainTex_ST.zw ; 
                return o ; 
            }
            
            fixed4 frag(v2f i ): SV_Target
            {
                
                fixed3 color1 = tex2D(_MainTex,i.uv).rgb ;
                fixed3 color2 = tex2D(_DetailTex,i.uv).rgb ; 
                fixed3 finalColor = color1 * ( color2 * 2 ) ;
                
                return fixed4(finalColor,1.0) ; 
               
            }
            
            ENDCG          
        }
	} 
	FallBack "Diffuse"
}
