/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid
using Unity.Mathematics;

namespace NativeGridNamespace
{
	/// <summary> Non-generic, abstract parent class for NativeGrid<T>. </summary>
	public abstract partial class NativeGrid
	{
		
		public interface INativeEnumerator<T> where T : unmanaged
		{
			T Current { get; }
			bool MoveNext();
			bool MoveNext( out T current );
			void Reset();
		}

		public struct NeighbourEnumerator : INativeEnumerator<int2>
		{
			readonly int2 _coord;
			readonly int _xMax, _yMax;
			int2 _current;
			byte _tick;

			public NeighbourEnumerator ( int2 coord , int gridWidth , int gridHeight )
			{
				this._coord = coord;
				this._xMax = gridWidth-1;
				this._yMax = gridHeight-1;
				this._current = new int2(-1,-1);
				this._tick = 0;
			}

			public int2 Current => _current;

			public bool MoveNext ()
			{
				if( _tick>7 ) return false;
				int2 candidate = _current;
				switch( _tick++ )
				{
					case 0:	candidate = _coord + new int2{ x=-1 , y=-1 };	break;// y-1
					case 1:	candidate = _coord + new int2{        y=-1 };	break;
					case 2:	candidate = _coord + new int2{ x=+1 , y=-1 };	break;
					case 3:	candidate = _coord + new int2{ x=-1        };	break;// y
					case 4:	candidate = _coord + new int2{ x=+1        };	break;
					case 5:	candidate = _coord + new int2{ x=-1 , y=+1 };	break;// y+1
					case 6:	candidate = _coord + new int2{        y=+1 };	break;
					case 7:	candidate = _coord + new int2{ x=+1 , y=+1 };	break;
					default: return false;
				}
				bool4 isOutOfBounds = new bool4{ x=candidate.x<0 , y=candidate.y<0 , z=candidate.x>_xMax , w=candidate.y>_yMax };
				if( math.any(isOutOfBounds) ) return MoveNext();
				_current = candidate;
				return true;
			}

			public bool MoveNext ( out int2 neighbourCoord )
			{
				bool success = MoveNext();
				neighbourCoord = _current;
				return success;
			}

			public void Reset ()
			{
				_current = _coord;
				_tick = 0;
			}

		}

		public struct LineTraceEnumerator : INativeEnumerator<int2>
		{
			readonly int2 _src, _dst;
			int2 _current;

			public LineTraceEnumerator ( INT2 src , INT2 dst )
			{
				this._src = src;
				this._dst = dst;
				this._current = src;
			}
			// public LineTraceEnumerator ( INT2 src , INT2 dst , INT2 min , INT2 max )
			// 	: this( src:src , dst:dst )
			// {
			// 	if(  )
			// 	{

			// 	}
			// }

			public int2 Current => _current;

			public bool MoveNext ()
			{
				int d, dx, dy, ai, bi, xi, yi;

				if( _current.x< _dst.x )
				{
					xi = 1;
					dx = _dst.x - _current.x;
				}
				else
				{
					xi = -1;
					dx = _current.x - _dst.x;
				}
				
				if( _current.y< _dst.y )
				{
					yi = 1;
					dy = _dst.y - _current.y;
				}
				else
				{
					yi = -1;
					dy = _current.y - _dst.y;
				}
				
				if( dx>dy )
				{
					ai = (dy - dx) * 2;
					bi = dy * 2;
					d = bi - dx;

					while( _current.x!=_dst.x )
					{
						if( d>=0 )
						{
							_current.x += xi;
							_current.y += yi;
							d += ai;
						}
						else
						{
							d += bi;
							_current.x += xi;
						}
						return true;
					}
				}
				else
				{
					ai = ( dx - dy ) * 2;
					bi = dx * 2;
					d = bi - dy;
					
					while( _current.y!=_dst.y )
					{
						if( d>=0 )
						{
							_current.x += xi;
							_current.y += yi;
							d += ai;
						}
						else
						{
							d += bi;
							_current.y += yi;
						}
						return true;
					}
				}
				return false;
			}

			public bool MoveNext ( out int2 next )
			{
				bool success = MoveNext();
				next = _current;
				return success;
			}

			public void Reset ()
			{
				_current = _src;
			}

		}
		
	}
}
