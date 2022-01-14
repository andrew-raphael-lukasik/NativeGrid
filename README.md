# NativeGrid

### NativeGrid:
A bunch of 2D data utilities for ECS and `Unity.Jobs`.

### NativeGrid<span><</span>T<span>></span>:
Managed container that stores `NativeArray` with it's basic info to use in 2d/grid manner.

---
- Decent A*/AStar implementation
  <p float="center">
    <img src="https://i.imgur.com/HsFXAGI.gif" width="49%">
    <img src="https://i.imgur.com/enK6UOs.gif" width="49%">
  </p>

  > Test window available under: Test>NativeGrid>Pathfinding

Here is an example how you can store store `RawTextureData` (pointer to a CPU-side texture buffer) inside this `NativeGrid<ARGB32>`-thing and do something ambiguously useful with it, like idk, trace and draw lines/paths on that texture:
```csharp
var job = GRID.FillLine( startPixel , endPixel , fillColor );
fillLineJob.Complete();
```
Full example code: [a relative link](/Samples~/Texture2D/NativeGridPaint.cs)
Note: this saves RAM because texture memory is not being duplicated on CPU-side anymore.
- Performant, allocation-free enumerator to find all neighbouring cells (`NeighbourEnumerator : INativeEnumerator<int2>`)
```
var enumerator = new NeighbourEnumerator( coord:new int2(0,1) , gridWidth:128 , gridHeight:128 );
while( enumerator.MoveNext(out int2 neighbourCoord) )
{
    /* wow, it's so easy :O */
}
```
- Trace lines (Bresenham's algorithm)
- Schedule your jobs to read/write to grid.Array using grid.Dependency JobHandle

# installation
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.nativegrid": "https://github.com/andrew-raphael-lukasik/NativeGrid.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/NativeGrid.git#upm
```
