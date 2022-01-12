/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace NativeGridNamespace
{
	/// <summary>
	/// Abstract parent class for generic NativeGrid<T>. To simplify referencing static functions/types from "NativeGrid<byte>.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
	/// </summary>
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


		/// <summary> Enumerates neighbours in growing spiral pattern (clockwise). </summary>
		/// <note> Offsets will repeat for border cells due to clamping (make sure this breaks nothing). </note>
		/// <note> Range > 1 will produce spiral pattern </note>
		/// <returns> Enumeration of index 2d coordinates to form this spiral. </returns>
		public static void EnumerateNeighbours ( NativeList<int2> results , int width , int height , INT2 origin2 , int range = 1 )
		{
			// ..  24  25  26  27  28  29
			// 46  23   8   9  10  11  30
			// 45  22   7   0   1  12  31
			// 44  21   6   ^   2  13  32
			// 43  20   5   4   3  14  33
			// 42  19  18  17  16  15  34
			// 41  40  39  38  37  36  35

			results.Clear();
			{
				int capacity = (range*2 + 1)*(range*2 + 1) - 1;
				if( results.Capacity<capacity )
				{
					results.Capacity = capacity;
				}
			}

			int2 origin2d = origin2;
			int2 o2 = int2.zero;
			for( int ir=1 ; ir<=range ; ir++ )
			{
				//move 1 step up:
				o2 += new int2{ y=1 };
				results.Add( ClampIndex2d( origin2d+o2 , width , height ) );

				//move right:
				for( int imr=0 ; imr<(ir*2)-1 ; imr++ )
				{
					o2.x++;
					results.Add( ClampIndex2d( origin2d+o2 , width , height ) );
				}

				//move down:
				for( int imd=0 ; imd<ir*2 ; imd++ )
				{
					o2.y--;
					results.Add( ClampIndex2d( origin2d+o2 , width , height ) );
				}

				//move left:
				for( int iml=0 ; iml<ir*2 ; iml++ )
				{
					o2.x--;
					results.Add( ClampIndex2d( origin2d+o2 , width , height ) );
				}

				//move up:
				for( int imu=0 ; imu<ir*2 ; imu++ )
				{
					o2.y++;
					results.Add( ClampIndex2d( origin2d+o2 , width , height ) );
				}
			}
		}

		public static void EnumerateNeighbours ( NativeArray<int2> results , int width , int height , INT2 origin2 )
		{
			#if DEBUG
			ASSERT_TRUE( results.Length==8 , "Array length must be 8" );
			#endif
			
			int2 origin2d = origin2;
			int2 o2 = int2.zero;
			int i = 0;
			
			//move 1 step up:
			o2 += new int2{ y=1 };
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );

			//move 1 step right:
			o2.x++;
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );

			//move 2 steps down:
			o2.y--;
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );
			o2.y--;
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );

			//move 2 steps left:
			o2.x--;
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );
			o2.x--;
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );

			//move 2 steps up:
			o2.y++;
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );
			o2.y++;
			results[i++] = ClampIndex2d( origin2d+o2 , width , height );
		}


		#endregion
	}
}
