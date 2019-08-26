#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace Tests
{
    public class Test_NativeGrid_pathfinding : EditorWindow
    {

        const float _pxSize = 512;
        const int _numRows = 128;
        float2 _p1 = new float2{ x=0.1f , y=0.1f };
        float2 _p2 = new float2{ x=0.9f , y=0.9f };
        [SerializeField] float _elasticity = 1f;
        [SerializeField] float _offset = 0f;

        VisualElement[] _grid;

        public void OnEnable ()
        {
            var ROOT = rootVisualElement;

            var SLIDER = new Slider( $"Elasticity:" , 0f , 1f );
            SLIDER.style.paddingLeft = SLIDER.style.paddingRight = 10;
            SLIDER.value = _elasticity;
            SLIDER.RegisterValueChangedCallback( (e)=> {
                _elasticity = e.newValue;
                NewRandomMap();
                SolvePath();
            } );
            ROOT.Add( SLIDER );

            var GRID = new ScrollView();//new VisualElement();
            var gridStyle = GRID.style;
            gridStyle.width = _pxSize;
            gridStyle.height = _pxSize;
            gridStyle.marginBottom = gridStyle.marginLeft = gridStyle.marginRight = gridStyle.marginTop = 2;
            gridStyle.backgroundColor = new Color( 0f , 0f , 0f , 0.02f );
            GRID.RegisterCallback( (MouseDownEvent e)=>{
                _offset = (float)EditorApplication.timeSinceStartup;
                NewRandomMap();
                SolvePath();
            } );
            ROOT.Add( GRID );

            float pxCell = _pxSize / (float)_numRows;
            _grid = new VisualElement[ _numRows*_numRows ];
            for( int i=0, y=0 ; y<_numRows ; y++ )
            {
                var ROW = new VisualElement();
                var rowStyle = ROW.style;
                rowStyle.flexDirection = FlexDirection.RowReverse;
                rowStyle.width = _pxSize;
                rowStyle.height = pxCell;

                for( int x=0 ; x<_numRows ; x++, i++ )
                {
                    var CELL = new VisualElement();
                    var cellStyle = CELL.style;
                    cellStyle.width = pxCell;
                    cellStyle.height = pxCell;

                    ROW.Add( CELL );
                    _grid[i] = CELL;
                }
                
                GRID.Add( ROW );
            }

            NewRandomMap();
            SolvePath();
        }

        [MenuItem("Test/NativeGrid/Pathfinding")]
        static void ShowWindow ()
        {
            var window = GetWindow<Test_NativeGrid_pathfinding>();
            window.titleContent = new GUIContent("Test NativeGrid pathfinding");
            window.minSize = window.maxSize = new Vector2{ x=_pxSize+4 , y=_pxSize+4+20 };
        }

        void NewRandomMap ()
        {
            float frac = 1f / (float)_numRows;
            for( int i=0, y=0 ; y<_numRows ; y++ )
            for( int x=0 ; x<_numRows ; x++, i++ )
            {
                float fx = (float)x * frac * 4f + _offset;
                float fy = (float)y * frac * 4f + _offset;
                float noise1 = Mathf.PerlinNoise( fx , fy );
                float noise2 = math.pow( Mathf.PerlinNoise(fx*2.3f,fy*2.3f) , 3f );
                float noise3 = math.pow( Mathf.PerlinNoise(fx*14f,fy*14f) , 6f ) * (1f-noise1) * (1f-noise2);
                float noiseSum = math.pow( noise1 + noise2*0.3f + noise3*0.08f , 0.6f );
                _grid[i].style.backgroundColor = new Color{ r=noiseSum , g=noiseSum , b=noiseSum , a=1f };
            }
        }

        void SolvePath ()
        {
            //prepare data:
            var weights = new NativeArray<float>( _numRows*_numRows , Allocator.Persistent );
            for( int i=_numRows*_numRows-1 ; i!=-1 ; i-- )
            {
                weights[i] = _grid[i].style.backgroundColor.value.r;
            }

            //calculate:
			NativeList<int2> path;
            {
				#if DEBUG
				var watch = System.Diagnostics.Stopwatch.StartNew();
				#endif

				path = new NativeList<int2>( _numRows , Allocator.TempJob );
				var job = new NativeGrid.AStarJob(
					(int2)( _p1 * _numRows ) , (int2)( _p2 * _numRows ) ,
					weights , _numRows ,
					path ,
					_elasticity
				);
				job.Run();
				job.Dispose();

				#if DEBUG
				watch.Stop();
				Debug.Log($"{nameof(NativeGrid.AStarJob)} took {watch.ElapsedMilliseconds} ms");
				#endif
			}

            //visualize:
            foreach( var i2 in path )
            {
                int i = NativeGrid.BurstSafe.Index2dTo1d( i2 , _numRows );
                var style = _grid[i].style;
                Color col = style.backgroundColor.value;
                col.b = 1f;
                style.backgroundColor = col;
            }

            //dispose data:
            weights.Dispose();
            path.Dispose();
        }

    }
}
#endif
