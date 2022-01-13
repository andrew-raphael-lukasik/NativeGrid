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
			readonly int2 _index2D;
			int2 _current;
			byte _tick;
			int _xMax, _yMax;

			public NeighbourEnumerator ( int2 index2D , int gridWidth , int gridHeight )
			{
				this._index2D = index2D;
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
					case 0:	candidate = _index2D + new int2{ x=-1 , y=-1 };	break;// y-1
					case 1:	candidate = _index2D + new int2{        y=-1 };	break;
					case 2:	candidate = _index2D + new int2{ x=+1 , y=-1 };	break;
					case 3:	candidate = _index2D + new int2{ x=-1        };	break;// y
					case 4:	candidate = _index2D + new int2{ x=+1        };	break;
					case 5:	candidate = _index2D + new int2{ x=-1 , y=+1 };	break;// y+1
					case 6:	candidate = _index2D + new int2{        y=+1 };	break;
					case 7:	candidate = _index2D + new int2{ x=+1 , y=+1 };	break;
					default: return false;
				}
				bool4 isOutOfBounds = new bool4{ x=candidate.x<0 , y=candidate.y<0 , z=candidate.x>_xMax , w=candidate.y>_yMax };
				if( math.any(isOutOfBounds) ) return MoveNext();
				_current = candidate;
				return true;
			}

			public bool MoveNext ( out int2 neighbourIndex2D )
			{
				bool success = MoveNext();
				neighbourIndex2D = _current;
				return success;
			}

			public void Reset ()
			{
				_current = _index2D;
				_tick = 0;
			}

		}
		
	}
}
