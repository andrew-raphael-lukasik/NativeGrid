/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


//TODO: jobify entire thing


/// <summary>
/// NativeGrid<STRUCT> is grid data layout class. Parent NativeGrid class is for static functions and nested types.
/// </summary>
public class NativeGrid <STRUCT>
    : NativeGrid,System.IDisposable
    where STRUCT : unmanaged
{
    #region FIELDS & PROPERTIES


    [SerializeField] protected NativeArray<STRUCT> _values;
    /// <summary> Internal 1d data array </summary>
    public NativeArray<STRUCT> values => _values;

    public readonly int width;
    public readonly int height;
    public readonly int length;
    public bool IsCreated => _values.IsCreated;
    public JobHandle writeAccess = default(JobHandle);


    #endregion
    #region CONSTRUCTORS


    public NativeGrid ( int width , int height , Allocator allocator )
    {
        this._values = new NativeArray<STRUCT>( width * height , allocator );
        this.width = width;
        this.height = height;
        this.length = width * height;
    }
    public NativeGrid ( int width , int height , NativeArray<STRUCT> nativeArray )
    {
        this._values = nativeArray;
        this.width = width;
        this.height = height;
        this.length = width * height;
    }


    #endregion
    #region OPERATORS


    public STRUCT this [ int i ]
    {
        get { writeAccess.Complete(); return _values[i]; }
        set { writeAccess.Complete(); _values[i] = value; }
    }

    public STRUCT this [ int x , int y ]
    {
        get { writeAccess.Complete(); return _values[Index2dTo1d(x,y)]; }
        set { writeAccess.Complete(); _values[Index2dTo1d(x,y)] = value; }
    }


    #endregion
    #region PRIVATE METHODS



    #endregion
    #region PUBLIC METHODS


    /// <summary> Executes for each grid cell </summary>
    public void ForEach ( IAction<STRUCT> action )
    {
        int arrayLength = this.length;
        for( int i=0 ; i < arrayLength ; i++ )
        {
            action.Execute( this._values[i] );
        }
    }
    public void ForEach ( IPredicate<STRUCT> predicate , IAction<STRUCT> action )
    {
        int arrayLength = this.length;
        for( int i=0 ; i < arrayLength ; i++ )
        {
            STRUCT cell = this._values[i];
            if( predicate.Execute( cell )==true )
            {
                action.Execute( cell );
            }
        }
    }
    public JobHandle ForEach ( IFunc<STRUCT> func , JobHandle dependency = default(JobHandle) )
    {
        var job = new ForEachFuncJob<STRUCT>( _values , func );
        return writeAccess = job.Schedule(
            _values.Length , 1024 ,
            JobHandle.CombineDependencies( dependency , writeAccess )
        );
    }
    /// <param name="action"> argument is grid index1d </param>
    public JobHandle ForEach ( IAction<int> action , JobHandle dependency = default(JobHandle) )
    {
        var job = new ForEachIndexActionJob<STRUCT>( _values , action );
        return job.Schedule(
            _values.Length , 1024 ,
            JobHandle.CombineDependencies( writeAccess , dependency )
        );
    }
    /// <param name="action"> arguments are grid index2d </param>
    public JobHandle ForEach ( IAction<int,int> action , JobHandle dependency = default(JobHandle) )
    {
        var job = new ForEachIndex2dActionJob<STRUCT>( _values , width , action );
        return job.Schedule(
            _values.Length , 1024 ,
            JobHandle.CombineDependencies( writeAccess , dependency )
        );
    }

    /// <summary> For each in rectangle </summary>
    /// <param name="action">'s 2 parameters are grid X and Y 2d indexes </param>
    public void ForEach ( int x , int y , int w , int h , IAction<int,int> action , IAction<int,int> onRectIsOutOfBounds = null )
    {
        int yStart = y;
        int xEnd = x + w;
        int yEnd = y + h;
        if( onRectIsOutOfBounds!=null && ( xEnd>width || yEnd>height ) )
        {
            onRectIsOutOfBounds.Execute(x,y);
        }
        else
        {
            for( ; x<xEnd ; x++ )
            {
                for( ; y<yEnd ; y++ ) action.Execute(x,y);
                y = yStart;
            }
        }
    }
    //TODO: make this work with NativeGrid or remove:
    // public void ForEach ( int x , int y , int w , int h , IFunc<T,T> func )
    // {
    //     ForEach(
    //         x , y , w , h ,
    //         (ax,ay) => (this)[ ax , ay ] = func.Execute( ( this )[ ax , ay ] )
    //     );
    // }
    // /// <param name="action"> parameter provides 1d index </param>
    // public void ForEach ( int x , int y , int w , int h , IAction<int> action )
    // {
    //     ForEach(
    //         x , y , w , h ,
    //         (ax,ay) => action.Execute( Index2dTo1d( ax , ay ) )
    //     );
    // }

    /// <summary> Converts index 2d to 1d equivalent </summary>
    public int Index2dTo1d ( int x , int y )
    {
        #if DEBUG
        if( IsIndex2dValid(x,y)==false ) { Debug.LogWarningFormat( "[{0},{1}] index is invalid for this grid" , x , y ); }
        #endif
        return y * width + x;
    }
    public int Index2dTo1d ( int2 index2d ) => Index2dTo1d( index2d.x , index2d.y );

    
    /// <summary> Converts 1d to 2d array index </summary>
    public int2 Index1dTo2d ( int i ) => new int2 { x=i%width , y=i/width };

    
    /// <summary> Transforms local position to cell index </summary>
    public bool LocalPointToIndex2d ( float3 localPoint , float spacing , out int2 result )
    {
        int x = (int)( ( localPoint.x+(float)width*0.5f*spacing )/spacing );
        int z = (int)( ( localPoint.z+(float)height*0.5f*spacing )/spacing );
        if( IsIndex2dValid(x,z) )
        {
            result = new int2{ x=x , y=z };
            return true;
        } else {
            result = new int2{ x=-1 , y=-1 };
            return false;
        }
    }


    /// <summary> Determines whether index 2d is inside array bounds </summary>
    public bool IsIndex2dValid ( int x , int y ) => IsIndex2dValid( x , y , width , height );


    /// <summary> Determines whether index 1d is inside array bounds </summary>
    public bool IsIndex1dValid ( int i ) => IsIndex1dValid( i , this.length );


    /// <summary> Transforms index to local position. </summary>
    public float3 IndexToLocalPoint ( int x , int y , float spacing )
    {
        return new float3(
            ( (float)x*spacing )+( -width*spacing*0.5f )+( spacing*0.5f ) ,
            0f ,
            ( (float)y*spacing )+( -height*spacing*0.5f )+( spacing*0.5f )
        );
    }
    public float3 IndexToLocalPoint ( int index1d , float spacing )
    {
        int2 index2d = Index1dTo2d( index1d );
        return new float3(
            ( index2d.x*spacing )+( -width*spacing*0.5f )+( spacing*0.5f ) ,
            0f ,
            ( index2d.y*spacing )+( -height*spacing*0.5f )+( spacing*0.5f )
        );
    }


    /// <returns> Rect center position </returns>
    public float3 IndexToLocalPoint ( int x , int y , int w , int h , float spacing )
    {
        float3 cornerA = IndexToLocalPoint( x , y , spacing );
        float3 cornerB = IndexToLocalPoint( x+w-1 , y+h-1 , spacing );
        return cornerA+(cornerB-cornerA)*0.5f;
    }


    /// <summary> Gets the surrounding type count. </summary>
    public int GetSurroundingTypeCount ( int x , int y , IPredicate<STRUCT> predicate )
    {
        int result = 0;
        for( int neighbourX=x-1 ; neighbourX<=x+1 ; neighbourX++ )
        for( int neighbourY=y-1 ; neighbourY<=y+1 ; neighbourY++ )
        {
            if( neighbourX>=0 && neighbourX<this.width && neighbourY>=0 && neighbourY<this.height )
            {
                if( neighbourX!=x || neighbourY!=y )
                {
                    result += predicate.Execute((this)[neighbourX,neighbourY]) ? 1 : 0;
                }
            }
            else { result++; }
        }
        return result;
    }


    /// <summary> Gets the surrounding field values </summary>
    /// <returns>
    /// 8-bit clockwise-enumerated bit values 
    /// 7 0 1           [x-1,y+1]  [x,y+1]  [x+1,y+1]
    /// 6   2     ==    [x-1,y]     [x,y]     [x+1,y]
    /// 5 4 3           [x-1,y-1]  [x,y-1]  [x+1,y-1]
    /// for example: 1<<0 is top, 1<<1 is top-right, 1<<2 is right, 1<<6|1<<4|1<<2 is both left,down and right
    /// </returns>
    public byte GetMarchingSquares ( int x , int y , IPredicate<STRUCT> predicate )
    {
        const byte zero = 0b_0000_0000;
        byte result = zero;

        //out of bounds test:
        bool xPlus = x+1 < width;
        bool yPlus = y+1 < height;
        bool xMinus = x-1 >= 0;
        bool yMinus = y-1 >= 0;

        //top, down:
        result |= yPlus && predicate.Execute( this[ x , y+1 ] ) ? (byte)0b_0000_0001 : zero;
        result |= yMinus && predicate.Execute( this[ x , y-1 ] ) ? (byte)0b_0001_0000 : zero;

        //right side:
        result |= xPlus && yPlus && predicate.Execute( this[ x+1 , y+1 ] ) ? (byte)0b_0000_0010 : zero;
        result |= xPlus && predicate.Execute( this[ x+1 , y ] ) ? (byte)0b_0000_0100 : zero;
        result |= xPlus && yMinus && predicate.Execute( this[ x+1 , y-1 ] ) ? (byte)0b_0000_1000 : zero;

        //left side:
        result |= xMinus && yPlus && predicate.Execute( this[ x-1 , y+1 ] ) ? (byte)0b_0010_0000 : zero;
        result |= xMinus && predicate.Execute( this[ x-1 , y ] )==true ? (byte)0b_0000_0100 : zero;
        result |= xMinus && yMinus && predicate.Execute( this[ x-1 , y-1 ] ) ? (byte)0b_1000_0000 : zero;
        
        return result;
    }


    //TODO: make this work with NativeGrid or remove:
    // /// <summary>
    // /// AND operation on cells
    // /// </summary>
    // public bool TrueForEvery ( int x , int y , int w , int h , IPredicate<T> predicate , bool debug = false )
    // {
    //     bool result = true;
    //     ForEach(
    //         x , y , w , h ,
    //         (ax,ay) =>
    //         {
    //             //debug next field:
    //             if( debug==true )
    //             {
    //                 Debug.Log( $"\t\t{ ax }|{ ay } (debug = { debug })" );
    //             }

    //             //evaluate next field:
    //             if( predicate.Execute( ( this )[ ax , ay ] )==false )
    //             {
    //                 result = false;
    //                 return;
    //             }

    //         } ,
    //         (ax,ay) =>
    //         {
    //             //debug on out of bounds:
    //             if( debug==true )
    //             {
    //                 Debug.Log( string.Format( "\t\trect[{0},{1},{2},{3}] is out of grid bounds" , ax , ay , w , h ) );
    //             }

    //             //exe on out of bounds:
    //             result = false;
    //             return;
    //         }
    //     );
    //     return result;
    // }

    //TODO: make this work with NativeGrid or remove:
    // /// <summary>
    // /// OR operation on cells
    // /// </summary>
    // public bool TrueForAny ( int x , int y , int w , int h , IPredicate<T> predicate )
    // {
    //     bool result = false;
    //     ForEach(
    //         x , y , w , h ,
    //         (ax,ay) =>
    //         {
    //             if( predicate.Execute( ( this )[ ax , ay ] )==true )
    //             {
    //                 result = true;
    //                 return;
    //             }
    //         }
    //     );
    //     return result;
    // }


    /// <summary>
    /// Smooth operation
    /// TODO: Test!
    /// </summary>
    public void Smooth
    (
        int iterations ,
        IPredicate<STRUCT> countNeighbours ,
        IFunc<STRUCT,STRUCT> overThreshold ,
        IFunc<STRUCT,STRUCT> belowThreshold ,
        IFunc<STRUCT,STRUCT> equalsThreshold ,
        int threshold = 4
    )
    {
        for( int i=0 ; i<iterations ; i++ )
        for( int x=0 ; x<width ; x++ )
        for( int y=0 ; y<height ; y++ )
        {
            int neighbourWallTiles = GetSurroundingTypeCount( x , y , countNeighbours );
            if( neighbourWallTiles > threshold ) { ( this )[ x , y ] = overThreshold.Execute( ( this )[ x , y ] ); }
            else if( neighbourWallTiles < threshold ) { ( this )[ x , y ] = belowThreshold.Execute( ( this )[ x , y ] ); }
            else { ( this )[ x , y ] = equalsThreshold.Execute( ( this )[ x , y ] ); }
        }
    }


    /// <summary> Fill </summary>
    public JobHandle Fill ( STRUCT value , JobHandle dependency = default(JobHandle) )
    {
        var job = new FillJob<STRUCT>( array:_values , value:value );
        return writeAccess = job.Schedule(
            _values.Length , 1024 ,
            JobHandle.CombineDependencies( dependency , writeAccess )
        );
    }

    /// <summary> Fill rectangle </summary>
    public JobHandle Fill ( RectInt region , STRUCT value , JobHandle dependency = default(JobHandle) )
    {
        var job = new FillRegionJob<STRUCT>( region:region , array:this._values , value:value , array_width:this.width );
        return writeAccess = job.Schedule(
            region.width*region.height , 1024 ,
            JobHandle.CombineDependencies( dependency , writeAccess )
        );
    }
    public void Fill ( IPredicate<STRUCT> predicate , STRUCT fill )
    {
        int length = this.length;
        for( int i=0 ; i<length ; i++ )
            if( predicate.Execute(_values[i])==true )
                _values[i] = fill;
    }
    public void Fill ( IFunc<STRUCT> fillFunc )
    {
        int length = this.length;
        for( int i=0 ; i<length ; i++ )
            _values[i] = fillFunc.Execute();
    }
    /// <param name="fillFunc"> int params are x and y cordinates (index 2d)</param>
    public void Fill ( IFunc<int,int,STRUCT> fillFunc )
    {
        for( int x=0 ; x<width ; x++ )
            for( int y=0 ; y<height ; y++ )
                ( this )[ x , y ] = fillFunc.Execute(x,y);
    }
    /// <param name="fillFunc"> int param is index 1d </param>
    public void Fill ( IFunc<int,STRUCT> fillFunc )
    {
        int length = this.length;
        for( int i=0 ; i<length ; i++ )
            _values[i] = fillFunc.Execute( i );
    }

    
    /// <summary> Fills grid border cells. </summary>
    public JobHandle FillBorders ( STRUCT fill , JobHandle dependency = default(JobHandle) )
    {
        var job = new FillBordersJob<STRUCT>( array:_values , width:width , height:height , fill:fill );
        return writeAccess = job.Schedule( JobHandle.CombineDependencies(dependency,writeAccess) );
    }


    public JobHandle Copy ( RectInt region , out NativeGrid<STRUCT> copy ) => Copy( this , region , out copy );
    

    public void Dispose () => _values.Dispose();


    #endregion
}




