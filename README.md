# NativeGrid
GOALS:
1. Create grid data class that works well with Unity.Jobs and ECS.
<br>"Grid", here, means 1d array that usefully pretends to be 2d to bring some ease working with data that has some kind of 2 spatial components (screen/texture space, XZ world space, etc.).
2. No GC allocations outside DEBUG. It's no NativeContainer tho.

  Note: NativeGrid will remain a class. Mutable fields such as 'JobHandle Dependency' benefits from being allocated somewhere on the heap. Also this enables inheritance semantics providing some additional extensibility.

WARNING: Not all features are production-ready, this is work-in-progress code

FEATURES:
- Uses NativeArray<span><</span>T<span>></span> where T : unmanaged
- Schedule your jobs to read/write to grid.Array using grid.Dependency JobHandle (!)
- enumerate neighbouring cells, enumerate all cells along growing spiral-shaped path
- Bresenham's trace line algorithm
- A*/AStar implementation
- You can process Texture2D without (managed) allocations by nesting it's native array inside NativeGrid<span><</span>RGB24<span>></span>. You can trace and draw lines/paths on that texture for example.
<br>(relevant raw color structs: https://github.com/andrew-raphael-lukasik/RawTextureDataProcessingExamples)
- Marching squares method (all 8 neighbours to byte)

TODO:
- continue ideas started with https://github.com/andrew-raphael-lukasik/GridT
- make it more usefull
- trace bezier curves
- improve A* speed

![astar gif goes here](https://i.imgur.com/vW5bVeQ.gif)

Test window available under: Test>NativeGrid>Pathfinding

# instalation
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.nativegrid": "https://github.com/andrew-raphael-lukasik/NativeGrid.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/NativeGrid.git#upm
```
