/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace NativeGridNamespace
{
	/// <summary> Non-generic, abstract parent class for NativeGrid<T>. </summary>
	public abstract partial class NativeGrid
	{
		#region PUBLIC METHODS


		/// <summary> Bresenham's line drawing algorithm (https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm). </summary>
		public static void TraceLine ( INT2 A , INT2 B , NativeList<int2> results , int2 min , int2 max )
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

			int2 coord = A;
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
			
			results.Add( coord );
			
			if( dx>dy )
			{
				ai = (dy - dx) * 2;
				bi = dy * 2;
				d = bi - dx;

				while( coord.x!=B.x )
				{
					if( d>=0 )
					{
						coord.x += xi;
						coord.y += yi;
						d += ai;
					}
					else
					{
						d += bi;
						coord.x += xi;
					}

					// test for out of bounds:
					if( math.any(new bool4{ x=coord.x<min.x , y=coord.y<min.y , z=coord.x>max.x , w=coord.y>max.y }) ) return;
					
					results.Add( coord );
				}
			}
			else
			{
				ai = ( dx - dy ) * 2;
				bi = dx * 2;
				d = bi - dy;
				
				while( coord.y!=B.y )
				{
					if( d>=0 )
					{
						coord.x += xi;
						coord.y += yi;
						d += ai;
					}
					else
					{
						d += bi;
						coord.y += yi;
					}
					
					// test for out of bounds:
					if( math.any(new bool4{ x=coord.x<min.x , y=coord.y<min.y , z=coord.x>max.x , w=coord.y>max.y }) ) return;

					results.Add( coord );
				}
			}
		}


		#endregion
	}
}
