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
        [ReadOnly] NativeArray<float> weights;
        int weightsWidth;
        NativeList<int2> path;
        float elasticity;

        NativeArray<float> costs;
        NativeArray<int2> solution;
        NativeMinHeap<int2,MyComparer> frontier;
        NativeHashMap<int2,byte> visited;
		NativeList<int2> neighbours;

        public unsafe AStarJob
        (
            INT2 start ,
            INT2 destination ,
            NativeArray<float> weights ,
            int weightsWidth ,
            NativeList<int2> path ,
            float elasticity = 1f
        )
        {
            this.start = start;
            this.destination = destination;
            this.weights = weights;
            this.weightsWidth = weightsWidth;
            this.path = path;
            this.elasticity = elasticity;

            int numWeights = weights.Length;
            int start1d = BurstSafe.Index2dTo1d( start , weightsWidth );
            costs = new NativeArray<float>( numWeights , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
			solution = new NativeArray<int2>( numWeights , Allocator.TempJob );
            frontier = new NativeMinHeap<int2,MyComparer>(
                new MyComparer( costs , weightsWidth , destination ) ,
                Allocator.TempJob ,
				numWeights
            );
			visited = new NativeHashMap<int2,byte>( numWeights , Allocator.TempJob );//TODO: use actual hashSet once available
            neighbours = new NativeList<int2>( 100 , Allocator.TempJob );
        }
        public void Execute ()
        {
            int numWeights = weights.Length;
			int start1d = BurstSafe.Index2dTo1d( start , weightsWidth );
            {
                for( int i=costs.Length-1 ; i!=-1 ; i-- ) costs[i] = float.MaxValue;
                costs[start1d] = 0;
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
            float euclideanMaxLength = EuclideanHeuristicMaxLength( numWeights , weightsWidth );
            int2 current = int2.zero-1;
            while( frontier.Count!=0 && math.any(current!=destination) )
            {
                current = frontier.Pop();
                int current1d = BurstSafe.Index2dTo1d( current , weightsWidth );

				EnumerateNeighbours( neighbours , weightsWidth , weightsWidth , current );
				int neighboursLength = neighbours.Length;
                for( int i=0 ; i<neighboursLength ; i++ )
                {
					int2 neighbour = neighbours[i];
                    int neighbour1d = BurstSafe.Index2dTo1d( neighbour , weightsWidth );
                    float newNeighbourCost =
                        costs[current1d]
                        + math.pow( 1f-weights[neighbour1d] , -5f*elasticity ) * 100f
                        + EuclideanHeuristicNormalized( current , destination , euclideanMaxLength );
                    if(
                        newNeighbourCost<costs[neighbour1d]
                        && visited.TryAdd(neighbour,0)//makes sure it wasn't visited already (infinite loops otherwise)
                    )
                    {
                        costs[neighbour1d] = newNeighbourCost;
                        solution[neighbour1d] = current;
                        frontier.Push(neighbour);
                    }
                }
            }

            //create path:
            BacktrackToPath( solution , weightsWidth , destination , path );
        }
        public void Dispose ()
        {
            costs.Dispose();
            solution.Dispose();
            frontier.Dispose();
            visited.Dispose();
			neighbours.Dispose();
        }
        unsafe struct MyComparer : IComparerInt2
        {
			void* _ptr;
			int _width;
			int2 _dest;
			public MyComparer ( NativeArray<float> costs , int weightsWidth , INT2 destination )
            {
                this._ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr( costs );
				this._width = weightsWidth;
				this._dest = destination;
            }
            int IComparerInt2.Compare ( int2 lhs , int2 rhs )
            {
				int lhs1d = BurstSafe.Index2dTo1d( lhs , _width );
				int rhs1d = BurstSafe.Index2dTo1d( rhs , _width );
				
				float lhsCost = UnsafeUtility.ReadArrayElement<float>( _ptr , lhs1d ) + EuclideanHeuristic( lhs , _dest );
				float rhsCost = UnsafeUtility.ReadArrayElement<float>( _ptr , rhs1d ) + EuclideanHeuristic( rhs , _dest );
				
				return lhsCost.CompareTo(rhsCost);
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
