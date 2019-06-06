# NativeGrid
GOAL: Create grid data that works well with Unity.Jobs and ECS

FEATURES:
- Uses NativeArray<STRUCT>
- Write your own jobs to read/write to grid.values using grid.writeAccess JobHandle (!)
- Contains some useful methods when working with grid data layout

TODO:
- continue porting (this is work in progress project)
