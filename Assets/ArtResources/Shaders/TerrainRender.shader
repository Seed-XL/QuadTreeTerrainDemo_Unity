Shader "Terrain/QuadTree/TerrainRender" 
{
	Properties 
    {
        _Color("Color Tint",Color) = (1,1,1,1)
		_MainTex ("Main Tex", 2D) = "white" {}
        _Specular("Specular",Color) = (1,1,1,1)
        _Gloss ("Gloss",Range(8.0,256)) = 20 
	}
	SubShader 
    {
    
		Tags { "LightMode"="ForwardBase" }

		
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
            fixed4 _Specular ; 
            float _Gloss ; 
            
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
                //fixed3 worldNormal = normalize(i.worldNormal) ; 
                //fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz); 
                
                //fixed3 albdeo = tex2D(_MainTex,i.uv).rgb * _Color.rgb ; 
                
                //fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albdeo ; 
                //fixed3 diffuse = _LightColor0.rgb * albdeo * max(0,dot(worldNormal,worldLightDir)) ; 
                
                //fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz) ; 
                //fixed3 halfDir = normalize(worldLightDir + viewDir); 
                //fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0,dot(worldNormal,halfDir)),_Gloss) ; 
                
                //return fixed4(ambient + diffuse + specular,1.0) ; 

                fixed3 albdeo =  tex2D(_MainTex,i.uv).rgb ;
                return fixed4(albdeo,1.0) ; 
               
            }
            
            ENDCG          
        }
	} 
	FallBack "Diffuse"
}