/// <summary>
/// Abstract parent class for generic NativeGrid<STRUCT>. To simplify referencing static functions/types from "NativeGrid<byte>.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
/// </summary>
public abstract class NativeGrid
{
    #region PUBLIC METHODS


    /// <summary> Converts 1d to 2d array index </summary>
    public static int2 Index1dTo2d ( int i , int width )
    {
        #if DEBUG
        Assert_Index1dTo2d( i , width );
        #endif

        return new int2{ x=i%width , y=i/width };
    }


    /// <summary> Converts index 2d to 1d equivalent </summary>
    public static int Index2dTo1d ( int x , int y , int width )
    {
        #if DEBUG
        Assert_Index2dTo1d( x , y , width );
        #endif

        return y * width + x;
    }


    /// <summary> Translate regional coordinate to outer array index 1d </summary>
    /// <param name="R">Outer RectInt</param>
    /// <param name="r">Inner, smaller RectInt</param>
    /// <param name="rx">Inner x coordinate</param>
    /// <param name="ry">Inner y coordinate</param>
    /// <param name="R_width">Outer RectInt.width</param>
    public static int IndexTranslate ( RectInt r , int rx , int ry , int R_width )
    {
        #if DEBUG
        Assert_IndexTranslate( r , rx , ry , R_width );
        #endif

        return Index2dTo1d( r.x+rx , r.y+ry , R_width );
    }

