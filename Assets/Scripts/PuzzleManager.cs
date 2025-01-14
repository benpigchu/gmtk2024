using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace ScaleSokoban{

    public enum LevelMode{
        Demo,
        Puzzle,
    }

    public abstract class PuzzleObject : MonoBehaviour
    {
        Grid grid;

        public void Setup(Grid grid){
            this.grid=grid;
        }
        public void MoveTo(float x,float y){
            var position=grid.CellToLocalInterpolated(PuzzleManager.CoordToTilemapCoord(x+0.5f,y+0.5f));
            position.z=-2;
            transform.position=position;
        }
        public void SetBig(float bigPortion){
            float scale=Mathf.Lerp(1,3,bigPortion);
            transform.localScale=new Vector3(scale,scale,1);
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

        class PuzzleTarget{
            public int x,y;
            public bool big;
        }

        enum PuzzleTriggerKind{
            Grow,
            Shrink,
        }

        public Camera MainCamera;
        public Tilemap BackgroundTilemap;
        public Tilemap NotationTilemap;
        public GameObject Player;
        public GameObject Box;

        public Tile Wall;
        public Tile Ground;
        public Tile Grow;
        public Tile Shrink;
        public Tile Target;

        public static PuzzleManager Instance;

        InputAction MoveAction;
        InputAction UndoAction;
        InputAction PauseAction;
        InputAction RestartAction;
        private void Awake()
        {
            Instance = this;
            MoveAction=InputSystem.actions.FindAction("Move");
            UndoAction=InputSystem.actions.FindAction("Undo");
            PauseAction=InputSystem.actions.FindAction("Pause");
            RestartAction=InputSystem.actions.FindAction("Restart");
        }

        void Start()
        {
        }

        // level data

        bool levelLoaded=false;

        LevelMode levelMode=LevelMode.Demo;
        int height=0;
        int width=0;

        bool[,] walls;
        PuzzleElement[,] puzzleElementColliders;

        Dictionary<PuzzleElementKind,List<PuzzleElement>> puzzleElements;
        Dictionary<PuzzleTriggerKind,List<Vector2Int>> puzzleTriggers;

        List<PuzzleTarget> puzzleTargets;

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

        void SetupCollider(PuzzleElement element){
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
        void RemoveCollider(PuzzleElement element){
            if(element.big){
                foreach(var offset in offsets3x3){
                    var colliderX=offset.x+element.x;
                    var colliderY=offset.y+element.y;
                    if(colliderX>=0&&colliderX<width&&colliderY>=0&&colliderY<height&&puzzleElementColliders[colliderY,colliderX]==element){
                        puzzleElementColliders[colliderY,colliderX]=null;
                    }
                }
            }else{
                if(puzzleElementColliders[element.y,element.x]==element){
                    puzzleElementColliders[element.y,element.x]=null;
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
            result.puzzleObject=Instantiate(GetPrefabFromKind(kind),BackgroundTilemap.layoutGrid.transform).GetComponent<PuzzleObject>();
            result.puzzleObject.Setup(BackgroundTilemap.layoutGrid);
            result.puzzleObject.MoveTo(x,y);
            result.puzzleObject.SetBig(big?1:0);
            SetupCollider(result);
            return result;
        }
        void AddPuzzleTrigger(int x,int y,PuzzleTriggerKind kind){
            if(!puzzleTriggers.ContainsKey(kind)){
                puzzleTriggers.Add(kind,new List<Vector2Int>());
            }
            puzzleTriggers[kind].Add(new Vector2Int(x,y));
        }

        private void ResetLevel()
        {
            levelLoaded=false;
            BackgroundTilemap.ClearAllTiles();
            NotationTilemap.ClearAllTiles();
            if(puzzleElements!=null){
                foreach (var puzzleElementsByKind in puzzleElements.Values)
                {
                    foreach (var puzzleElement in puzzleElementsByKind)
                    {
                        Destroy(puzzleElement.puzzleObject.gameObject);
                    }
                }
            }
            pendingAnimationData.Clear();
            currentAnimationData=null;
            currentAnimationProgress=0;
            historyStates.Clear();
            walls=null;
            puzzleElementColliders=null;
            puzzleElements=null;
            puzzleTriggers=null;
            puzzleTargets=null;
            completed=false;
            completedTimer=0;
            demoTimer=0;
        }

        public void LoadTextLevel(string level,LevelMode levelMode){
            ResetLevel();
            var rows=level.Split("\n").Where(str=>str!="").Select(str=>str.Trim()).ToList();
            height=rows.Count;
            width=rows[0].Length;
            walls=new bool[height,width];
            puzzleElementColliders=new PuzzleElement[height,width];
            puzzleElements=new Dictionary<PuzzleElementKind, List<PuzzleElement>>();
            puzzleTriggers=new Dictionary<PuzzleTriggerKind, List<Vector2Int>>();
            puzzleTargets=new List<PuzzleTarget>();
            // setup ground tilemap
            for(int y=0;y<height;y++){
                for(int x=0;x<width;x++){
                    var tileLocation=CoordToTilemapCoord(x,y);
                    char c=rows[y][x];
                    if(c=='#'){
                        BackgroundTilemap.SetTile(tileLocation,Wall);
                        BackgroundTilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1))*Matrix4x4.Scale(new Vector3(3,3,1)));
                        setupWall(x,y,true);
                    }else if(c=='*'){
                        BackgroundTilemap.SetTile(tileLocation,Wall);
                        BackgroundTilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1)));
                        setupWall(x,y,false);
                    }else if(c=='X'){
                        BackgroundTilemap.SetTile(tileLocation,Grow);
                        BackgroundTilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1))*Matrix4x4.Scale(new Vector3(3,3,1)));
                        AddPuzzleTrigger(x,y,PuzzleTriggerKind.Grow);
                    }else if(c=='x'){
                        BackgroundTilemap.SetTile(tileLocation,Shrink);
                        BackgroundTilemap.SetTransformMatrix(tileLocation,Matrix4x4.Translate(new Vector3(0,0,-1))*Matrix4x4.Scale(new Vector3(3,3,1)));
                        AddPuzzleTrigger(x,y,PuzzleTriggerKind.Shrink);
                    }else if(c=='O'){
                        BackgroundTilemap.SetTile(tileLocation,Ground);
                        NotationTilemap.SetTile(tileLocation,Target);
                        NotationTilemap.SetTransformMatrix(tileLocation,Matrix4x4.Scale(new Vector3(3,3,1)));
                        puzzleTargets.Add(new PuzzleTarget{
                            x=x,
                            y=y,
                            big=true,
                        });
                    }else if(c=='o'){
                        BackgroundTilemap.SetTile(tileLocation,Ground);
                        NotationTilemap.SetTile(tileLocation,Target);
                        puzzleTargets.Add(new PuzzleTarget{
                            x=x,
                            y=y,
                            big=false,
                        });
                    }else if(c=='P'){
                        BackgroundTilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,true,PuzzleElementKind.Player);
                    }else if(c=='p'){
                        BackgroundTilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,false,PuzzleElementKind.Player);
                    }else if(c=='B'){
                        BackgroundTilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,true,PuzzleElementKind.Box);
                    }else if(c=='b'){
                        BackgroundTilemap.SetTile(tileLocation,Ground);
                        AddPuzzleElement(x,y,false,PuzzleElementKind.Box);
                    }else{
                        BackgroundTilemap.SetTile(tileLocation,Ground);
                    }
                }
            }
            // setup camera
            var cameraCenter=BackgroundTilemap.layoutGrid.CellToLocalInterpolated(CoordToTilemapCoord(width/2f,height/2f));
            cameraCenter.z=MainCamera.transform.position.z;
            MainCamera.transform.position=cameraCenter;
            levelLoaded=true;
            this.levelMode=levelMode;
        }

        //movement animation
        interface IAnimationData{
            public void Seek(float portion){}
            public void Start(){}
        }
        struct MoveAnimationData:IAnimationData{
            public PuzzleObject PuzzleObject;
            public float StartX,StartY,EndX,EndY;

            public void Seek(float portion)
            {
                PuzzleObject.MoveTo(Mathf.Lerp(StartX,EndX,portion),Mathf.Lerp(StartY,EndY,portion));
            }
        }
        struct ScaleAnimationData:IAnimationData{
            public PuzzleObject PuzzleObject;
            public float StartBig,EndBig;

            public void Seek(float portion)
            {
                PuzzleObject.SetBig(Mathf.Lerp(StartBig,EndBig,portion));
            }
        }
        struct SfxAnimationData:IAnimationData{
            public AudioClip clip;
            public void Start(){
                AudioManager.Instance.PlaySfx(clip);
            }
        }
        Queue<List<IAnimationData>> pendingAnimationData=new Queue<List<IAnimationData>>();
        List<IAnimationData> currentAnimationData=null;

        float currentAnimationProgress=0;

        public float AnimationStepLength=0.2f;

        void Update()
        {
            if(!levelLoaded){
                return;
            }
            if(completed){
                completedTimer+=Time.deltaTime;
                if(completedTimer>CompleteTimeLength){
                    GameManager.Instance.CompleteLevel();
                }
                return;
            }
            var inputBlocked=GameManager.Instance.BlockPuzzleInput||levelMode!=LevelMode.Puzzle;
            if(!inputBlocked&&UndoAction.WasPressedThisFrame()){
                Undo();
                return;
            }
            if(!inputBlocked&&PauseAction.WasPressedThisFrame()){
                GameManager.Instance.Pause();
            }
            if(!inputBlocked&&RestartAction.WasPressedThisFrame()){
                GameManager.Instance.Restart();
                return;
            }
            if(UpdateAnimation()){
                return;
            }
            if(levelMode==LevelMode.Demo){
                ProcessDemo();
            }
            if(!inputBlocked&&MoveAction.WasPressedThisFrame()){
                var moveDirection=MoveAction.ReadValue<Vector2>();
                var x=Math.Sign(Mathf.RoundToInt(moveDirection.x));
                var y=-Math.Sign(Mathf.RoundToInt(moveDirection.y));
                if(x!=0){
                    y=0;
                }
                ProcessMovement(x,y);
            }
        }

        float demoTimer=0;
        public float DemoTimerLength=0.5f;

        private void ProcessDemo()
        {
            demoTimer+=Time.deltaTime;
            if(demoTimer>DemoTimerLength){
                demoTimer=0;
                var player=puzzleElements[PuzzleElementKind.Player][0];
                if(player.big){
                    ProcessMovement(1,0);
                }else{
                    ProcessMovement(-1,0);
                }
            }
        }

        private bool UpdateAnimation()
        {
            if(currentAnimationData==null){
                if(pendingAnimationData.Count>0){
                    currentAnimationData=pendingAnimationData.Dequeue();
                    foreach (var animation in currentAnimationData)
                    {
                        animation.Start();
                    }
                    currentAnimationProgress=0;
                }else{
                    return false;
                }
            }
            currentAnimationProgress+=Time.deltaTime/AnimationStepLength;
            while(currentAnimationProgress>=1){
                currentAnimationProgress-=1;
                foreach (var animation in currentAnimationData)
                {
                    animation.Seek(1);
                }
                if(pendingAnimationData.Count>0){
                    currentAnimationData=pendingAnimationData.Dequeue();
                    foreach (var animation in currentAnimationData)
                    {
                        animation.Start();
                    }
                }else{
                    currentAnimationData=null;
                    break;
                }
            }
            if(currentAnimationData!=null){
                foreach (var animation in currentAnimationData)
                {
                    animation.Seek(currentAnimationProgress);
                }
            }else{
                CheckPuzzleComplete();
            }
            return true;
        }

        void ResetAnimation(){
            pendingAnimationData.Clear();
            currentAnimationData=null;
            currentAnimationProgress=0;
        }

        void ApplyPuzzleObjectStates(){
            foreach (var puzzleElementsByKind in puzzleElements.Values)
            {
                foreach (var puzzleElement in puzzleElementsByKind)
                {
                    puzzleElement.puzzleObject.MoveTo(puzzleElement.x,puzzleElement.y);
                    puzzleElement.puzzleObject.SetBig(puzzleElement.big?1:0);
                }
            }
        }

        void ProcessMovement(int directionX,int directionY){
            CaptureHistory();
            bool noMovement=true;
            if(puzzleElements.ContainsKey(PuzzleElementKind.Player)){
                foreach(var player in puzzleElements[PuzzleElementKind.Player]){
                    bool playerMovedByTrigger=false;
                    int moveStep=1;
                    if(player.big){
                        moveStep=3;
                    }
                    for(int i=0;i<moveStep;i++){
                        var pushingPuzzleElements=GetPushingPuzzleElements(player,directionX,directionY,!player.big);
                        if(pushingPuzzleElements==null){
                            break;
                        }
                        noMovement=false;
                        var animationData=new List<IAnimationData>();
                        MovePuzzleElements(pushingPuzzleElements,directionX,directionY,animationData);
                        if(levelMode==LevelMode.Puzzle){
                            animationData.Add(new SfxAnimationData{clip=AudioManager.Instance.Move});
                        }
                        pendingAnimationData.Enqueue(animationData);
                        while(true){
                            var animationBeforeTrigger=pendingAnimationData.Count;
                            var playerMoved=CheckPuzzleTriggers(player);
                            playerMovedByTrigger=playerMovedByTrigger||playerMoved;
                            if(pendingAnimationData.Count<=animationBeforeTrigger){
                                break;
                            }
                        }
                        if(playerMovedByTrigger){
                            break;
                        }
                    }
                }
            }
            if(noMovement){
                historyStates.Pop();
            }
        }

        void MovePuzzleElements(HashSet<PuzzleElement> puzzleElements,int directionX,int directionY,List<IAnimationData> animationData){
            foreach (var puzzleElement in puzzleElements)
            {
                RemoveCollider(puzzleElement);
            }
            foreach (var puzzleElement in puzzleElements)
            {
                animationData.Add(new MoveAnimationData{
                    PuzzleObject=puzzleElement.puzzleObject,
                    StartX=puzzleElement.x,
                    StartY=puzzleElement.y,
                    EndX=puzzleElement.x+directionX,
                    EndY=puzzleElement.y+directionY,
                });
                puzzleElement.x+=directionX;
                puzzleElement.y+=directionY;
            }
            foreach (var puzzleElement in puzzleElements)
            {
                SetupCollider(puzzleElement);
            }
        }

        HashSet<PuzzleElement> GetPushingPuzzleElements(PuzzleElement puzzleElement,int directionX,int directionY,bool limitPushPower=false){
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
                            if(limitPushPower&&blocker.big){
                                return null;
                            }
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

        bool CheckPuzzleTriggers(PuzzleElement initializer){
            bool initializerMoved=false;
            if(puzzleTriggers.ContainsKey(PuzzleTriggerKind.Shrink)){
                foreach (var shrink in puzzleTriggers[PuzzleTriggerKind.Shrink])
                {
                    var puzzleElement=puzzleElementColliders[shrink.y,shrink.x];
                    if(puzzleElement==null){
                        continue;
                    }
                    if(puzzleElement.big&&puzzleElement.x==shrink.x&&puzzleElement.y==shrink.y){
                        var animationData=new List<IAnimationData>();
                        if(puzzleElement==initializer){
                            initializerMoved=true;
                        }
                        animationData.Add(new ScaleAnimationData{
                            PuzzleObject=puzzleElement.puzzleObject,
                            StartBig=1,
                            EndBig=0,
                        });
                        RemoveCollider(puzzleElement);
                        puzzleElement.big=false;
                        SetupCollider(puzzleElement);
                        if(levelMode==LevelMode.Puzzle){
                            animationData.Add(new SfxAnimationData{clip=AudioManager.Instance.Scale});
                        }
                        pendingAnimationData.Enqueue(animationData);
                    }
                }
            }
            if(puzzleTriggers.ContainsKey(PuzzleTriggerKind.Grow)){
                foreach (var grow in puzzleTriggers[PuzzleTriggerKind.Grow])
                {
                    if(!(grow.x>=0&&grow.x<width&&grow.y>=0&&grow.y<height)){
                        continue;
                    }
                    var puzzleElement=puzzleElementColliders[grow.y,grow.x];
                    if(puzzleElement==null){
                        continue;
                    }
                    if(!puzzleElement.big){
                        bool growable=true;
                        var left=puzzleElementColliders[grow.y,grow.x-1];
                        var right=puzzleElementColliders[grow.y,grow.x+1];
                        var up=puzzleElementColliders[grow.y-1,grow.x];
                        var down=puzzleElementColliders[grow.y+1,grow.x];
                        var leftUp=puzzleElementColliders[grow.y-1,grow.x-1];
                        var leftDown=puzzleElementColliders[grow.y+1,grow.x-1];
                        var rightUp=puzzleElementColliders[grow.y-1,grow.x+1];
                        var rightDown=puzzleElementColliders[grow.y+1,grow.x+1];
                        var pushLeft=new HashSet<PuzzleElement>();
                        var pushRight=new HashSet<PuzzleElement>();
                        var pushUp=new HashSet<PuzzleElement>();
                        var pushDown=new HashSet<PuzzleElement>();
                        if(left!=null){
                            var result=GetPushingPuzzleElements(left,-1,0);
                            if(result==null){
                                growable=false;
                            }else{
                                pushLeft.UnionWith(result);
                            }
                        }
                        if(right!=null){
                            var result=GetPushingPuzzleElements(right,1,0);
                            if(result==null){
                                growable=false;
                            }else{
                                pushRight.UnionWith(result);
                            }
                        }
                        if(up!=null){
                            var result=GetPushingPuzzleElements(up,0,-1);
                            if(result==null){
                                growable=false;
                            }else{
                                pushUp.UnionWith(result);
                            }
                        }
                        if(down!=null){
                            var result=GetPushingPuzzleElements(down,0,1);
                            if(result==null){
                                growable=false;
                            }else{
                                pushDown.UnionWith(result);
                            }
                        }
                        // for conner box we always push left/right

                        if(leftUp!=null&&leftUp!=left&&leftUp!=up){
                            var result=GetPushingPuzzleElements(leftUp,-1,0);
                            if(result==null){
                                growable=false;
                            }else{
                                pushLeft.UnionWith(result);
                            }
                        }
                        if(leftDown!=null&&leftDown!=left&&leftDown!=down){
                            var result=GetPushingPuzzleElements(leftDown,-1,0);
                            if(result==null){
                                growable=false;
                            }else{
                                pushLeft.UnionWith(result);
                            }
                        }
                        if(rightUp!=null&&rightUp!=right&&rightUp!=up){
                            var result=GetPushingPuzzleElements(rightUp,1,0);
                            if(result==null){
                                growable=false;
                            }else{
                                pushLeft.UnionWith(result);
                            }
                        }
                        if(rightDown!=null&&rightDown!=right&&rightDown!=down){
                            var result=GetPushingPuzzleElements(rightDown,1,0);
                            if(result==null){
                                growable=false;
                            }else{
                                pushRight.UnionWith(result);
                            }
                        }

                        if(!growable){
                            continue;
                        }
                        var animationData=new List<IAnimationData>();
                        if(pushLeft.Contains(initializer)){
                            initializerMoved=true;
                        }
                        if(pushRight.Contains(initializer)){
                            initializerMoved=true;
                        }
                        if(pushUp.Contains(initializer)){
                            initializerMoved=true;
                        }
                        if(pushDown.Contains(initializer)){
                            initializerMoved=true;
                        }
                        if(puzzleElement==initializer){
                            initializerMoved=true;
                        }
                        MovePuzzleElements(pushLeft,-1,0,animationData);
                        MovePuzzleElements(pushRight,1,0,animationData);
                        MovePuzzleElements(pushUp,0,-1,animationData);
                        MovePuzzleElements(pushDown,0,1,animationData);

                        animationData.Add(new ScaleAnimationData{
                            PuzzleObject=puzzleElement.puzzleObject,
                            StartBig=0,
                            EndBig=1,
                        });
                        RemoveCollider(puzzleElement);
                        puzzleElement.big=true;
                        SetupCollider(puzzleElement);
                        if(levelMode==LevelMode.Puzzle){
                            animationData.Add(new SfxAnimationData{clip=AudioManager.Instance.Scale});
                        }
                        pendingAnimationData.Enqueue(animationData);
                    }
                }
            }
            return initializerMoved;
        }

        //undo
        interface IHistoryStateEntry{
            public void Recover();
        }
        struct PuzzleElementState : IHistoryStateEntry
        {
            public bool big;
            public int x;
            public int y;
            public PuzzleElement puzzleElement;
            public void Recover()
            {
                puzzleElement.big=big;
                puzzleElement.x=x;
                puzzleElement.y=y;
            }
        }

        Stack<List<IHistoryStateEntry>> historyStates=new Stack<List<IHistoryStateEntry>>();

        void CaptureHistory(){
            var entries=new List<IHistoryStateEntry>();
            foreach (var puzzleElementsByKind in puzzleElements.Values)
            {
                foreach (var puzzleElement in puzzleElementsByKind)
                {
                    entries.Add(new PuzzleElementState{
                        puzzleElement=puzzleElement,
                        big=puzzleElement.big,
                        x=puzzleElement.x,
                        y=puzzleElement.y,
                    });
                }
            }
            historyStates.Push(entries);
        }
        void Undo(){
            ResetAnimation();
            if(historyStates.Count>0){
                var entries=historyStates.Pop();
                foreach (var puzzleElementsByKind in puzzleElements.Values)
                {
                    foreach (var puzzleElement in puzzleElementsByKind)
                    {
                        RemoveCollider(puzzleElement);
                    }
                }
                foreach (var entry in entries)
                {
                    entry.Recover();
                }
                foreach (var puzzleElementsByKind in puzzleElements.Values)
                {
                    foreach (var puzzleElement in puzzleElementsByKind)
                    {
                        SetupCollider(puzzleElement);
                    }
                }
            }
            ApplyPuzzleObjectStates();
        }

        //level complete check

        bool completed=false;
        float completedTimer=0;
        public float CompleteTimeLength=1f;
        void CheckPuzzleComplete(){
            if(levelMode!=LevelMode.Puzzle){
                return;
            }
            foreach (var target in puzzleTargets)
            {
                var puzzleElement=puzzleElementColliders[target.y,target.x];
                if(puzzleElement==null){
                    return;
                }
                if(puzzleElement.kind!=PuzzleElementKind.Box){
                    return;
                }
                if(puzzleElement.x!=target.x||puzzleElement.y!=target.y||puzzleElement.big!=target.big){
                    return;
                }
            }
            completed=true;
            completedTimer=0;
            AudioManager.Instance.PlaySfx(AudioManager.Instance.Complete);
        }
    }

}
