/// homepage: https://github.com/andrew-raphael-lukasik/Unity.Mathematics.Explicit

/// PROBLEM: Unity.Mathematics' types agressively implicitly cast. Using these as method arguments leads to unproductive complexity.
/// SOLUTION: Create intermediary types that implicitly cast ONLY to/from equivalents. Use them as method arguments only.

using UnityEngine;
using Unity.Mathematics;

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
	
	public static float2 operator + ( FLOAT2 a , FLOAT2 b ) => a.Value + b.Value;
	public static float2 operator - ( FLOAT2 a , FLOAT2 b ) => a.Value - b.Value;
	public static float2 operator * ( FLOAT2 a , FLOAT2 b ) => a.Value * b.Value;
	public static float2 operator / ( FLOAT2 a , FLOAT2 b ) => a.Value / b.Value;
	public static float2 operator * ( FLOAT2 F2 , float f ) => F2.Value * f;

	override public string ToString () => this.Value.ToString();
}