    /// <summary> Translate regional coordinate to outer array index 1d </summary>
    /// <param name="R">Outer RectInt</param>
    /// <param name="r">Inner, smaller RectInt</param>
    /// <param name="rx">Inner x coordinate</param>
    /// <param name="ry">Inner y coordinate</param>
    /// <param name="R_width">Outer RectInt.width</param>
    public static int2 IndexTranslate ( RectInt r , int2 rxy )
    {
        #if DEBUG
        Assert_IndexTranslate( r , rxy.x , rxy.y );
        #endif

        return new int2{ x=r.x , y=r.y } + rxy;
    }

    /// <summary> Translate regional index to outer one </summary>
    /// <param name="R">Outer RectInt</param>
    /// <param name="r">Inner, smaller RectInt</param>
    /// <param name="ri">Index in inner rect</param>
    /// <param name="R_width">Outer RectInt.width</param>
    public static int IndexTranslate ( RectInt r , int ri , int R_width )
    {
        int2 ri2d = Index1dTo2d( ri , r.width );
        return IndexTranslate( r , ri2d.x , ri2d.y , R_width );
    }


    /// <summary> Determines whether index 2d is inside array bounds </summary>
    public static bool IsIndex2dValid ( int x , int y , int w , int h ) => x>=0 && x<w && y>=0 && y<h;


