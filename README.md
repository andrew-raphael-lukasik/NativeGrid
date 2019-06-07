# NativeGrid
GOAL: Create grid data class that works well with Unity.Jobs and ECS

WARNING: this is work in progress, not everything is as it should yet

FEATURES:
- Uses NativeArray<span><</span>STRUCT<span>></span> (no big GC allocations)
- Write your own jobs to read/write to grid.values using grid.writeAccess JobHandle (!)
- You can process Texture2D without allocations by nesting it's native array inside NativeGrid<span><</span>RGB24<span>></span> (relevant structs: https://github.com/andrew-raphael-lukasik/RawTextureDataProcessingExamples)

TODO:
- continue ideas started with https://github.com/andrew-raphael-lukasik/GridT
- make it more usefull
