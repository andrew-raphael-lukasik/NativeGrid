/// src: https://github.com/andrew-raphael-lukasik/Unity.Mathematics.Explicit
using UnityEngine;
using Unity.Mathematics;

namespace NativeGridNamespace
{
	public struct FLOAT4
	{
		float4 Value;

		public float x => Value.x;
		public float y => Value.y;
		public float z => Value.z;
		public float w => Value.w;
		
		public static implicit operator float4 ( FLOAT4 F4 ) => F4.Value;
		public static implicit operator FLOAT4 ( float4 f4 ) => new FLOAT4{ Value=f4 };
		public static implicit operator FLOAT4 ( Vector4 v4 ) => new FLOAT4{ Value=v4 };
		public static implicit operator Vector4 ( FLOAT4 F4 ) => F4.Value;
		public static implicit operator FLOAT4 ( (float x,float y,float z,float w) tuple ) => new FLOAT4{ Value=new float4{ x=tuple.x , y=tuple.y , z=tuple.z , w=tuple.w } };

		public static float4 operator + ( FLOAT4 a , FLOAT4 b ) => a.Value + b.Value;
		public static float4 operator - ( FLOAT4 a , FLOAT4 b ) => a.Value - b.Value;
		public static float4 operator * ( FLOAT4 a , FLOAT4 b ) => a.Value * b.Value;
		public static float4 operator / ( FLOAT4 a , FLOAT4 b ) => a.Value / b.Value;
		public static float4 operator * ( FLOAT4 F4 , float f ) => F4.Value * f;

		override public string ToString () => this.Value.ToString();
	}

	public struct FLOAT3
	{
		float3 Value;

		public float x => Value.x;
		public float y => Value.y;
		public float z => Value.z;

		public float2 XZ () => new float2{ x=this.Value.x , y=this.Value.z };

		public static implicit operator float3 ( FLOAT3 F3 ) => F3.Value;
		public static implicit operator FLOAT3 ( float3 f3 ) => new FLOAT3{ Value=f3 };
		public static implicit operator FLOAT3 ( Vector3 v3 ) => new FLOAT3{ Value=v3 };
		public static implicit operator Vector3 ( FLOAT3 F3 ) => F3.Value;
		public static implicit operator FLOAT3 ( (float x,float y,float z) tuple ) => new FLOAT3{ Value=new float3{ x=tuple.x , y=tuple.y , z=tuple.z } };

		public static float3 operator + ( FLOAT3 a , FLOAT3 b ) => a.Value + b.Value;
		public static float3 operator - ( FLOAT3 a , FLOAT3 b ) => a.Value - b.Value;
		public static float3 operator * ( FLOAT3 a , FLOAT3 b ) => a.Value * b.Value;
		public static float3 operator / ( FLOAT3 a , FLOAT3 b ) => a.Value / b.Value;
		public static float3 operator * ( FLOAT3 F3 , float f ) => F3.Value * f;

		override public string ToString () => this.Value.ToString();
	}

	public struct FLOAT2
	{
		float2 Value;

		public float x => Value.x;
		public float y => Value.y;
		
		public static implicit operator float2 ( FLOAT2 F2 ) => F2.Value;
		public static implicit operator FLOAT2 ( float2 f2 ) => new FLOAT2{ Value=f2 };
		public static implicit operator FLOAT2 ( Vector2 v2 ) => new FLOAT2{ Value=v2 };
		public static implicit operator Vector2 ( FLOAT2 F2 ) => F2.Value;
		public static implicit operator FLOAT2 ( (float x,float y) tuple ) => new FLOAT2{ Value=new float2{ x=tuple.x , y=tuple.y } };
		
		public static float2 operator + ( FLOAT2 a , FLOAT2 b ) => a.Value + b.Value;
		public static float2 operator - ( FLOAT2 a , FLOAT2 b ) => a.Value - b.Value;
		public static float2 operator * ( FLOAT2 a , FLOAT2 b ) => a.Value * b.Value;
		public static float2 operator / ( FLOAT2 a , FLOAT2 b ) => a.Value / b.Value;
		public static float2 operator * ( FLOAT2 F2 , float f ) => F2.Value * f;

		override public string ToString () => this.Value.ToString();
	}

	public struct FLOAT
	{
		float Value;
		
		public static implicit operator float ( FLOAT F ) => F.Value;
		public static implicit operator FLOAT ( float f ) => new FLOAT{ Value=f };
		
		public static float operator + ( FLOAT a , FLOAT b ) => a.Value + b.Value;
		public static float operator - ( FLOAT a , FLOAT b ) => a.Value - b.Value;
		public static float operator * ( FLOAT a , FLOAT b ) => a.Value * b.Value;
		public static float operator / ( FLOAT a , FLOAT b ) => a.Value / b.Value;
		public static float operator * ( FLOAT F2 , float f ) => F2.Value * f;

		override public string ToString () => this.Value.ToString();
	}

}
