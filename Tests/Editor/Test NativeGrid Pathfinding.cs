using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace Tests
{
	public class Test_NativeGrid_Pathfinding : EditorWindow
	{

		VisualElement[] _grid;
		int _resolution = 128;
		float2 _offset = 0f;// perlin noise pos offset

		float2 _start01 = new float2{ x=0.1f , y=0.1f };
		float2 _dest01 = new float2{ x=0.9f , y=0.9f };
		float heuristic_cost = 0.001f;
		float heuristic_search = 20f;
		float2 _smoothstep = new float2{ x=0.1f , y=0.3f };// perlin noise post process
		int _steplimit = int.MaxValue;

		const int _drawTextMaxResolution = 50;
		bool labelsExist => _resolution<=_drawTextMaxResolution;

		public void OnEnable ()
		{
			var ROOT = rootVisualElement;
			var GRID = new VisualElement();
			var TOOLBAR = new VisualElement();
			ROOT.Add( TOOLBAR );
			ROOT.Add( GRID );

			var RESOLUTION = new IntegerField( "Resolution:" );
			RESOLUTION.style.paddingLeft = RESOLUTION.style.paddingRight = 10;
			RESOLUTION.value = _resolution;
			RESOLUTION.RegisterValueChangedCallback( (e) => {
				_resolution = math.clamp( e.newValue , 1 , 256 );
				if( RESOLUTION.value!=_resolution )  RESOLUTION.value = _resolution;
				GRID.Clear();
				CreateGridLayout( GRID );
				NewRandomMap();
				Repaint();
			} );
			TOOLBAR.Add( RESOLUTION );

			var HEURISTIC_COST = new FloatField( $"Cost Heuristic:" );
			HEURISTIC_COST.style.paddingLeft = HEURISTIC_COST.style.paddingRight = 10;
			HEURISTIC_COST.value = heuristic_cost;
			HEURISTIC_COST.RegisterValueChangedCallback( (e)=> {
				heuristic_cost = e.newValue;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			TOOLBAR.Add( HEURISTIC_COST );

			var HEURISTIC_SEARCH = new FloatField( $"Search Heuristic:" );
			HEURISTIC_SEARCH.style.paddingLeft = HEURISTIC_SEARCH.style.paddingRight = 10;
			HEURISTIC_SEARCH.value = heuristic_search;
			HEURISTIC_SEARCH.RegisterValueChangedCallback( (e)=> {
				heuristic_search = e.newValue;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			TOOLBAR.Add( HEURISTIC_SEARCH );

			var SMOOTHSTEP = new MinMaxSlider( "Levels" , _smoothstep.x , _smoothstep.y , 0 , 1 );
			{
				var style = SMOOTHSTEP.style;
				style.marginBottom = style.marginLeft = style.marginRight = style.marginTop = 2;
			}
			SMOOTHSTEP.RegisterValueChangedCallback( (ctx) => {
				_smoothstep = ctx.newValue;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			TOOLBAR.Add( SMOOTHSTEP );

			var STEPLIMIT = new IntegerField("Step Limit");
			{
				STEPLIMIT.value = _steplimit;
				STEPLIMIT.RegisterValueChangedCallback( (ctx) => {
					if( ctx.newValue>=0 )
					{
						_steplimit = ctx.newValue;
					}
					else
					{
						_steplimit = 0;
						STEPLIMIT.SetValueWithoutNotify( 0 );
					}
					NewRandomMap();
					SolvePath();
					Repaint();
				} );
			}
			TOOLBAR.Add( STEPLIMIT );

			{
				var gridStyle = GRID.style;
				gridStyle.flexGrow = 1;
				gridStyle.marginBottom = gridStyle.marginLeft = gridStyle.marginRight = gridStyle.marginTop = 2;
				gridStyle.backgroundColor = new Color{ a = 0.02f };
			}
			GRID.RegisterCallback( (MouseDownEvent e)=>{
				_offset = (float) EditorApplication.timeSinceStartup;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			CreateGridLayout( GRID );

			NewRandomMap();
			SolvePath();
		}

		[MenuItem("Test/NativeGrid/Pathfinding")]
		static void ShowWindow ()
		{
			var window = GetWindow<Test_NativeGrid_Pathfinding>();
			window.titleContent = new GUIContent("NativeGrid Pathfinding Test");
			window.minSize = new Vector2{ x=512+4 , y=512+4+60 };
		}

		void CreateGridLayout ( VisualElement GRID )
		{
			_grid = new VisualElement[ _resolution*_resolution ];
			for( int i=0, y=0 ; y<_resolution ; y++ )
			{
				var ROW = new VisualElement();
				var rowStyle = ROW.style;
				rowStyle.flexDirection = FlexDirection.RowReverse;
				rowStyle.flexGrow = 1;

				for( int x=0 ; x<_resolution ; x++, i++ )
				{
					var CELL = new VisualElement();
					CELL.style.flexGrow = 1;
					if( labelsExist )
					{
						var LABEL = new Label("00");
						LABEL.visible = false;
						LABEL.StretchToParentSize();
						LABEL.style.unityTextAlign = TextAnchor.MiddleCenter;
						CELL.Add( LABEL );
					}

					ROW.Add( CELL );
					_grid[i] = CELL;
				}
				
				GRID.Add( ROW );
			}
		}

		void NewRandomMap ()
		{
			float frac = 1f / (float)_resolution;
			for( int i=0, y=0 ; y<_resolution ; y++ )
			for( int x=0 ; x<_resolution ; x++, i++ )
			{
				float fx = (float)x * frac * 4f + _offset.x;
				float fy = (float)y * frac * 4f + _offset.y;
				float noise1 = Mathf.PerlinNoise( fx , fy );
				float noise2 = math.pow( Mathf.PerlinNoise(fx*2.3f,fy*2.3f) , 3f );
				float noise3 = math.pow( Mathf.PerlinNoise(fx*14f,fy*14f) , 6f ) * (1f-noise1) * (1f-noise2);
				float noiseSum = math.pow( noise1 + noise2*0.3f + noise3*0.08f , 3.6f );
				float smoothstep = math.smoothstep( _smoothstep.x , _smoothstep.y , noiseSum );
				_grid[i].style.backgroundColor = new Color{ r=smoothstep , g=smoothstep , b=smoothstep , a=1f };
			}
		}

		void SolvePath ()
		{
			//prepare data:
			NativeArray<byte> moveCost;
			{
				int len = _resolution*_resolution;
				moveCost = new NativeArray<byte>( len , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
				byte[] arr = new byte[len];// NativeArray enumeration is slow outside Burst
				for( int i=len-1 ; i!=-1 ; i-- )
					arr[i] = (byte)( _grid[i].style.backgroundColor.value.r * 255 );
				moveCost.CopyFrom( arr );
			}

			//calculate:
			NativeList<int2> path;
			float[] debug_F;
			int2[] visited;
			{
				path = new NativeList<int2>( _resolution , Allocator.TempJob );

				#if DEBUG
				var watch = System.Diagnostics.Stopwatch.StartNew();
				#endif

				//run job:
				var job = new NativeGrid.AStarJob(
					start: 				(int2)( _start01 * _resolution ) ,
					destination:		(int2)( _dest01 * _resolution ) ,
					moveCost:			moveCost ,
					moveCost_width:		_resolution ,
					heuristic_cost:		heuristic_cost ,
					heuristic_search:	heuristic_search ,
					output_path:		path ,
					step_limit:			_steplimit
				);
				job.Run();

				#if DEBUG
				watch.Stop();
				Debug.Log($"{nameof(NativeGrid.AStarJob)} took {watch.ElapsedMilliseconds} ms");
				#endif

				// copy debug data:
				debug_F = job._F_.ToArray();
				using( var arr = job.visited.GetKeyArray( Allocator.Temp ) ) visited = arr.ToArray();

				//dispose unmanaged arrays:
				job.Dispose();
			}

			//visualize:
			foreach( var i2 in path )
			{
				int i = NativeGrid.Index2dTo1d( i2 , _resolution );
				var CELL = _grid[i];
				
				var cellStyle = CELL.style;
				Color col = cellStyle.backgroundColor.value;
				col.r = 1f;
				cellStyle.backgroundColor = col;
			}
			if( labelsExist )
			for( int i=debug_F.Length-1 ; i!=-1 ; i-- )
			{
				var CELL = _grid[i];
				Label LABEL = CELL[0] as Label;

				var f = debug_F[i];
				if( f!=float.MaxValue )
				{
					LABEL.text = $"f:{f:0.000}";
					LABEL.visible = true;
				}
				else
				{
					LABEL.visible = false;
				}
			}
			foreach( var i2 in visited )
			{
				int i = NativeGrid.Index2dTo1d( i2 , _resolution );
				var CELL = _grid[i];
				
				var cellStyle = CELL.style;
				Color col = cellStyle.backgroundColor.value;
				col.b = 1f;
				cellStyle.backgroundColor = col;
			}

			//dispose data:
			moveCost.Dispose();
			path.Dispose();
		}

	}
}