    /// <summary> Determines whether index 1d is inside array bounds </summary>
    public static bool IsIndex1dValid ( int i , int len ) => 0>=0 && i<len;


    public static JobHandle Copy <T>
    (
        NativeGrid<T> source ,
        RectInt region ,
        out NativeGrid<T> copy ,
        JobHandle dependency = default(JobHandle)
    ) where T : unmanaged
    {
        copy = new NativeGrid<T>( region.width , region.height , Allocator.TempJob );
        var job = new CopyRegionJob<T>(
            src: source.values ,
            dst: copy.values ,
            src_region: region ,
            src_width: source.width
        );
        return job.Schedule(
            region.width*region.height , 1024 ,
            JobHandle.CombineDependencies( source.writeAccess , dependency )
        );
    }


    /// <summary>
    /// Bresenham's line drawing algorithm (https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm).
    /// </summary>
    public System.Collections.Generic.IEnumerable<int2> BresenhamLine ( int2 A , int2 B )
    {
        int2 result = A;
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
        
        yield return result;
        
        if( dx>dy )
        {
            ai = (dy - dx) * 2;
            bi = dy * 2;
            d = bi - dx;

            while( result.x!=B.x )
            {
                if( d>=0 )
                {
                    result.x += xi;
                    result.y += yi;
                    d += ai;
                }
                else
                {
                    d += bi;
                    result.x += xi;
                }
                
                yield return result;
            }
        }
        else
        {
            ai = ( dx - dy ) * 2;
            bi = dx * 2;
            d = bi - dy;
            
            while( result.y!=B.y )
            {
                if( d>=0 )
                {
                    result.x += xi;
                    result.y += yi;
                    d += ai;
                }
                else
                {
                    d += bi;
                    result.y += yi;
                }
                
                yield return result;
            }
        }
    }


