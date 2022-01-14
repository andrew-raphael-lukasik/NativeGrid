# NativeGrid

### NativeGrid:
A bunch of 2D data utilities for ECS and `Unity.Jobs`.

### NativeGrid<span><</span>T<span>></span>:
Managed container that stores `NativeArray` with it's basic info to use in 2d/grid manner.

---

Here is an example how you can store store `RawTextureData` (pointer to a CPU-side texture buffer) inside this NativeGrid<span><</span>RGB24<span>></span>-thing and do something ambiguously useful with it: trace and draw lines/paths on that texture ([more relevant info](https://github.com/andrew-raphael-lukasik/RawTextureDataProcessingExamples)).

Note: this saves RAM because texture memory is not being duplicated on CPU-side anymore.
```
using UnityEngine;
using Unity.Mathematics;
using NativeGridNamespace;
public class NativeGridPaint : MonoBehaviour
{
	NativeGrid<ARGB32> GRID;
	[SerializeField] int _width = 512, _height = 512;
	[SerializeField] Color32 _color = Color.yellow;
	Texture2D _texture = null;
	int2 _prevCoord;
	void OnEnable ()
	{
		_texture = new Texture2D( _width , _height , TextureFormat.ARGB32 , 0 , true );
		GRID = new NativeGrid<ARGB32>( width:_width , height:_height , _texture.GetRawTextureData<ARGB32>() );
		var fillJobHandle = GRID.Fill( new ARGB32{ A=0 , R=255 , G=255 , B=255 } , GRID.Dependency );
		fillJobHandle.Complete();
		_texture.Apply();
	}
	void OnDisable ()
	{
		Destroy( _texture );
		GRID.Dispose();
	}
	void Update ()
	{
		int2 coord = (int2) math.round( Input.mousePosition / new Vector2{ x=Screen.width , y=Screen.height } * new Vector2{ x=_texture.width , y=_texture.height } );
		if( math.any(coord!=_prevCoord) && math.all(new bool4{ x=coord.x>=0 , y=coord.y>=0 , z=coord.x<_texture.width , w=coord.y<_texture.height }) )
		{
			if( Input.GetMouseButtonDown(0) )
			{
				_prevCoord = coord;
				GRID[coord] = _color;
				_texture.Apply();
			}
			else if( Input.GetMouseButton(0) )
			{
				var fillLineJob = GRID.FillLine( _prevCoord , coord , _color );
				fillLineJob.Complete();
				_prevCoord = coord;
				_texture.Apply();
			}
		}
	}
	void OnGUI () => Graphics.DrawTexture( new Rect{ width=Screen.width , height=Screen.height } , _texture );
	public struct ARGB32
	{
		public byte A,R,G,B;
		public static implicit operator ARGB32 ( Color32 col ) => new ARGB32{ A=col.a , R=col.r , G=col.g , B=col.b };
	}
}

```
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
