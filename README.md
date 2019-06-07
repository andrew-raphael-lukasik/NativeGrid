# NativeGrid
GOAL: Create grid data class that works well with Unity.Jobs and ECS

WARNING: this is work in progress, not everything is as it should yet

FEATURES:
- Uses NativeArray<STRUCT>
- Write your own jobs to read/write to grid.values using grid.writeAccess JobHandle (!)
- Contains (hopefully) useful methods when working with grid data layout

TODO:
- continue ideas started with https://github.com/andrew-raphael-lukasik/GridT
- make it more usefull
