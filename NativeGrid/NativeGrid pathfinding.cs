/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

using IComparerInt2 = System.Collections.Generic.IComparer<Unity.Mathematics.int2>;


/// <summary>
/// Abstract parent class for generic NativeGrid{STRUCT}. To simplify referencing static functions/types from "NativeGrid{byte}.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
/// </summary>
public abstract partial class NativeGrid
{
    #region PUBLIC METHODS


    /// <summary> Traces path using some kind of A* algorithm </summary>
    /// Format weights to 0.0 to 1.0 range. Heuristic can cease to work otherwise.
    [Unity.Burst.BurstCompile]
    public struct AStarJob : IJob, System.IDisposable
    {

        INT2 start;
        INT2 destination;
        [ReadOnly] NativeArray<float> moveCost;
        int moveCost_width;
        NativeList<int2> path;
        float heuristic_cost;
        float heuristic_search;

        public NativeArray<float> _F_;
        public NativeArray<int2> solution;
        NativeMinHeap<int2,MyComparer> frontier;
        public NativeHashMap<int2,byte> visited;
		public NativeList<int2> neighbours;

        /// <summary> Traces path using some kind of A* algorithm </summary>
        /// <param name="start"> Start index 2d </param>
        /// <param name="destination"> Destination index 2d </param>
        /// <param name="moveCost"> Move cost data 2d array </param>
        /// <param name="moveCost_width"> 2d array's width </param>
        /// <param name="heuristic_cost"> Cost heuristic multiplier. Figure out yourself what value works best for your specific moveCost data </param>
        /// <param name="heuristic_search"> Search heuristic multiplier. Figure out yourself what value works best for your specific moveCost data </param>
        /// <param name="output_path"> Resulting path goes here </param>
        public unsafe AStarJob
        (
            INT2 start ,
            INT2 destination ,
            NativeArray<float> moveCost ,
            int moveCost_width ,
            float heuristic_cost ,
            float heuristic_search ,
            NativeList<int2> output_path
        )
        {
            this.start = start;
            this.destination = destination;
            this.moveCost = moveCost;
            this.moveCost_width = moveCost_width;
            this.path = output_path;
            this.heuristic_cost = heuristic_cost;
            this.heuristic_search = heuristic_search;

            int length = moveCost.Length;
            int start1d = BurstSafe.Index2dTo1d( start , moveCost_width );
            _F_ = new NativeArray<float>( length , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
			solution = new NativeArray<int2>( length , Allocator.TempJob );
            frontier = new NativeMinHeap<int2,MyComparer>(
                new MyComparer( _F_ , moveCost_width , destination , heuristic_search ) ,
                Allocator.TempJob , length
            );
			visited = new NativeHashMap<int2,byte>( length , Allocator.TempJob );//TODO: use actual hashSet once available
            neighbours = new NativeList<int2>( 8 , Allocator.TempJob );
        }
        public void Execute ()
        {
            int length = moveCost.Length;
			int start1d = BurstSafe.Index2dTo1d( start , moveCost_width );
            {
                for( int i=_F_.Length-1 ; i!=-1 ; i-- ) _F_[i] = float.MaxValue;
                _F_[start1d] = 0;
            }
			{
				solution[start1d] = start;
			}
			{
				frontier.Push( start );
			}
			{
                visited.TryAdd( start , 0 );
            }
            
            //solve;
            float euclideanMaxLength = EuclideanHeuristicMaxLength( length , moveCost_width );
            int2 node = int2.zero-1;
            while( frontier.Count!=0 && math.any(node!=destination) )
            {
                node = frontier.Pop();//we grab candidate with lowest F so far
                int node1d = BurstSafe.Index2dTo1d( node , moveCost_width );
                float node_f = _F_[node1d];

                //lets check all its neighbours:
				EnumerateNeighbours( neighbours , moveCost_width , moveCost_width , node );
				int neighboursLength = neighbours.Length;
                for( int i=0 ; i<neighboursLength ; i++ )
                {
					int2 neighbour = neighbours[i];
                    int neighbour1d = BurstSafe.Index2dTo1d( neighbour , moveCost_width );
                    
                    bool isOrthogonal = math.any( node==neighbour );//is relative position orthogonal or diagonal

                    float g = node_f + moveCost[neighbour1d]*( isOrthogonal ? 1f : 1.41421356237f );
                    float h = EuclideanHeuristicNormalized( neighbour , destination , euclideanMaxLength )*heuristic_cost;
                    float f = g + h;
                    
                    //update F when this connection is less costly:
                    if(
                        f<_F_[neighbour1d]
                    )
                    {
                        _F_[neighbour1d] = f;
                        solution[neighbour1d] = node;
                    }

                    //update frontier:
                    if( visited.TryAdd(neighbour,0) )
                    {
                        frontier.Push(neighbour);
                    }
                }
            }

            //create path:
            BacktrackToPath( solution , moveCost_width , destination , path );
        }
        public void Dispose ()
        {
            _F_.Dispose();
            solution.Dispose();
            frontier.Dispose();
            visited.Dispose();
			neighbours.Dispose();
        }
        unsafe struct MyComparer : IComparerInt2
        {
			void* _F_Ptr;
			int _width;
			int2 _dest;
            float heuristic_search;
			public MyComparer ( NativeArray<float> _G_ , int weightsWidth , INT2 destination , float heuristic_search )
            {
                this._F_Ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr( _G_ );
				this._width = weightsWidth;
				this._dest = destination;
                this.heuristic_search = heuristic_search;
            }
            int IComparerInt2.Compare ( int2 lhs , int2 rhs )
            {
				int lhs1d = BurstSafe.Index2dTo1d( lhs , _width );
				int rhs1d = BurstSafe.Index2dTo1d( rhs , _width );
				
                float euclideanHeuristicMaxLength = EuclideanHeuristicMaxLength(_width*_width,_width);
                
				float lhs_g = UnsafeUtility.ReadArrayElement<float>( _F_Ptr , lhs1d ) + EuclideanHeuristicNormalized(lhs,_dest,euclideanHeuristicMaxLength)*heuristic_search;
				float rhs_g = UnsafeUtility.ReadArrayElement<float>( _F_Ptr , rhs1d ) + EuclideanHeuristicNormalized(rhs,_dest,euclideanHeuristicMaxLength)*heuristic_search;

				return lhs_g.CompareTo(rhs_g);
            }
        }
    }
    