    #endregion
    #region ASSERTIONS


    #if DEBUG

    [Unity.Burst.BurstDiscard]
    static void Assert_IndexTranslate ( RectInt r , int rx , int ry , int R_width )
    {
        Assert.IsTrue( R_width>0 , $"FAILED: R_width ({R_width}) > 0" );
        Assert.IsTrue( r.width<=R_width , $"FAILED: r.width ({r.width}) > ({R_width})  R_width" );
        Assert_IndexTranslate( r , rx , ry );
    }
    [Unity.Burst.BurstDiscard]
    static void Assert_IndexTranslate ( RectInt r , int rx , int ry )
    {
        Assert.IsTrue( rx>=0 , $"FAILED: rx ({rx}) >= 0" );
        Assert.IsTrue( ry>=0 , $"FAILED: ry ({ry}) >= 0" );

        Assert.IsTrue( r.width>0 , $"FAILED: r.width ({r.width}) > 0" );
        Assert.IsTrue( r.height>0 , $"FAILED: r.height ({r.height}) > 0" );
        Assert.IsTrue( r.x>=0 , $"FAILED: r.x ({r.x}) >= 0" );
        Assert.IsTrue( r.y>=0 , $"FAILED: r.y ({r.y}) >= 0" );

        Assert.IsTrue( rx>=0 && rx<r.width , $"FAILED: rx ({rx}) is out of bounds for r ({r})" );
        Assert.IsTrue( ry>=0 && ry<r.height , $"FAILED: ry ({ry}) is out of bounds for r ({r})" );
    }

