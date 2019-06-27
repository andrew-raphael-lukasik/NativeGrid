/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


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


    /// <summary> Gets the surrounding field values </summary>
    /// <returns>
    /// 8-bit clockwise-enumerated bit values 
    /// 7 0 1           [x-1,y+1]  [x,y+1]  [x+1,y+1]
    /// 6   2     ==    [x-1,y]     [x,y]     [x+1,y]
    /// 5 4 3           [x-1,y-1]  [x,y-1]  [x+1,y-1]
    /// for example: 1<<0 is top, 1<<1 is top-right, 1<<2 is right, 1<<6|1<<4|1<<2 is both left,down and right
    /// </returns>
    public byte GetMarchingSquares ( int x , int y , System.Predicate<STRUCT> predicate )
    {
        const byte zero = 0b_0000_0000;
        byte result = zero;

        //out of bounds test:
        bool xPlus = x+1 < width;
        bool yPlus = y+1 < height;
        bool xMinus = x-1 >= 0;
        bool yMinus = y-1 >= 0;

        //top, down:
        result |= yPlus && predicate(this[x,y+1]) ? (byte)0b_0000_0001 : zero;
        result |= yMinus && predicate(this[x,y-1]) ? (byte)0b_0001_0000 : zero;

        //right side:
        result |= xPlus && yPlus && predicate(this[x+1,y+1]) ? (byte)0b_0000_0010 : zero;
        result |= xPlus && predicate(this[x+1,y]) ? (byte)0b_0000_0100 : zero;
        result |= xPlus && yMinus && predicate(this[x+1,y-1]) ? (byte)0b_0000_1000 : zero;

        //left side:
        result |= xMinus && yPlus && predicate(this[x-1,y+1]) ? (byte)0b_0010_0000 : zero;
        result |= xMinus && predicate(this[x-1,y]) ? (byte)0b_0000_0100 : zero;
        result |= xMinus && yMinus && predicate(this[x-1,y-1]) ? (byte)0b_1000_0000 : zero;
        
        return result;
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


    /// <summary> Point from 2d indices </summary>
    public static float2 Index2dToPoint ( int x , int y , float stepX , float stepY ) => new float2{ x=(float)x*stepX , y=(float)y*stepY };


    /// <summary> Value at point </summary>
    public static T PointToValue <T> ( float2 point , float2 worldSize , NativeArray<T> array , int width , int height ) where T : unmanaged
    {
        return array[ PointToIndex( point , worldSize , width , height ) ];
    }


    /// <summary> Index from point </summary>
    public static int PointToIndex ( float2 point , float2 worldSize , int width , int height )
    {
        int2 xy = PointToIndex2d( point , worldSize , width , height );
        return Index2dTo1d( xy.x , xy.y , width );
    }


    /// <summary> Index 2d from point </summary>
    public static int2 PointToIndex2d ( float2 point , float2 worldSize , int width , int height )
    {
        float2 clampedPoint = math.clamp( point , float2.zero , worldSize );
        float2 normalized = clampedPoint / worldSize;
        int2 lastIndex = new int2{ x=width-1 , y=height-1 };
        return MidpointRoundingAwayFromZero( normalized*lastIndex );
    }


    public static int MidpointRoundingAwayFromZero ( float value ) => (int)( value + (value<0f ? -0.5f : 0.5f) );
    public static float MidpointRoundingAwayFromZero ( float value , float step ) => (float)MidpointRoundingAwayFromZero( value/step ) * step;
    public static int2 MidpointRoundingAwayFromZero ( float2 value ) => new int2{ x=(int)( value.x + ( value.x<0f ? -0.5f : 0.5f ) ) , y=(int)( value.y + ( value.y<0f ? -0.5f : 0.5f ) ) };
    public static float2 MidpointRoundingAwayFromZero ( float2 value , float2 step ) => (float2)MidpointRoundingAwayFromZero( value/step ) * step;
    

    /// <summary> Bresenham's line drawing algorithm (https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm). </summary>
    public static System.Collections.Generic.IEnumerable<int2> TraceLine ( int2 A , int2 B )
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
    public struct CopyRegionJob <T> : IJobParallelFor where T : unmanaged
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
    public struct FillJob <T> : IJobParallelFor where T : unmanaged
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
    public struct FillRegionJob <T> : IJobParallelFor where T : unmanaged
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
    public struct FillBordersJob <T> : IJob where T : unmanaged
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


    #endregion
}
