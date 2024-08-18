using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ScaleSokoban{
    public class PuzzleManager : MonoBehaviour
    {

        public Camera MainCamera;
        public Tilemap Tilemap;

        public Tile Wall;
        public Tile Ground;

        public TextAsset InitLevel;
        PuzzleManager Instance;
        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            LoadTextLevel(InitLevel.text);
        }

        void Update()
        {

        }

        // level data
        int height=0;
        int width=0;

        bool[,] walls;

        static Vector2Int[] offsets3x3=new Vector2Int[]{
            new Vector2Int(-1,-1),
            new Vector2Int(0,-1),
            new Vector2Int(1,-1),
            new Vector2Int(-1,0),
            new Vector2Int(0,0),
            new Vector2Int(1,0),
            new Vector2Int(-1,1),
            new Vector2Int(0,1),
            new Vector2Int(1,1),
        };
        Vector3Int CoordToTilemapCoord(int x,int y)=>new Vector3Int(x,-y,0);
        Vector3 CoordToTilemapCoord(float x,float y)=>new Vector3(x,-y,0);

        void setup3x3wall(int x,int y){
            foreach(var offset in offsets3x3){
                var wallX=offset.x+x;
                var wallY=offset.y+y;
                if(wallX>=0&&wallX<width&&wallY>=0&&wallY<height){
                    walls[wallY,wallX]=true;
                }
            }
        }

        void LoadTextLevel(string level){
            var rows=level.Split("\n").Where(str=>str!="").ToList();
            height=rows.Count;
            width=rows[0].Length;
            walls=new bool[height,width];
            // setup ground tilemap
            for(int y=0;y<height;y++){
                for(int x=0;x<width;x++){
                    var tileLocation=CoordToTilemapCoord(x,y);
                    char c=rows[y][x];
                    if(c=='#'){
                        Tilemap.SetTile(tileLocation,Wall);
                        Tilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1))*Matrix4x4.Scale(new Vector3(3,3,1)));
                        setup3x3wall(x,y);
                    }else if(c=='*'){
                        Tilemap.SetTile(tileLocation,Wall);
                        Tilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1)));
                        walls[y,x]=true;
                    }else{
                        Tilemap.SetTile(tileLocation,Ground);
                    }
                }
            }
            // setup camera
            var cameraCenter=Tilemap.layoutGrid.CellToLocalInterpolated(CoordToTilemapCoord(width/2f,height/2f));
            cameraCenter.z=MainCamera.transform.position.z;
            MainCamera.transform.position=cameraCenter;
        }
    }
}
