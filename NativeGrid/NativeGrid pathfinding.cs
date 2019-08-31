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

    
    /// <summary> An Astar solving job </summary>
    /// Format weights to 0.0 to 1.0 range. Heuristic can cease to work otherwise.
    /// Elasticity range is 0.0 to 1.0. Where 0.0 produces straight lines and positive values produces less costly, winding paths.
    [Unity.Burst.BurstCompile]
    public struct AStarJob : IJob, System.IDisposable
    {
        INT2 start;
        INT2 destination;
        [ReadOnly] NativeArray<float> moveCost;
        int moveCost_width;
        NativeList<int2> path;
        float heuristic_multiplier;

        public NativeArray<float> _G_;
        public NativeArray<int2> solution;
        NativeMinHeap<int2,MyComparer> frontier;
        public NativeHashMap<int2,byte> visited;
		public NativeList<int2> neighbours;

        public unsafe AStarJob
        (
            INT2 start ,
            INT2 destination ,
            NativeArray<float> moveCost ,
            int moveCost_width ,
            NativeList<int2> output_path ,
            float heuristic_multiplier
        )
        {
            this.start = start;
            this.destination = destination;
            this.moveCost = moveCost;
            this.moveCost_width = moveCost_width;
            this.path = output_path;
            this.heuristic_multiplier = heuristic_multiplier;

            int length = moveCost.Length;
            int start1d = BurstSafe.Index2dTo1d( start , moveCost_width );
            _G_ = new NativeArray<float>( length , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
			solution = new NativeArray<int2>( length , Allocator.TempJob );
            frontier = new NativeMinHeap<int2,MyComparer>(
                new MyComparer( _G_ , moveCost_width , destination , heuristic_multiplier ) ,
                Allocator.TempJob ,
				length
            );
			visited = new NativeHashMap<int2,byte>( length , Allocator.TempJob );//TODO: use actual hashSet once available
            neighbours = new NativeList<int2>( 8 , Allocator.TempJob );
        }
        public void Execute ()
        {
            int length = moveCost.Length;
			int start1d = BurstSafe.Index2dTo1d( start , moveCost_width );
            {
                for( int i=_G_.Length-1 ; i!=-1 ; i-- ) _G_[i] = float.MaxValue;
                _G_[start1d] = 0;
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
            int2 parent = int2.zero-1;
            while( frontier.Count!=0 && math.any(parent!=destination) )
            {
                parent = frontier.Pop();//we grab candidate with lowest F so far
                int parent1d = BurstSafe.Index2dTo1d( parent , moveCost_width );
                float parent_g = _G_[parent1d];

                //lest check all his neighbours:
				EnumerateNeighbours( neighbours , moveCost_width , moveCost_width , parent );
				int neighboursLength = neighbours.Length;
                for( int i=0 ; i<neighboursLength ; i++ )
                {
					int2 neighbour = neighbours[i];
                    int neighbour1d = BurstSafe.Index2dTo1d( neighbour , moveCost_width );

                    float g = parent_g + moveCost[neighbour1d];
                    
                    //update G when this connection is less costly:
                    if(
                        g<_G_[neighbour1d]
                    )
                    {
                        _G_[neighbour1d] = g;
                        solution[neighbour1d] = parent;
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
            _G_.Dispose();
            solution.Dispose();
            frontier.Dispose();
            visited.Dispose();
			neighbours.Dispose();
        }
        unsafe struct MyComparer : IComparerInt2
        {
			void* _G_Ptr;
			int _width;
			int2 _dest;
            float heuristic_multiplier;
			public MyComparer ( NativeArray<float> _G_ , int weightsWidth , INT2 destination , float heuristic_multiplier )
            {
                this._G_Ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr( _G_ );
				this._width = weightsWidth;
				this._dest = destination;
                this.heuristic_multiplier = heuristic_multiplier;
            }
            int IComparerInt2.Compare ( int2 lhs , int2 rhs )
            {
				int lhs1d = BurstSafe.Index2dTo1d( lhs , _width );
				int rhs1d = BurstSafe.Index2dTo1d( rhs , _width );
				
                float euclideanHeuristicMaxLength = EuclideanHeuristicMaxLength(_width*_width,_width);
                
				float lhs_g = UnsafeUtility.ReadArrayElement<float>( _G_Ptr , lhs1d ) + (EuclideanHeuristic( lhs , _dest )/euclideanHeuristicMaxLength)*heuristic_multiplier;
				float rhs_g = UnsafeUtility.ReadArrayElement<float>( _G_Ptr , rhs1d ) + (EuclideanHeuristic( rhs , _dest )/euclideanHeuristicMaxLength)*heuristic_multiplier;

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
