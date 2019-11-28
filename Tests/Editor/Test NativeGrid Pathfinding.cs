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

		const float _pxSize = 512;
		VisualElement[] _grid;
		int _resolution = 128;
		float _offset = 0f;

		float2 _p1 = new float2{ x=0.1f , y=0.1f };
		float2 _p2 = new float2{ x=0.9f , y=0.9f };
		float heuristic_cost = 0.001f;
		float heuristic_search = 20f;

		const int _drawTextMaxResolution = 50;
		bool labelsExist => _resolution<=_drawTextMaxResolution;

		public void OnEnable ()
		{
			var ROOT = rootVisualElement;
			var GRID = new VisualElement();

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
			ROOT.Add( RESOLUTION );

			// COST HEURISTIC:
			var HEURISTIC_COST = new FloatField( $"Cost Heuristic:" );
			HEURISTIC_COST.style.paddingLeft = HEURISTIC_COST.style.paddingRight = 10;
			HEURISTIC_COST.value = heuristic_cost;
			HEURISTIC_COST.RegisterValueChangedCallback( (e)=> {
				heuristic_cost = e.newValue;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			ROOT.Add( HEURISTIC_COST );

			// SEARCH HEURISTIC:
			var HEURISTIC_SEARCH = new FloatField( $"Search Heuristic:" );
			HEURISTIC_SEARCH.style.paddingLeft = HEURISTIC_SEARCH.style.paddingRight = 10;
			HEURISTIC_SEARCH.value = heuristic_search;
			HEURISTIC_SEARCH.RegisterValueChangedCallback( (e)=> {
				heuristic_search = e.newValue;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			ROOT.Add( HEURISTIC_SEARCH );

			// GRID:
			var gridStyle = GRID.style;
			gridStyle.width = _pxSize;
			gridStyle.height = _pxSize;
			gridStyle.marginBottom = gridStyle.marginLeft = gridStyle.marginRight = gridStyle.marginTop = 2;
			gridStyle.backgroundColor = new Color( 0f , 0f , 0f , 0.02f );
			GRID.RegisterCallback( (MouseDownEvent e)=>{
				_offset = (float)EditorApplication.timeSinceStartup;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			ROOT.Add( GRID );
			CreateGridLayout( GRID );

			NewRandomMap();
			SolvePath();
		}

		[MenuItem("Test/NativeGrid/Pathfinding")]
		static void ShowWindow ()
		{
			var window = GetWindow<Test_NativeGrid_Pathfinding>();
			window.titleContent = new GUIContent("NativeGrid Pathfinding Test");
			window.minSize = window.maxSize = new Vector2{ x=_pxSize+4 , y=_pxSize+4+60 };
		}

		void CreateGridLayout ( VisualElement GRID )
		{
			float pxCell = _pxSize / (float)_resolution;
			_grid = new VisualElement[ _resolution*_resolution ];
			for( int i=0, y=0 ; y<_resolution ; y++ )
			{
				var ROW = new VisualElement();
				var rowStyle = ROW.style;
				rowStyle.flexDirection = FlexDirection.RowReverse;
				rowStyle.width = _pxSize;
				rowStyle.height = pxCell;

				for( int x=0 ; x<_resolution ; x++, i++ )
				{
					var CELL = new VisualElement();
					var cellStyle = CELL.style;
					cellStyle.width = pxCell;
					cellStyle.height = pxCell;

					if( labelsExist )
					{
						var LABEL = new Label("00");
						LABEL.visible = false;
						var style = LABEL.style;
						style.fontSize = pxCell/5;
						style.alignSelf = Align.Stretch;
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
				float fx = (float)x * frac * 4f + _offset;
				float fy = (float)y * frac * 4f + _offset;
				float noise1 = Mathf.PerlinNoise( fx , fy );
				float noise2 = math.pow( Mathf.PerlinNoise(fx*2.3f,fy*2.3f) , 3f );
				float noise3 = math.pow( Mathf.PerlinNoise(fx*14f,fy*14f) , 6f ) * (1f-noise1) * (1f-noise2);
				float noiseSum = math.pow( noise1 + noise2*0.3f + noise3*0.08f , 3.6f );
				_grid[i].style.backgroundColor = new Color{ r=noiseSum , g=noiseSum , b=noiseSum , a=1f };
			}
		}

		void SolvePath ()
		{
			//prepare data:
			NativeArray<float> moveCost;
			{
				int len = _resolution*_resolution;
				moveCost = new NativeArray<float>( len , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
				float[] arr = new float[len];//NativeArray enumeration is slow outside Burst
				for( int i=len-1 ; i!=-1 ; i-- )
				{
					arr[i] = _grid[i].style.backgroundColor.value.r;
				}
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
					(int2)( _p1 * _resolution ) , (int2)( _p2 * _resolution ) ,
					moveCost , _resolution ,
					heuristic_cost ,
					heuristic_search ,
					path
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
