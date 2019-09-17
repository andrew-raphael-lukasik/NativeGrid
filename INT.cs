/// homepage: https://github.com/andrew-raphael-lukasik/Unity.Mathematics.Explicit

/// PROBLEM: Unity.Mathematics' types agressively implicitly cast. Using these as method arguments leads to unproductive complexity.
/// SOLUTION: Create intermediary types that implicitly cast ONLY to/from equivalents. Use them as method arguments only.

using UnityEngine;
using Unity.Mathematics;

public struct INT4
{
	int4 Value;

	public int x => Value.x;
	public int y => Value.y;
	public int z => Value.z;
	public int w => Value.w;

	public static implicit operator int4 ( INT4 I4 ) => I4.Value;
	public static implicit operator INT4 ( int4 i4 ) => new INT4{ Value=i4 };

	public static int4 operator + ( INT4 a , INT4 b ) => a.Value + b.Value;
	public static int4 operator - ( INT4 a , INT4 b ) => a.Value - b.Value;
	public static int4 operator * ( INT4 a , INT4 b ) => a.Value * b.Value;
	public static int4 operator / ( INT4 a , INT4 b ) => a.Value / b.Value;
	public static int4 operator * ( INT4 F3 , int f ) => F3.Value * f;

	override public string ToString () => this.Value.ToString();
}

public struct INT3
{
	int3 Value;

	public int x => Value.x;
	public int y => Value.y;
	public int z => Value.z;

	public int2 XZ () => new int2{ x=this.Value.x , y=this.Value.z };

	public static implicit operator int3 ( INT3 I3 ) => I3.Value;
	public static implicit operator INT3 ( int3 i3 ) => new INT3{ Value=i3 };
	public static implicit operator INT3 ( Vector3Int v3 ) => new INT3{ Value=new int3{ x=v3.x , y=v3.y , z=v3.z } };
	public static implicit operator Vector3Int ( INT3 I3 ) => new Vector3Int{ x=I3.Value.x , y=I3.Value.y , z=I3.Value.z };

	public static int3 operator + ( INT3 a , INT3 b ) => a.Value + b.Value;
	public static int3 operator - ( INT3 a , INT3 b ) => a.Value - b.Value;
	public static int3 operator * ( INT3 a , INT3 b ) => a.Value * b.Value;
	public static int3 operator / ( INT3 a , INT3 b ) => a.Value / b.Value;
	public static int3 operator * ( INT3 F3 , int f ) => F3.Value * f;

	override public string ToString () => this.Value.ToString();
}

public struct INT2
{
	int2 Value;

	public int x => Value.x;
	public int y => Value.y;

	public static implicit operator int2 ( INT2 I2 ) => I2.Value;
	public static implicit operator INT2 ( int2 i2 ) => new INT2{ Value=i2 };
	public static implicit operator INT2 ( Vector2Int v2 ) => new INT2{ Value=new int2{ x=v2.x , y=v2.y } };
	public static implicit operator Vector2Int ( INT2 I2 ) => new Vector2Int{ x=I2.Value.x , y=I2.Value.y };
	
	public static int2 operator + ( INT2 a , INT2 b ) => a.Value + b.Value;
	public static int2 operator - ( INT2 a , INT2 b ) => a.Value - b.Value;
	public static int2 operator * ( INT2 a , INT2 b ) => a.Value * b.Value;
	public static int2 operator / ( INT2 a , INT2 b ) => a.Value / b.Value;
	public static int2 operator * ( INT2 F2 , int f ) => F2.Value * f;

	override public string ToString () => this.Value.ToString();
}