    static float EuclideanHeuristic ( INT2 a , INT2 b ) => math.length( a-b );
    static float EuclideanHeuristicNormalized ( INT2 a , INT2 b , float maxLength ) => math.length( a-b ) / maxLength;
    static float EuclideanHeuristicMaxLength ( int arrayLength , int arrayWidth ) => EuclideanHeuristic( int2.zero , new int2{ x=arrayWidth-1 , y=arrayLength/arrayWidth-1 } );

    static void BacktrackToPath ( NativeArray<int2> solution , int solutionWidth , INT2 destination , NativeList<int2> path )
    {
        path.Clear();
        if( path.Capacity<solutionWidth*2 ) path.Capacity = solutionWidth*2;//preallocate for reasonably common/bad scenario
        int solutionLength = solution.Length;

        int2 pos = destination;
        int pos1d = BurstSafe.Index2dTo1d( pos , solutionWidth );
        int step = 0;
        while(
            math.any( pos!=solution[pos1d] )
            && step++<solutionLength
        )
        {
            path.Add( pos );
            pos = solution[pos1d];
            pos1d = BurstSafe.Index2dTo1d( pos , solutionWidth );
        }

        #if DEBUG
        if( step>=solutionLength && solutionLength!=0 ) throw new System.Exception($"{pos} == {solution[pos1d]}, {math.all( pos!=solution[pos1d] )}\tINFINITE LOOP AVERTED");
        #endif

        // TODO: can this step be avoided?
        // path.Reverse();
        {
            int pathLength = path.Length;
            int pathLengthHalf = pathLength / 2;
            int lastIndex = pathLength-1;
            for( int i=0 ; i<pathLengthHalf ; i++ )
            {
                var tmp = path[i];
                path[i] = path[lastIndex-i];
                path[lastIndex-i] = tmp;
            }
        }
    }


    #endregion
}
