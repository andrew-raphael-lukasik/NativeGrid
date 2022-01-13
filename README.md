# NativeGrid

Bunch of 2D data utilities for ECS, Unity.Jobs.

- Managed container to store state of unmanaged allocation
```
[SerializeField] int Width, Height;
void OnEnable ()
{
	Pixels = new NativeGrid<RGB24>( width:Width , height:Height , Allocator.Persistent );
}
void Update ()
{
	/* does something useful with Pixels */
}
void OnDisable ()
{
	Pixels.Dispose();
}
public struct RGB24 { public byte R,G,B; }
```
- Schedule your jobs to read/write to grid.Array using grid.Dependency JobHandle
- Performant, allocation-free enumerator to find all neighbouring cells (`NeighbourEnumerator : INativeEnumerator<int2>`)
```
var enumerator = new NeighbourEnumerator( coord:new int2(0,1) , gridWidth:128 , gridHeight:128 );
while( enumerator.MoveNext(out int2 neighbourCoord) )
{
    /* wow, it's so easy :O */
}
```
- Trace lines (Bresenham's algorithm)
- Decent A*/AStar implementation
  <p float="center">
    <img src="https://i.imgur.com/HsFXAGI.gif" width="49%">
    <img src="https://i.imgur.com/enK6UOs.gif" width="49%">
  </p>

  > Test window available under: Test>NativeGrid>Pathfinding

- You can process Texture2D without (managed) allocations by nesting it's native array inside NativeGrid<span><</span>RGB24<span>></span>. You can trace and draw lines/paths on that texture for example.
<br>(relevant raw color structs: https://github.com/andrew-raphael-lukasik/RawTextureDataProcessingExamples)

# installation
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.nativegrid": "https://github.com/andrew-raphael-lukasik/NativeGrid.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/NativeGrid.git#upm
```
