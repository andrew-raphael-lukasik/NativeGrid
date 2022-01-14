using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace NativeGridNamespace.Tests
{
	public class PathfindingTester : EditorWindow
	{

		VisualElement[] _grid;
		int _resolution = 128;
		float2 _offset = 0f;// perlin noise pos offset
		float2 _start01 = new float2{ x=0.1f , y=0.1f };
		int2 _startI2 => (int2)( _start01 * (_resolution-1) );
		float2 _dest01 = new float2{ x=0.9f , y=0.9f };
		int2 _destI2 => (int2)( _dest01 * (_resolution-1) );
		float2 _smoothstep = new float2{ x=0f , y=0.8f };// perlin noise post process
		float _hMultiplier = 1.5f;
		float _moveCostSensitivity = 10f;
		int _stepBudget = int.MaxValue;

		const int _drawTextMaxResolution = 50;
		bool labelsExist => _resolution<=_drawTextMaxResolution;

		public void OnEnable ()
		{
			var ROOT = rootVisualElement;
			var GRID = new VisualElement();
			var TOOLBAR = new VisualElement();
			var TOOLBAR_COLUMN_0 = new VisualElement();
			var TOOLBAR_COLUMN_1 = new VisualElement();
			ROOT.Add( TOOLBAR );
			TOOLBAR.Add( TOOLBAR_COLUMN_0 );
			TOOLBAR.Add( TOOLBAR_COLUMN_1 );
			ROOT.Add( GRID );

			{
				var style = TOOLBAR.style;
				style.flexDirection = FlexDirection.Row;
			}
			{
				var style = TOOLBAR_COLUMN_0.style;
				style.width = new Length( 50f , LengthUnit.Percent );
				style.flexDirection = FlexDirection.Column;
			}
			{
				var style = TOOLBAR_COLUMN_1.style;
				style.width = new Length( 50f , LengthUnit.Percent );
				style.flexDirection = FlexDirection.Column;
			}

			var RESOLUTION = new IntegerField( "Resolution:" );
			RESOLUTION.style.paddingLeft = RESOLUTION.style.paddingRight = 10;
			RESOLUTION.value = _resolution;
			RESOLUTION.RegisterValueChangedCallback( (e) => {
				_resolution = math.clamp( e.newValue , 1 , 256 );
				if( RESOLUTION.value!=_resolution )  RESOLUTION.value = _resolution;
				GRID.Clear();
				CreateGridLayout( GRID );
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			TOOLBAR_COLUMN_0.Add( RESOLUTION );

			var HEURISTIC_COST = new FloatField( $"H Multiplier:" );
			HEURISTIC_COST.style.paddingLeft = HEURISTIC_COST.style.paddingRight = 10;
			HEURISTIC_COST.value = _hMultiplier;
			HEURISTIC_COST.RegisterValueChangedCallback( (e)=> {
				_hMultiplier = e.newValue;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			TOOLBAR_COLUMN_0.Add( HEURISTIC_COST );

			var HEURISTIC_SEARCH = new FloatField( $"Move Cost Sensitivity:" );
			HEURISTIC_SEARCH.style.paddingLeft = HEURISTIC_SEARCH.style.paddingRight = 10;
			HEURISTIC_SEARCH.value = _moveCostSensitivity;
			HEURISTIC_SEARCH.RegisterValueChangedCallback( (e)=> {
				_moveCostSensitivity = e.newValue;
				NewRandomMap();
				SolvePath();
				Repaint();
			} );
			TOOLBAR_COLUMN_0.Add( HEURISTIC_SEARCH );

			var SMOOTHSTEP = new MinMaxSlider( "Move Cost Range:" , _smoothstep.x , _smoothstep.y , 0 , 1 );
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
			TOOLBAR_COLUMN_1.Add( SMOOTHSTEP );

			var START_DEST_LINE = new VisualElement();
			{
				// START_DEST_LINE.style.flexGrow = 1;
				START_DEST_LINE.style.flexDirection = FlexDirection.Row;

				var SPACE = new VisualElement();
				SPACE.style.flexGrow = 1;

				var START = new Label("Start:");
				var START_X = new Slider( 0 , 1 );
				var START_Y = new Slider( 0 , 1 );
				START_X.value = _start01.x;
				START_Y.value = _start01.y;
				START_X.style.flexGrow = 1;
				START_Y.style.flexGrow = 1;
				START_X.RegisterValueChangedCallback( (ctx) => {
					_start01.x = ctx.newValue;
					NewRandomMap();
					SolvePath();
					Repaint();
				} );
				START_Y.RegisterValueChangedCallback( (ctx) => {
					_start01.y = ctx.newValue;
					NewRandomMap();
					SolvePath();
					Repaint();
				} );
				START_DEST_LINE.Add( START );
				START_DEST_LINE.Add( SPACE );
				START_DEST_LINE.Add( START_X );
				START_DEST_LINE.Add( START_Y );
				START_DEST_LINE.Add( SPACE );

				var END = new Label("End:");
				var END_X = new Slider( 0 , 1 );
				var END_Y = new Slider( 0 , 1 );
				END_X.value = _dest01.x;
				END_Y.value = _dest01.y;
				END_X.style.flexGrow = 1;
				END_Y.style.flexGrow = 1;
				END_X.RegisterValueChangedCallback( (ctx) => {
					_dest01.x = ctx.newValue;
					NewRandomMap();
					SolvePath();
					Repaint();
				} );
				END_Y.RegisterValueChangedCallback( (ctx) => {
					_dest01.y = ctx.newValue;
					NewRandomMap();
					SolvePath();
					Repaint();
				} );
				START_DEST_LINE.Add( END );
				START_DEST_LINE.Add( SPACE );
				START_DEST_LINE.Add( END_X );
				START_DEST_LINE.Add( END_Y );
				START_DEST_LINE.Add( SPACE );
			}
			TOOLBAR_COLUMN_1.Add( START_DEST_LINE );

			var STEPLIMIT = new IntegerField("Step Budget:");
			{
				STEPLIMIT.value = _stepBudget;
				STEPLIMIT.RegisterValueChangedCallback( (ctx) => {
					if( ctx.newValue>=0 )
					{
						_stepBudget = ctx.newValue;
					}
					else
					{
						_stepBudget = 0;
						STEPLIMIT.SetValueWithoutNotify( 0 );
					}
					NewRandomMap();
					SolvePath();
					Repaint();
				} );
				STEPLIMIT.RegisterCallback( (WheelEvent e) => {
					Vector2 mouseScrollDelta = e.mouseDelta;
					int scrollDir = (int) Mathf.Sign(mouseScrollDelta.y);
					_stepBudget = Mathf.Max( _stepBudget - scrollDir , 0 );
					STEPLIMIT.SetValueWithoutNotify( _stepBudget );
					NewRandomMap();
					SolvePath();
					Repaint();
				} );
			}
			TOOLBAR_COLUMN_1.Add( STEPLIMIT );

			{
				var gridStyle = GRID.style;
				gridStyle.flexGrow = 1;
				gridStyle.flexDirection = FlexDirection.ColumnReverse;
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
			var window = GetWindow<PathfindingTester>();
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
				rowStyle.flexDirection = FlexDirection.Row;
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
			// prepare data:
			NativeArray<byte> moveCost;
			{
				int len = _resolution*_resolution;
				moveCost = new NativeArray<byte>( len , Allocator.TempJob , NativeArrayOptions.UninitializedMemory );
				byte[] arr = new byte[len];// NativeArray enumeration is slow outside Burst
				for( int i=len-1 ; i!=-1 ; i-- )
					arr[i] = (byte)( _grid[i].style.backgroundColor.value.r * 255 );
				moveCost.CopyFrom( arr );
			}

			// calculate:
			NativeList<int2> path;
			half[] fData;
			half[] gData;
			int2[] solution;
			int2[] visited;
			{
				path = new NativeList<int2>( _resolution , Allocator.TempJob );
				
				// run job:
				var watch = System.Diagnostics.Stopwatch.StartNew();
				var job = new NativeGrid.AStarJob(
					start: 					_startI2 ,
					destination:			_destI2 ,
					moveCost:				moveCost ,
					moveCostWidth:			_resolution ,
					results:				path ,
					hMultiplier:			_hMultiplier ,
					moveCostSensitivity:	_moveCostSensitivity ,
					stepBudget:				_stepBudget
				);
				job.Run();
				watch.Stop();
				bool success = job.Results.Length!=0;
				Debug.Log($"{nameof(NativeGrid.AStarJob)} took {(double)watch.ElapsedTicks/(double)System.TimeSpan.TicksPerMillisecond:G8} ms {(success?$"and succeeded in finding a path of {job.Results.Length} steps":"but <b>no path was found</b>")}.");

				// copy debug data:
				fData = job.F.ToArray();
				gData = job.G.ToArray();
				solution = job.Solution.ToArray();
				using( var nativeArray = job.Visited.ToNativeArray(Allocator.Temp) ) visited = nativeArray.ToArray();

				// dispose unmanaged arrays:
				job.Dispose();
			}

			// visualize:
			{
				// start cell
				int startI = NativeGrid.CoordToIndex( _startI2 , _resolution );
				var cellStyle = _grid[startI].style;
				Color col = cellStyle.backgroundColor.value * 0.75f;
				col.r = 1f;
				cellStyle.backgroundColor = col;
			}
			foreach( var coord in path )// path
			{
				int i = NativeGrid.CoordToIndex( coord , _resolution );
				var cellStyle = _grid[i].style;
				Color col = cellStyle.backgroundColor.value * 0.75f;
				col.r = 1f;
				cellStyle.backgroundColor = col;
			}
			foreach( var coord in visited )// visited
			{
				int i = NativeGrid.CoordToIndex( coord , _resolution );
				var CELL = _grid[i];
				
				var cellStyle = CELL.style;
				Color col = cellStyle.backgroundColor.value;
				col.b = 1f;
				cellStyle.backgroundColor = col;
			}
			if( labelsExist )
			for( int i=fData.Length-1 ; i!=-1 ; i-- )// labels
			{
				var CELL = _grid[i];
				int2 coord = NativeGrid.IndexToCoord( i , _resolution );
				Label LABEL = CELL[0] as Label;

				var f = fData[i];
				var g = gData[i];
				var h = NativeGrid.EuclideanHeuristic( coord , _destI2 );
				int2 origin = solution[i];
				if( f!=float.MaxValue )
				{
					LABEL.text = $"<b>[{coord.x},{coord.y}]</b>\n<b>F</b>:{f:G8}\n<b>G</b>:{g:G8}\n<b>H</b>:{h:G8}\nstep-1: [{origin.x},{origin.y}]";
					LABEL.visible = true;
				}
				else LABEL.visible = false;
			}

			// dispose data:
			moveCost.Dispose();
			path.Dispose();
		}

	}
}
