using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace NativeGridNamespace.Tests
{
	static class Enumerators
	{
		static ( int2 coord , int gridWidth , int gridheight , int2[] expected )[] _tests = new ( int2 , int , int , int2[] )[]{
			( new int2(1,1) , 0 , 0 , new int2[0] ) ,
			( new int2(-1,1) , 0 , 0 , new int2[0] ) ,
			( new int2(1,-1) , 0 , 0 , new int2[0] ) ,

			( new int2(5,5) , 10 , 10 , new int2[]{
				new int2(4,4) , new int2(5,4) , new int2(6,4) ,
				new int2(4,5) ,                 new int2(6,5) ,
				new int2(4,6) , new int2(5,6) , new int2(6,6)
			} ) ,

			( new int2(5,5) , 6 , 6 , new int2[]{
				new int2(4,4) , new int2(5,4) ,
				new int2(4,5) ,
			} ) ,

			( new int2(0,0) , 10 , 10 , new int2[]{
				                                 new int2(1,0) ,
				                 new int2(0,1) , new int2(1,1)
			} ) ,

			( new int2(3,0) , 5 , 5 , new int2[]{
				new int2(2,0) ,                 new int2(4,0) ,
				new int2(2,1) , new int2(3,1) , new int2(4,1)
			} ) ,

			( new int2(333,333) , 333 , 333 , new int2[]{
				new int2(332,332)
			} ) ,

			( new int2(-1,-1) , 333 , 33 , new int2[]{
				new int2(0,0)
			} ) ,

			( new int2(0,-1) , 333 , 33 , new int2[]{
				                 new int2(0,0) , new int2(1,0)
			} ) ,
		};
		[Test] public static void NeighbourEnumerator__multiple_tests ()
		{
			Debug.Log("test start");
			foreach( var test in _tests )
			{
				Debug.Log( $"case: coord: ( {test.coord.x} , {test.coord.y} ) , gridWidth:{test.gridWidth} , gridHeight:{test.gridheight}" );
				
				var enumerator = new NativeGrid.NeighbourEnumerator( coord:test.coord , gridWidth:test.gridWidth , gridHeight:test.gridheight );
				var results = new List<int2>( capacity:8 );
				while( enumerator.MoveNext(out int2 coord) )
					results.Add(coord);
				
				{
					var sb = new System.Text.StringBuilder("{");
					foreach( int2 coord in results )
					{
						sb.AppendFormat(" ({0},{1})",coord.x,coord.y);
						sb.Append(" ,");
					}
					if( sb[sb.Length-1]==',' ) sb.Remove(sb.Length-1,1);
					sb.Append('}');
					Debug.Log($"        results: {sb}");
				}

				Debug.Log($"    comparing number of results: {results.Count}, expected:{test.expected.Length} ...");
				Assert.AreEqual( expected:test.expected.Length , actual:results.Count );
				Debug.Log("        passed.");

				Debug.Log($"    comparing indices...");
				for( int i=0 ; i<test.expected.Length ; i++ )
				{
					int2 actual = results[i];
					int2 expected = test.expected[i];
					Debug.Log($"    expected:( {expected.x} , {expected.y} ), actual:( {actual.x} , {actual.y} ) ...");
					Assert.AreEqual( expected:expected , actual:actual );
				}
				Debug.Log("        passed.");
			}
		}

	}
}
