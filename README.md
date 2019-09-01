# NativeGrid
GOALS:
1. Create grid data class that works well with Unity.Jobs and ECS.
2. No GC allocations outside DEBUG. It's no NativeContainer tho.

WARNING: Not production ready, work in progress

FEATURES:
- Uses NativeArray<span><</span>T<span>></span> where T : unmanaged
- Write your own jobs to read/write to grid.values using grid.writeAccess JobHandle (!)
- enumerate neighbouring cells, enumerate all cells along growing spiral-shaped path
- Bresenham's trace line algorithm
- A*/AStar implementation
- You can process Texture2D without allocations by nesting it's native array inside NativeGrid<span><</span>RGB24<span>></span> (relevant structs: https://github.com/andrew-raphael-lukasik/RawTextureDataProcessingExamples)
- Marching squares method (neighbours to byte)

TODO:
- continue ideas started with https://github.com/andrew-raphael-lukasik/GridT
- make it more usefull
- improve A* speed

![astar gif goes here](https://i.imgur.com/np3guuA.gif)

Test window available under: Test>NativeGrid>Pathfinding
