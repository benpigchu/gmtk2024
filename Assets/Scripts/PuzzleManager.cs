using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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
            Box,
        }

        class PuzzleElement{
            public int x,y;
            public bool big;
            public PuzzleElementKind kind;
            public PuzzleObject puzzleObject;
        }

        public Camera MainCamera;
        public Tilemap Tilemap;
        public GameObject Player;
        public GameObject Box;

        public Tile Wall;
        public Tile Ground;

        public TextAsset InitLevel;
        PuzzleManager Instance;

        InputAction Move;
        private void Awake()
        {
            Instance = this;
            Move=InputSystem.actions.FindAction("Move");
        }

        void Start()
        {
            LoadTextLevel(InitLevel.text);
        }

        // level data
        int height=0;
        int width=0;

        bool[,] walls;
        PuzzleElement[,] puzzleElementColliders;

        Dictionary<PuzzleElementKind,List<PuzzleElement>> puzzleElements;

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
        public static Vector3 CoordToTilemapCoord(float x,float y)=>new Vector3(x,-y+1,0);

        void setupWall(int x,int y,bool big){
            if(big){
                foreach(var offset in offsets3x3){
                    var wallX=offset.x+x;
                    var wallY=offset.y+y;
                    if(wallX>=0&&wallX<width&&wallY>=0&&wallY<height){
                        walls[wallY,wallX]=true;
                    }
                }
            }else{
                walls[y,x]=true;
            }
        }

        void setupCollider(PuzzleElement element){
            if(element.big){
                foreach(var offset in offsets3x3){
                    var colliderX=offset.x+element.x;
                    var colliderY=offset.y+element.y;
                    if(colliderX>=0&&colliderX<width&&colliderY>=0&&colliderY<height){
                        puzzleElementColliders[colliderY,colliderX]=element;
                    }
                }
            }else{
                puzzleElementColliders[element.y,element.x]=element;
            }
        }
        void removeCollider(PuzzleElement element){
            if(element.big){
                foreach(var offset in offsets3x3){
                    var colliderX=offset.x+element.x;
                    var colliderY=offset.y+element.y;
                    if(colliderX>=0&&colliderX<width&&colliderY>=0&&colliderY<height&&puzzleElementColliders[colliderY,colliderX]==element){
                        puzzleElementColliders[colliderY,colliderX]=null;
                    }
                }
            }else{
                if(puzzleElementColliders[element.x,element.y]==element){
                    puzzleElementColliders[element.x,element.y]=element;
                }
            }
        }

        GameObject GetPrefabFromKind(PuzzleElementKind kind){
            if(kind==PuzzleElementKind.Player){
                return Player;
            }else if(kind==PuzzleElementKind.Box){
                return Box;
            }
            Debug.LogError($"No prefab for {kind}");
            return null;
        }

        PuzzleElement AddPuzzleElement(int x,int y,bool big,PuzzleElementKind kind){
            PuzzleElement result=new PuzzleElement{
                x=x,
                y=y,
                big=big,
                kind=kind,
            };
            if(!puzzleElements.ContainsKey(kind)){
                puzzleElements.Add(kind,new List<PuzzleElement>());
            }
            puzzleElements[kind].Add(result);
            result.puzzleObject=Instantiate(GetPrefabFromKind(kind),Tilemap.layoutGrid.transform).GetComponent<PuzzleObject>();
            result.puzzleObject.Setup(Tilemap.layoutGrid);
            result.puzzleObject.MoveTo(x,y);
            result.puzzleObject.SetBig(big);
            setupCollider(result);
            return result;
        }

        void LoadTextLevel(string level){
            var rows=level.Split("\n").Where(str=>str!="").Select(str=>str.Trim()).ToList();
            height=rows.Count;
            width=rows[0].Length;
            walls=new bool[height,width];
            puzzleElementColliders=new PuzzleElement[height,width];
            puzzleElements=new Dictionary<PuzzleElementKind, List<PuzzleElement>>();
            // setup ground tilemap
            for(int y=0;y<height;y++){
                for(int x=0;x<width;x++){
                    var tileLocation=CoordToTilemapCoord(x,y);
                    char c=rows[y][x];
                    if(c=='#'){
                        Tilemap.SetTile(tileLocation,Wall);
                        Tilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1))*Matrix4x4.Scale(new Vector3(3,3,1)));
                        setupWall(x,y,true);
                    }else if(c=='*'){
                        Tilemap.SetTile(tileLocation,Wall);
                        Tilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1)));
                        setupWall(x,y,false);
                    }else if(c=='P'){
                        Tilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,true,PuzzleElementKind.Player);
                    }else if(c=='p'){
                        Tilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,false,PuzzleElementKind.Player);
                    }else if(c=='B'){
                        Tilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,true,PuzzleElementKind.Box);
                    }else if(c=='b'){
                        Tilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,false,PuzzleElementKind.Box);
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
        void Update()
        {
            if(Move.WasPressedThisFrame()){
                var moveDirection=Move.ReadValue<Vector2>();
                ProcessMovement(Mathf.RoundToInt(moveDirection.x),-Mathf.RoundToInt(moveDirection.y));
            }
        }

        void ProcessMovement(int directionX,int directionY){
            foreach(var player in puzzleElements[PuzzleElementKind.Player]){
                int moveStep=1;
                if(player.big){
                    moveStep=3;
                }
                for(int i=0;i<moveStep;i++){
                    var pushingPuzzleElements=GetPushingPuzzleElements(player,directionX,directionY);
                    if(pushingPuzzleElements==null){
                        break;
                    }
                    MovePuzzleElements(pushingPuzzleElements,directionX,directionY);
                }
            }
        }

        void MovePuzzleElements(HashSet<PuzzleElement> puzzleElements,int directionX,int directionY){
            foreach (var puzzleElement in puzzleElements)
            {
                removeCollider(puzzleElement);
            }
            foreach (var puzzleElement in puzzleElements)
            {
                puzzleElement.x+=directionX;
                puzzleElement.y+=directionY;
                puzzleElement.puzzleObject.MoveTo(puzzleElement.x,puzzleElement.y);
            }
            foreach (var puzzleElement in puzzleElements)
            {
                setupCollider(puzzleElement);
            }
        }

        HashSet<PuzzleElement> GetPushingPuzzleElements(PuzzleElement puzzleElement,int directionX,int directionY){
            var movingPuzzleElements=new HashSet<PuzzleElement>();
            var pendingPuzzleElements=new Queue<PuzzleElement>();
            pendingPuzzleElements.Enqueue(puzzleElement);
            movingPuzzleElements.Add(puzzleElement);
            while(pendingPuzzleElements.Count>0){
                var current=pendingPuzzleElements.Dequeue();
                var targets=new List<Vector2Int>();
                if(current.big){
                    targets.Add(new Vector2Int(current.x+2*directionX+directionY,current.y+2*directionY-directionX));
                    targets.Add(new Vector2Int(current.x+2*directionX,current.y+2*directionY));
                    targets.Add(new Vector2Int(current.x+2*directionX-directionY,current.y+2*directionY+directionX));
                }else{
                    targets.Add(new Vector2Int(current.x+directionX,current.y+directionY));
                }
                foreach(var target in targets){
                    if(!(target.x>=0&&target.x<width&&target.y>=0&&target.y<height)){
                        return null;
                    }else if(walls[target.y,target.x]){
                        return null;
                    }else{
                        var blocker=puzzleElementColliders[target.y,target.x];
                        if(blocker!=null){
                            if(!movingPuzzleElements.Contains(blocker)){
                                pendingPuzzleElements.Enqueue(blocker);
                                movingPuzzleElements.Add(blocker);
                            }
                        }
                    }
                }
            }
            return movingPuzzleElements;
        }
    }

}