    [Unity.Burst.BurstDiscard]
    static void Assert_Index1dTo2d ( int i , int width )
    {
        Assert.IsTrue( width>0 , $"FAILED: width ({width}) > 0" );
        Assert.IsTrue( i>=0 , $"FAILED: i ({i}) >= 0" );
    }

    [Unity.Burst.BurstDiscard]
    static void Assert_Index2dTo1d ( int x , int y , int width )
    {
        Assert.IsTrue( width>0 , $"FAILED: width ({width}) > 0" );
        Assert.IsTrue( x>=0 , $"FAILED: x ({x}) >= 0" );
        Assert.IsTrue( y>=0 , $"FAILED: y ({y}) >= 0" );
        Assert.IsTrue( x<width , $"FAILED: x ({x}) < ({width}) width" );
    }

    #endif


    #endregion
    #region JOBS


    [Unity.Burst.BurstCompile]
    public struct CopyRegionJob <T> : IJobParallelFor where T : struct
    {
        [ReadOnly] NativeArray<T> src;
        [WriteOnly] NativeArray<T> dst;
        readonly RectInt src_region;
        readonly int src_width;
        public CopyRegionJob ( NativeArray<T> src , NativeArray<T> dst , RectInt src_region , int src_width )
        {
            this.src = src;
            this.dst = dst;
            this.src_region = src_region;
            this.src_width = src_width;
        }
        void IJobParallelFor.Execute ( int regionIndex ) => dst[regionIndex] = src[IndexTranslate(src_region,regionIndex,src_width)];
    }

    [Unity.Burst.BurstCompile]
    public struct FillJob <T> : IJobParallelFor where T : struct
    {
        [WriteOnly] NativeArray<T> array;
        readonly T value;
        public FillJob ( NativeArray<T> array , T value )
        {
            this.array = array;
            this.value = value;
        }
        void IJobParallelFor.Execute ( int i ) => array[i] = value;
    }

    [Unity.Burst.BurstCompile]
    public struct FillRegionJob <T> : IJobParallelFor where T : struct
    {
        [WriteOnly][NativeDisableParallelForRestriction]
        NativeArray<T> array;
        readonly int array_width;
        readonly RectInt region;
        readonly T value;
        public FillRegionJob ( NativeArray<T> array , int array_width , RectInt region , T value )
        {
            this.array = array;
            this.array_width = array_width;
            this.region = region;
            this.value = value;
        }
        void IJobParallelFor.Execute ( int regionIndex ) => array[IndexTranslate( region , regionIndex , array_width )] = value;
    }

