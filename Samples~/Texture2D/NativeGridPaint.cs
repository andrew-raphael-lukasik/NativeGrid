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
			if( !Input.GetKey(KeyCode.LeftAlt) )
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
			else
			{
				if( GRID.IsCoordValid(coord) )
					_color = GRID[coord];
			}
		}
	}

	void OnGUI () => Graphics.DrawTexture( new Rect{ width=Screen.width , height=Screen.height } , _texture );

	public struct ARGB32
	{
		public byte A,R,G,B;
		public static implicit operator ARGB32 ( Color32 rgba32 ) => new ARGB32{ A=rgba32.a , R=rgba32.r , G=rgba32.g , B=rgba32.b };
		public static implicit operator Color32 ( ARGB32 argb32 ) => new Color32{ r=argb32.R , g=argb32.G , b=argb32.B , a=argb32.A };
	}

}
