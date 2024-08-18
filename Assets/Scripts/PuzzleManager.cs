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

        Vector3Int CoordToTilemapCoord(int x,int y)=>new Vector3Int(x,-y,0);
        Vector3 CoordToTilemapCoord(float x,float y)=>new Vector3(x,-y,0);

        void LoadTextLevel(string level){
            var rows=level.Split("\n").Where(str=>str!="").ToList();
            height=rows.Count;
            width=rows[0].Length;
            // setup ground tilemap
            for(int y=0;y<height;y++){
                for(int x=0;x<width;x++){
                    var tileLocation=CoordToTilemapCoord(x,y);
                    char c=rows[y][x];
                    if(c=='#'){
                        Tilemap.SetTile(tileLocation,Wall);
                        Tilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1))*Matrix4x4.Scale(new Vector3(3,3,1)));
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
