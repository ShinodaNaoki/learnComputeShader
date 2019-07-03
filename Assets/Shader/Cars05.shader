 Shader "Custom/CarsStatichader05" {
	SubShader {
        Tags {"Queue" = "Geometry" "RenderType" = "Opaque"}
        
        Pass {
	        Tags {"LightMode" = "ForwardBase"}
			LOD 200
			
			CGPROGRAM
	        
	        // シェーダーモデルは5.0を指定
	        #pragma target 5.0
	        
	        // シェーダー関数を設定 
	        #pragma vertex vert fullforwardshadows
			#pragma geometry geom
	        #pragma fragment frag
	         
	        #include "UnityCG.cginc"
	        
	        // テクスチャ
        	sampler2D _MainTex;
        	
			// 光源
			fixed4 _LightColor0;

        	// 車の構造体
			struct CarS
			{
				float3 size;
				float4 col;
        float idealVelocity;
        float mobility;
			};
						
        	// 車の構造体
			struct CarD
			{
				float2 pos;
				float2 dir;
				float veloc;
        int lane;
        int colider;
        float ticks;
			};

        	
        	// 車の構造化バッファ
        	StructuredBuffer<CarS> CarsStatic;
        	StructuredBuffer<CarD> CarsDynamic;
	        
	        // 頂点シェーダからの出力
	        struct VSOut {
	            float4 pos : SV_POSITION;
	            float2 tex : TEXCOORD0;
	            float4 col : COLOR;
	        };
	        
	        // 頂点シェーダ
			VSOut vert (uint id : SV_VertexID)
	       	{
				CarD car = CarsDynamic[id];

				// idを元に、車の情報を取得
	            VSOut output;
	            output.pos = float4(car.pos.x, 0.0f, car.pos.y, 1);
	            output.tex = float2(id, 0);
	            output.col = CarsStatic[id].col;
	             
	            return output;
	       	}
	       	
			// 頂点設定
			inline void setVertex(inout VSOut output, in float4 pos, in float3 offset, in float4 col, in float2x2 mat) 
			{
			    // 色
			    output.col = col;
				// テクスチャ座標
			    output.tex = normalize(offset.xy);
			    
			    // 頂点位置を計算
				output.pos = pos + float4(mul(offset.xz, mat), offset.y, 1).xzyw;
			    output.pos = mul (UNITY_MATRIX_VP, output.pos);
			}

			// ライティング設定
			inline void setLighting(inout VSOut output, in half3 normal) 
			{
				half3 worldNormal = UnityObjectToWorldNormal(normal);
				// 適用量
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                fixed4 diff = nl * _LightColor0;
			    diff.rgb += ShadeSH9(half4(worldNormal, 1));
			    // 色を補正
				output.col = output.col* diff;
			}

			// ベクトルY軸回転
			inline half3 rotate2d(in half3 normal, in float2x2 mat)
			{
				return half3(mul(normal.xz, mat), normal.y).xzy;
			}


	       	// ジオメトリシェーダ
		   	[maxvertexcount(21)]
		   	void geom (point VSOut input[1], inout TriangleStream<VSOut> outStream)
		   	{
		     	VSOut output;
		     	
		   		// 全ての頂点で共通の値を計算しておく
		      	float4 pos = input[0].pos;
				float2 dir = CarsDynamic[input[0].tex.x].dir;
				float3 size =  CarsStatic[input[0].tex.x].size;
		      	float4 clBody = input[0].col;
				float4 clWind = float4(0.1,0.1,0.1,1.0);

			    // Y軸回転の行列を作る
				float angle = atan2(-dir.x, -dir.y);
				float sina, cosa;
                sincos(angle, sina, cosa);
                float2x2 _matrix = float2x2(cosa, -sina, sina, cosa);		     	

				// 法線を準備
				half3 up = half3(0,1,0);
				half3 front = rotate2d(half3(0,0,-1), _matrix);
				half3 left = rotate2d(half3(-1,0,0), _matrix);
				half3 right = rotate2d(half3(1,0,0), _matrix);
				half3 back = rotate2d(half3(0,0,1), _matrix);

		      	outStream.RestartStrip();

				// ポリゴン生成

				// 背面
				setVertex(output, pos, float3(0, 0.1, 0.5) * size, clBody, _matrix);
				setLighting(output, back);
				outStream.Append (output);
				setVertex(output, pos, float3(0.5, 1.0, 0.5) * size, clBody, _matrix);
				setLighting(output, back);
				outStream.Append (output);
				setVertex(output, pos, float3(-0.5, 1.0, 0.5) * size, clBody, _matrix);
				setLighting(output, back);
				outStream.Append (output);

				// 上面
				setVertex(output, pos, float3(0, 1.0, -0.5) * size, clBody, _matrix);
				setLighting(output, up);
				outStream.Append (output);
				setVertex(output, pos, float3(0.5, 1.0, 0.5) * size, clBody, _matrix);
				setLighting(output, up);
				outStream.Append (output);
				setVertex(output, pos, float3(-0.5, 1.0, 0.5) * size, clBody, _matrix);
				setLighting(output, up);
				outStream.Append (output);

				// 前面
				setVertex(output, pos, float3(0, 1.0, -0.5) * size, clBody, _matrix);
				setLighting(output, front);
				outStream.Append (output);
				setVertex(output, pos, float3(0.5, 0.1, -0.5) * size, clBody, _matrix);
				setLighting(output, front);
				outStream.Append (output);
				setVertex(output, pos, float3(-0.5, 0.1, -0.5) * size, clBody, _matrix);
				setLighting(output, front);
				outStream.Append (output);
				
				// 左側面
				setVertex(output, pos, float3(-0.5, 0.1, -0.5) * size, clBody, _matrix);
				setLighting(output, left);
				outStream.Append (output);
				setVertex(output, pos, float3(-0.5, 1.0, 0.5) * size, clBody, _matrix);
				setLighting(output, left);
				outStream.Append (output);
				setVertex(output, pos, float3(-0.5, 0.1, 0.5) * size, clBody, _matrix);
				setLighting(output, left);
				outStream.Append (output);
				
				// 右側面
				setVertex(output, pos, float3(0.5, 0.1, -0.5) * size, clBody, _matrix);
				setLighting(output, right);
				outStream.Append (output);
				setVertex(output, pos, float3(0.5, 1.0, 0.5) * size, clBody, _matrix);
				setLighting(output, right);
				outStream.Append (output);
				setVertex(output, pos, float3(0.5, 0.1, 0.5) * size, clBody, _matrix);
				setLighting(output, right);
				outStream.Append (output);
				
		      	// トライアングルストリップを終了
		      	outStream.RestartStrip();

				left = rotate2d(half3(-1,-1,-0.5), _matrix);
				right = rotate2d(half3(1,-1,0.5), _matrix);

				// 右斜面
				setVertex(output, pos, float3(0, 1, -0.5) * size, clWind, _matrix);
				setLighting(output, left);
				outStream.Append (output);
				setVertex(output, pos, float3(0.5, 1, 0.5) * size, clWind, _matrix);
				setLighting(output, left);
				outStream.Append (output);
				setVertex(output, pos, float3(0.5, 0.1, -0.5) * size, clWind, _matrix);
				setLighting(output, left);
				outStream.Append (output);
		      	
				// 左斜面
				setVertex(output, pos, float3(0, 1, -0.5) * size, clWind, _matrix);
				setLighting(output, right);
				outStream.Append (output);
				setVertex(output, pos, float3(-0.5, 1, 0.5) * size, clWind, _matrix);
				setLighting(output, right);
				outStream.Append (output);
				setVertex(output, pos, float3(-0.5, 0.1, -0.5) * size, clWind, _matrix);
				setLighting(output, right);
				outStream.Append (output);

		      	// トライアングルストリップを終了
				outStream.RestartStrip();
		   	}
			
			// ピクセルシェーダー
	        fixed4 frag (VSOut i) : COLOR
	        {
		        // 色を返す
	            return i.col;
	        }
	         
	        ENDCG
	    }
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
     }
 }