    [Unity.Burst.BurstCompile]
    public struct FillBordersJob <T> : IJob where T : struct
    {
        [WriteOnly][NativeDisableParallelForRestriction]
        NativeArray<T> array;
        readonly int width;
        readonly int height;
        readonly T fill;
        public FillBordersJob ( NativeArray<T> array , int width , int height , T fill )
        {
            this.array = array;
            this.width = width;
            this.height = height;
            this.fill = fill;
        }
        void IJob.Execute ()
        {
            // fill horizontal border lines:
            int yMax = height-1;
            for( int x=0 ; x<width ; x++ )
            {
                array[Index2dTo1d(x,0,width)] = fill;
                array[Index2dTo1d(x,yMax,width)] = fill;
            }
            // fill vertical border lines:
            int xMax = width-1;
            for( int y = 1 ; y < height-1 ; y++ )
            {
                array[Index2dTo1d(0,y,width)] = fill;
                array[Index2dTo1d(xMax,y,width)] = fill;
            }
        }
    }

    [Unity.Burst.BurstCompile]
    public struct ForEachActionJob <T> : IJobParallelFor where T : struct
    {
        [ReadOnly] NativeArray<T> array;
        readonly IAction<T> action;
        public ForEachActionJob ( NativeArray<T> array , IAction<T> action )
        {
            this.array = array;
            this.action = action;
        }
        void IJobParallelFor.Execute ( int i ) => action.Execute( array[i] );
    }

    [Unity.Burst.BurstCompile]
    public struct ForEachIndexActionJob <T> : IJobParallelFor where T : struct
    {
        [ReadOnly] NativeArray<T> array;
        readonly IAction<int> action;
        public ForEachIndexActionJob ( NativeArray<T> array , IAction<int> action )
        {
            this.array = array;
            this.action = action;
        }
        void IJobParallelFor.Execute ( int i ) => action.Execute( i );
    }

    [Unity.Burst.BurstCompile]
    public struct ForEachIndex2dActionJob <T> : IJobParallelFor where T : struct
    {
        [ReadOnly] NativeArray<T> array;
        readonly int width;
        readonly IAction<int,int> action;
        public ForEachIndex2dActionJob ( NativeArray<T> array , int width , IAction<int,int> action )
        {
            this.array = array;
            this.width = width;
            this.action = action;
        }
        void IJobParallelFor.Execute ( int i )
        {
            int2 i2d = Index1dTo2d( i , width );
            action.Execute( i2d.x , i2d.y );
        }
    }

    [Unity.Burst.BurstCompile]
    public struct ForEachFuncJob <T> : IJobParallelFor where T : struct
    {
        [WriteOnly] NativeArray<T> array;
        readonly IFunc<T> func;
        public ForEachFuncJob ( NativeArray<T> array , IFunc<T> func )
        {
            this.array = array;
            this.func = func;
        }
        void IJobParallelFor.Execute ( int i ) => array[i] = func.Execute();
    }


    #endregion
    #region INTERFACES


    public interface IAction { void Execute(); }
    public interface IAction <ARG0> { void Execute( ARG0 arg0 ); }
    public interface IAction <ARG0,ARG1> { void Execute( ARG0 arg0 , ARG1 arg1 ); }

    public interface IFunc <RESULT> { RESULT Execute(); }
    public interface IFunc <ARG0,RESULT> { RESULT Execute( ARG0 arg0 ); }
    public interface IFunc <ARG0,ARG1,RESULT> { RESULT Execute( ARG0 arg0 , ARG1 arg1 ); }

    public interface IPredicate { bool Execute(); }
    public interface IPredicate <ARG0> { bool Execute( ARG0 arg0 ); }
    public interface IPredicate <ARG0,ARG1> { bool Execute( ARG0 arg0 , ARG1 arg1 ); }


    #endregion
}
