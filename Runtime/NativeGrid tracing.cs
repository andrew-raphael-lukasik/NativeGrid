/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace NativeGridNamespace
{
	/// <summary> Non-generic, abstract parent class for NativeGrid<T>. </summary>
	public abstract partial class NativeGrid
	{
		#region PUBLIC METHODS


		/// <summary> Bresenham's line drawing algorithm (https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm). </summary>
		public static void TraceLine ( NativeList<int2> results , INT2 A , INT2 B )
		{
			results.Clear();
			{
				int2 dir = math.abs( A - B );
				int capacity = math.max( dir.x , dir.y );
				if( results.Capacity<capacity )
				{
					results.Capacity = capacity;
				}
			}

			int2 pos = A;
			int d, dx, dy, ai, bi, xi, yi;

			if( A.x<B.x )
			{
				xi = 1;
				dx = B.x - A.x;
			}
			else
			{
				xi = -1;
				dx = A.x - B.x;
			}
			
			if( A.y<B.y )
			{
				yi = 1;
				dy = B.y - A.y;
			}
			else
			{
				yi = -1;
				dy = A.y - B.y;
			}
			
			results.Add( pos );
			
			if( dx>dy )
			{
				ai = (dy - dx) * 2;
				bi = dy * 2;
				d = bi - dx;

				while( pos.x!=B.x )
				{
					if( d>=0 )
					{
						pos.x += xi;
						pos.y += yi;
						d += ai;
					}
					else
					{
						d += bi;
						pos.x += xi;
					}
					
					results.Add( pos );
				}
			}
			else
			{
				ai = ( dx - dy ) * 2;
				bi = dx * 2;
				d = bi - dy;
				
				while( pos.y!=B.y )
				{
					if( d>=0 )
					{
						pos.x += xi;
						pos.y += yi;
						d += ai;
					}
					else
					{
						d += bi;
						pos.y += yi;
					}
					
					results.Add( pos );
				}
			}
		}

		public struct TraceLineJob<T> : IJob
			where T : unmanaged
		{
			public NativeArray<T> Array;
			public int2 Start, End;
			void IJob.Execute ()
			{
				var indices = new NativeList<int2>( (int)( math.distance(Start,End) * math.SQRT2 ) , Allocator.Temp );
				TraceLine( results:indices , A:Start , B:End );
				foreach( int2 coord in indices )
				{
					
				}
			}
		}


		#endregion
	}
}
