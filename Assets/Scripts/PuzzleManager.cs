using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ScaleSokoban{
    public abstract class PuzzleObject : MonoBehaviour
    {
        Grid grid;

        public void Setup(Grid grid){
            this.grid=grid;
        }
        public void MoveTo(int x,int y){
            var position=grid.CellToLocalInterpolated(PuzzleManager.CoordToTilemapCoord(x+0.5f,y+0.5f));
            position.z=-2;
            transform.position=position;
        }
        public void SetBig(bool big){
            transform.localScale=big?new Vector3(3,3,1):new Vector3(1,1,1);
        }
    }
    public class PuzzleManager : MonoBehaviour
    {

        enum PuzzleElementKind{
            Player,
        }

        struct PuzzleElement{
            public int x,y;
            public bool big;
            public PuzzleElementKind kind;
            public PuzzleObject puzzleObject;
        }

        public Camera MainCamera;
        public Tilemap Tilemap;
        public GameObject player;

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

        List<PuzzleElement> puzzleElements;

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
        public static Vector3Int CoordToTilemapCoord(int x,int y)=>new Vector3Int(x,-y,0);
        public static Vector3 CoordToTilemapCoord(float x,float y)=>new Vector3(x,-y,0);

        void setup3x3wall(int x,int y){
            foreach(var offset in offsets3x3){
                var wallX=offset.x+x;
                var wallY=offset.y+y;
                if(wallX>=0&&wallX<width&&wallY>=0&&wallY<height){
                    walls[wallY,wallX]=true;
                }
            }
        }

        GameObject GetPrefabFromKind(PuzzleElementKind kind){
            if(kind==PuzzleElementKind.Player){
                return player;
            }
            return null;
        }

        PuzzleElement AddPuzzleElement(int x,int y,bool big,PuzzleElementKind kind){
            PuzzleElement result=new PuzzleElement{
                x=x,
                y=y,
                big=big,
                kind=kind,
            };
            puzzleElements.Add(result);
            result.puzzleObject=Instantiate(GetPrefabFromKind(kind),Tilemap.layoutGrid.transform).GetComponent<PuzzleObject>();
            result.puzzleObject.Setup(Tilemap.layoutGrid);
            result.puzzleObject.MoveTo(x,y);
            result.puzzleObject.SetBig(big);
            return result;
        }

        void LoadTextLevel(string level){
            var rows=level.Split("\n").Where(str=>str!="").ToList();
            height=rows.Count;
            width=rows[0].Length;
            walls=new bool[height,width];
            puzzleElements=new List<PuzzleElement>();
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
                    }else if(c=='P'){
                        Tilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,true,PuzzleElementKind.Player);
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
