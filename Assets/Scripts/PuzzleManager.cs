using System.Linq;
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

        void LoadTextLevel(string level){
            var rows=level.Split("\n").Where(str=>str!="").ToList();
            height=rows.Count;
            width=rows[0].Length;
            // setup tilemap
            for(int x=0;x<width;x++){
                for(int y=0;y<height;y++){
                    Tilemap.SetTile(new Vector3Int(x,y,0),Ground);
                }
            }
            // setup camera
            var cameraCenter=Tilemap.layoutGrid.CellToLocalInterpolated(new Vector3(width/2f,height/2f,0));
            cameraCenter.z=MainCamera.transform.position.z;
            MainCamera.transform.position=cameraCenter;
        }
    }
}
