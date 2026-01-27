// Made with Amplify Shader Editor v1.9.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MG/Matcap_VertexOffset"
{
	Properties
	{
		_Matcap("Matcap", 2D) = "white" {}
		_VertexOffset_Tex("VertexOffset_Tex", 2D) = "white" {}
		_VertexOffsetInt("VertexOffsetInt", Float) = 0
		_Speed_XU_YV("Speed_XU_YV", Vector) = (0,0,0,0)
		_VertexOffset_Mask("VertexOffset_Mask", 2D) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		ZTest LEqual
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			half3 worldNormal;
		};

		uniform sampler2D _VertexOffset_Tex;
		uniform float4 _VertexOffset_Tex_ST;
		uniform half2 _Speed_XU_YV;
		uniform sampler2D _VertexOffset_Mask;
		uniform float4 _VertexOffset_Mask_ST;
		uniform half _VertexOffsetInt;
		uniform sampler2D _Matcap;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 uv_VertexOffset_Tex = v.texcoord.xy * _VertexOffset_Tex_ST.xy + _VertexOffset_Tex_ST.zw;
			half2 panner36 = ( 1.0 * _Time.y * _Speed_XU_YV + uv_VertexOffset_Tex);
			float2 uv_VertexOffset_Mask = v.texcoord.xy * _VertexOffset_Mask_ST.xy + _VertexOffset_Mask_ST.zw;
			half3 ase_vertexNormal = v.normal.xyz;
			v.vertex.xyz += ( ( tex2Dlod( _VertexOffset_Tex, float4( ( uv_VertexOffset_Tex + panner36 ), 0, 0.0) ) * tex2Dlod( _VertexOffset_Mask, float4( uv_VertexOffset_Mask, 0, 0.0) ) ) * half4( ase_vertexNormal , 0.0 ) * _VertexOffsetInt ).rgb;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			half3 ase_worldNormal = i.worldNormal;
			o.Emission = tex2D( _Matcap, ( ( mul( UNITY_MATRIX_V, half4( ase_worldNormal , 0.0 ) ).xyz * 0.5 ) + 0.5 ).xy ).rgb;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float3 worldPos : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldNormal = worldNormal;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldNormal = IN.worldNormal;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19100
Node;AmplifyShaderEditor.ViewMatrixNode;21;-1119.276,47.42967;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-971.5502,205.5904;Float;False;Constant;_Float0;Float 0;-1;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-960.7327,77.2156;Inherit;False;2;2;0;FLOAT4x4;0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;-793.4805,113.6614;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;24;-618.1711,163.7383;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-49.57976,164.356;Half;False;True;-1;2;ASEMaterialInspector;0;0;Standard;MG/Matcap_VertexOffset;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;3;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;0;4;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;1;False;;1;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;-248.2664,422.1288;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-526.873,419.1092;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;29;-932.7012,715.1068;Inherit;True;Property;_VertexOffset_Mask;VertexOffset_Mask;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NormalVertexDataNode;31;-586.6013,703.5711;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-1103.769,378.5883;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;35;-1603.15,491.8589;Inherit;False;Property;_Speed_XU_YV;Speed_XU_YV;3;0;Create;True;0;0;0;False;0;False;0,0;0.1,0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;36;-1302.696,499.5249;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;1;-422.7262,148.4586;Inherit;True;Property;_Matcap;Matcap;0;0;Create;True;0;0;0;False;0;False;-1;None;a300b6743556fc445a0ea511a0da166e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;1,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;30;-878.4431,390.8881;Inherit;True;Property;_VertexOffset_Tex;VertexOffset_Tex;1;0;Create;True;0;0;0;False;0;False;-1;None;42828cc03db012f4cb3a28cbe1c767c8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;33;-449.0777,652.7593;Inherit;False;Property;_VertexOffsetInt;VertexOffsetInt;2;0;Create;True;0;0;0;False;0;False;0;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;34;-1552.807,347.6414;Inherit;False;0;30;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;28;-1219.33,772.1801;Inherit;False;0;29;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldNormalVector;26;-1199.464,140.5635;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
WireConnection;18;0;21;0
WireConnection;18;1;26;0
WireConnection;22;0;18;0
WireConnection;22;1;23;0
WireConnection;24;0;22;0
WireConnection;24;1;23;0
WireConnection;0;2;1;0
WireConnection;0;11;37;0
WireConnection;37;0;32;0
WireConnection;37;1;31;0
WireConnection;37;2;33;0
WireConnection;32;0;30;0
WireConnection;32;1;29;0
WireConnection;29;1;28;0
WireConnection;27;0;34;0
WireConnection;27;1;36;0
WireConnection;36;0;34;0
WireConnection;36;2;35;0
WireConnection;1;1;24;0
WireConnection;30;1;27;0
ASEEND*/
//CHKSM=47188C96555F2FE9B065477488ACCDF290721950