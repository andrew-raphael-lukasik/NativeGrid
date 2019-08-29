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
        int _resolution = 128;
        float2 _p1 = new float2{ x=0.1f , y=0.1f };
        float2 _p2 = new float2{ x=0.9f , y=0.9f };
        [SerializeField] float _elasticity = 1f;
        [SerializeField] float _offset = 0f;

        VisualElement[] _grid;

        public void OnEnable ()
        {
            var ROOT = rootVisualElement;
            var GRID = new VisualElement();

            var RESOLUTION = new IntegerField( "Resolution:" );
            RESOLUTION.value = _resolution;
            RESOLUTION.RegisterValueChangedCallback( (e) => {
                _resolution = math.clamp( e.newValue , 1 , 256 );
                if( RESOLUTION.value!=_resolution )  RESOLUTION.value = _resolution;
                GRID.Clear();
                CreateGridLayout( GRID );
                NewRandomMap();
            } );
            ROOT.Add( RESOLUTION );

            // ELASTICITY:
            var ELASTICITY = new Slider( $"Elasticity:" , 0f , 1f );
            ELASTICITY.style.paddingLeft = ELASTICITY.style.paddingRight = 10;
            ELASTICITY.value = _elasticity;
            ELASTICITY.RegisterValueChangedCallback( (e)=> {
                _elasticity = e.newValue;
                NewRandomMap();
                SolvePath();
            } );
            ROOT.Add( ELASTICITY );

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
            } );
            ROOT.Add( GRID );
            CreateGridLayout( GRID );

            NewRandomMap();
            SolvePath();
        }

        [MenuItem("Test/NativeGrid/Pathfinding")]
        static void ShowWindow ()
        {
            var window = GetWindow<Test_NativeGrid_pathfinding>();
            window.titleContent = new GUIContent("Test NativeGrid pathfinding");
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
                float noiseSum = math.pow( noise1 + noise2*0.3f + noise3*0.08f , 0.6f );
                _grid[i].style.backgroundColor = new Color{ r=noiseSum , g=noiseSum , b=noiseSum , a=1f };
            }
        }

        void SolvePath ()
        {
            //prepare data:
            NativeArray<float> weights;
            {
                int len = _resolution*_resolution;
                weights = new NativeArray<float>( len , Allocator.Persistent );
                float[] arr = new float[len];//NativeArray enumeration is slow outside Burst
                for( int i=len-1 ; i!=-1 ; i-- )
                {
                    arr[i] = _grid[i].style.backgroundColor.value.r;
                }
                weights.CopyFrom( arr );
            }

            //calculate:
            NativeList<int2> path;
            {
                #if DEBUG
                var watch = System.Diagnostics.Stopwatch.StartNew();
                #endif

                path = new NativeList<int2>( _resolution , Allocator.TempJob );
                var job = new NativeGrid.AStarJob(
                    (int2)( _p1 * _resolution ) , (int2)( _p2 * _resolution ) ,
                    weights , _resolution ,
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
                int i = NativeGrid.BurstSafe.Index2dTo1d( i2 , _resolution );
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
