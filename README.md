# NativeGrid
GOAL: Create grid data that works well with Unity.Jobs and ECS
WARNING: this is work in progress, not everything as it should yet

FEATURES:
- Uses NativeArray<STRUCT>
- Write your own jobs to read/write to grid.values using grid.writeAccess JobHandle (!)
- Contains some useful methods when working with grid data layout

TODO:
- continue porting
