Shader "Unlit/CloudShader"
{
    Properties
    {
        [HideInInspector]
        _MainTex ("MainTexture", 2D) = "white" {}
        _MarchStep("步进的长度",Float) = 0.01
        _NoiseMapOffset("贴图的偏移",Vector) = (0,0,0,0)
        _NoiseMapScale("贴图的缩放",Float) = 0
        [HideInInspector]
        _ShapeNoise("3D贴图",3D) = "white"{}
        _ColorDark("暗部的颜色",Color) = (0,0,0,0)
        _ColorBright("亮部的颜色",Color) = (1,1,0,0)
        _LightAbsorption("光纤的吸收率",Float) = 0.2
        _ColorOffset1("颜色偏移1",Float) = 1
        _ColorOffset2("颜色偏移2",Float) = 1
        _DarknessThredhold("明暗控制",Float) = 0
        _NoiseMapMul("云的密度控制",Float) = 0
        _PhaseParams("瑞丽散射",Vector) = (1,1,1,1)
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
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float3 BoundMin;
            float3 BoundMax;
            //Texture3D<float4> ShapeNoise;
            sampler3D _ShapeNoise;
            float3 _NoiseMapOffset;
            float _NoiseMapScale;
            float _MarchStep;
            fixed4 _ColorDark;
            fixed4 _ColorBright;
            float _LightAbsorption;
            float _ColorOffset1;
            float _ColorOffset2;
            float _DarknessThredhold;
            float _NoiseMapMul;
            float4 _PhaseParams;
            sampler2D _CameraDepthTexture;

            //解析深度，获取世界坐标
            float4 GetWorldPositionFromDepthValue( float2 uv, float linearDepth ) 
            {
                float camPosZ = _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * linearDepth;

                // unity_CameraProjection._m11 = near / t，其中t是视锥体near平面的高度的一半。
                // 投影矩阵的推导见：http://www.songho.ca/opengl/gl_projectionmatrix.html。
                // 这里求的height和width是坐标点所在的视锥体截面（与摄像机方向垂直）的高和宽，并且
                // 假设相机投影区域的宽高比和屏幕一致。
                float height = 2 * camPosZ / unity_CameraProjection._m11;
                float width = _ScreenParams.x / _ScreenParams.y * height;

                float camPosX = width * uv.x - width / 2;
                float camPosY = height * uv.y - height / 2;
                float4 camPos = float4(camPosX, camPosY, camPosZ, 1.0);
                return mul(unity_CameraToWorld, camPos);
            }
            //AABB盒检测
            float2 rayBoxDst(float3 boundMin,float3 boundMax,float3 rayOrigin,float3 invRayDir){
                float3 t0 = (boundMin - rayOrigin) * invRayDir;
                float3 t1 = (boundMax - rayOrigin) * invRayDir;
                float3 tmin = min(t0,t1);
                float3 tmax = max(t0,t1);    
                float dstA = max(max(tmin.x,tmin.y),tmin.z);
                float dstB = min(tmax.x,min(tmax.y,tmax.z));
                float dstToBox = max(0,dstA);
                float dstInsideBox = max(0,dstB - dstToBox);
                return float2(dstToBox,dstInsideBox);
            }
            //3D贴图采样
            float sampleDensity(float3 rayPos)
            {
                float3 uvw = rayPos * _NoiseMapScale+_NoiseMapOffset;
                uvw = frac(uvw);
                float4 shapeNoise = tex3D(_ShapeNoise,uvw);
                float col =  saturate(shapeNoise.r*_NoiseMapMul);
                
                return col;
            }
            float hg(float a,float g)
            {
                float g2 = g*g;
                return (1-g2)/(4*3.1415*pow(1 + g2 - 2 * g * a, 1.5));
            }
            float phase(float a)
            {
                float blend = 0.5;
                float hgBlend = hg(a,_PhaseParams.x)*(1-blend) + hg(a,_PhaseParams.y)*blend;
                return _PhaseParams.z +hgBlend*_PhaseParams.w;
            }
            //向光源光线步进
            float3 LightMarching(float3 position)
            {
                //在AABB框内求步进
                float3 dirTolight = _WorldSpaceLightPos0.xyz;
                float dstInsideBox = rayBoxDst(BoundMin,BoundMax,position,1/dirTolight);
                float stepSize = dstInsideBox/8;
                float totalDensity = 0;
                //向光源步进来对光照进行采样
                for(int step = 0;step<8;step++)
                {
                    position += dirTolight*stepSize;
                    totalDensity +=max(0,sampleDensity(position));
                    
                }
                float transmittance = exp(-totalDensity*_LightAbsorption);
                float3 cloudColor = lerp(_ColorBright,unity_LightColor0,saturate(transmittance*_ColorOffset1));
                cloudColor = lerp(_ColorDark,cloudColor,saturate(pow(transmittance*_ColorOffset2,3)));
                return  _DarknessThredhold+transmittance*(1-_DarknessThredhold)*cloudColor;
            }
            //光线步进
            float4 cloudRayMarching(float3 startPoint,float3 direction,float3 endPos)
            {
                float3 testPoint = startPoint;
                float sum = 1;
                direction *=_MarchStep;
                float3 lightEnergy = 0;
                //向灯光方向的散射更强一点
                float3 worldViewVector = normalize(startPoint - _WorldSpaceCameraPos);
                float cosAngle = dot(worldViewVector,_WorldSpaceLightPos0.xyz);
                float3 phaseVal = phase(cosAngle);
                //float3 lightColor = float3(0,0,0);
                for(int i = 0;i<128;i++)
                {
                    testPoint += direction;
                    float density = sampleDensity(testPoint);
                    if(density>0)
                    {
                        float3 LightTransminttance = LightMarching(testPoint);
                        lightEnergy += density * _MarchStep * sum * LightTransminttance*phaseVal;
                        //lightColor.xyz += lightEnergy*LightTransminttance;
                         sum *= exp(-density*_MarchStep*_LightAbsorption);
                    }
                   
                    if(dot((endPos-testPoint),direction)<0)break;
                    if(sum<0.01)break;
                }
                float4 lightInf = float4(lightEnergy,sum);
                //lightCol = sum;
                return lightInf;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                //o.viewVector = v.vertex - _WorldSpaceCameraPos;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f input) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, input.uv);
                //重建世界坐标
                float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture,input.uv));
                float linearDepth = Linear01Depth(depth);
                float4 worldPos = GetWorldPositionFromDepthValue(input.uv,linearDepth);
                float3 viewVector = worldPos.xyz - _WorldSpaceCameraPos;
                //计算射线是否经过AABB盒
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(viewVector);
                float2 rayBoxInfo = rayBoxDst(BoundMin,BoundMax,rayOrigin,1/rayDir);
                float dstToBox = rayBoxInfo.x;
                float dstInsideBox = rayBoxInfo.y;
                //Test
                bool rayHitBox = dstInsideBox>0;
              
                if(rayHitBox){
                //col = 0;
                }
                //Test
                //计算遮挡以及在AABB盒中的步进
                float depthEyeLinear = length(viewVector);
                float dstLimit = min (depthEyeLinear-dstToBox,dstInsideBox);
                //在AABB盒中步进
                if(dstLimit>0)
                {
                    float3 endPos = worldPos;
                    if(rayHitBox&&depthEyeLinear>dstInsideBox+dstToBox)
                    {
                        endPos = _WorldSpaceCameraPos+rayDir*(dstToBox+dstInsideBox);
                    }
                    float3 startPos = _WorldSpaceCameraPos + dstToBox*rayDir;
                    float4 cloudInf = cloudRayMarching(startPos,rayDir,endPos);
                    col.xyz = col *cloudInf.w + cloudInf.xyz;
                }
                return col;
            }
            ENDCG
        }
    }
}
