/// homepage: https://github.com/andrew-raphael-lukasik/NativeGrid

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

/// <summary>
/// Abstract parent class for generic NativeGrid<STRUCT>. To simplify referencing static functions/types from "NativeGrid<byte>.Index1dTo2d(i)" to "NativeGrid.Index1dTo2d(i)".
/// </summary>
public abstract partial class NativeGrid
{
	#region JOBS


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
			src: source.Array ,
			dst: copy.Array ,
			src_region: region ,
			src_width: source.Width
		);
		return job.Schedule(
			region.width*region.height , 1024 ,
			JobHandle.CombineDependencies( source.Dependency , dependency )
		);
	}

	public unsafe struct CopyJob <T> : IJob where T : unmanaged
	{
		[ReadOnly] NativeArray<T> src;
		void* dst;
		public CopyJob ( NativeArray<T> src , void* dst )
		{
			this.src = src;
			this.dst = dst;
		}
		unsafe void IJob.Execute ()
		{
			//ASSERTION: sizeof(SRC)==sizeof(DST)
			UnsafeUtility.MemCpy(
				dst ,
				NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks( src ) ,
				src.Length * (long)UnsafeUtility.SizeOf<T>()
			);
		}
	}

	public struct CopyJob <SRC,DST> : IJob
		where SRC : unmanaged
		where DST : unmanaged
	{
		[ReadOnly] NativeArray<SRC> src;
		[WriteOnly] NativeArray<DST> dst;
		public CopyJob ( NativeArray<SRC> src , NativeArray<DST> dst )
		{
			this.src = src;
			this.dst = dst;
		}
		unsafe void IJob.Execute ()
		{
			//ASSERTION: sizeof(SRC)==sizeof(DST)
			UnsafeUtility.MemCpy(
				NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks( dst ) ,
				NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks( src ) ,
				dst.Length * (long)UnsafeUtility.SizeOf<SRC>()
			);
		}
	}

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
