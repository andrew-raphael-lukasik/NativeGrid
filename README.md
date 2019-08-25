# NativeGrid
GOALs:
1. Create grid data class that works well with Unity.Jobs and ECS.
2. No GC allocations outside DEBUG. It's not a NativeContainer tho (Im thinking about it but it's not decided yet)

WARNING: this is work in progress, not production ready yet.

FEATURES:
- Uses NativeArray<span><</span>STRUCT<span>></span>
- Write your own jobs to read/write to grid.values using grid.writeAccess JobHandle (!)
- Bresenham's trace line algorithm
- You can process Texture2D without allocations by nesting it's native array inside NativeGrid<span><</span>RGB24<span>></span> (relevant structs: https://github.com/andrew-raphael-lukasik/RawTextureDataProcessingExamples)
- Marching squares method (neighbours to byte)

TODO:
- continue ideas started with https://github.com/andrew-raphael-lukasik/GridT
- make it more usefull
