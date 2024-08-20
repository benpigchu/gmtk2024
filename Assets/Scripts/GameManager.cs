using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


namespace ScaleSokoban{
    public class GameManager : MonoBehaviour
    {
        enum GameScreen{
            Title,
            Puzzle,
        }
        public List<TextAsset> Levels=new List<TextAsset>();
        public TextAsset TutorialTexts;

        List<string> parsedTutorialTexts;
        public TextAsset DemoLevel;

        public RectTransform TitleScreen;
        public RectTransform PauseMenu;
        public RectTransform LevelIntro;
        public LevelSelector LevelSelector;
        public TMP_Text PauseLevelText;
        public TMP_Text IntroLevelText;
        public TMP_Text IntroTutorialText;
        public static GameManager Instance;

        private bool paused=false;
        private bool levelIntroVisible=false;

        public bool BlockPuzzleInput{get=>paused||levelIntroVisible;}

        private int currentLevel=0;
        private GameScreen currentScreen=GameScreen.Title;
        private void Awake()
        {
            Instance = this;
            parsedTutorialTexts=TutorialTexts.text.Split("\n").Select(str=>str.Trim()).ToList();
        }
        void Start()
        {
            SwitchToGameScreen(GameScreen.Title);
        }

        public void UpdateLevelSelector(){
            LevelSelector.UpdateText(currentLevel,currentLevel<=0,currentLevel>=Levels.Count-1);
        }

        public void SelectLevel(int offset)
        {
            int targetLevel=currentLevel+offset;
            if(targetLevel>=0&&targetLevel<Levels.Count){
                currentLevel=targetLevel;
                UpdateLevelSelector();
            }
        }

        public void EnterLevel(){
            SwitchToGameScreen(GameScreen.Puzzle);
        }

        void SwitchToGameScreen(GameScreen gameScreen){
            UpdateLevelSelector();
            if(gameScreen==GameScreen.Title){
                PuzzleManager.Instance.LoadTextLevel(DemoLevel.text,LevelMode.Demo);
                LevelIntro.gameObject.SetActive(false);
                levelIntroVisible=false;
            }else if(gameScreen==GameScreen.Puzzle){
                PuzzleManager.Instance.LoadTextLevel(Levels[currentLevel].text,LevelMode.Puzzle);
                SetupLevelIntro();
            }
            TitleScreen.gameObject.SetActive(gameScreen==GameScreen.Title);
            paused=false;
            PauseMenu.gameObject.SetActive(false);
            currentScreen=gameScreen;
        }

        void SetupLevelIntro(){
            levelIntroVisible=true;
            IntroLevelText.text=$"level {currentLevel+1:00}";
            if(currentLevel>=parsedTutorialTexts.Count){
                IntroTutorialText.text="";
            }else{
                IntroTutorialText.text=parsedTutorialTexts[currentLevel];
            }
            LevelIntro.gameObject.SetActive(true);
        }

        public void CompleteLevel(){
            if(currentLevel+1<Levels.Count){
                currentLevel+=1;
                SwitchToGameScreen(GameScreen.Puzzle);
            }else{
                SwitchToGameScreen(GameScreen.Title);
            }
        }

        public void Pause(){
            paused=true;
            PauseMenu.gameObject.SetActive(true);
            PauseLevelText.text=$"level {currentLevel+1:00}";
        }

        public void Resume(){
            paused=false;
            PauseMenu.gameObject.SetActive(false);
        }
        public void Restart(){
            SwitchToGameScreen(GameScreen.Puzzle);
        }
        public void ExitToMenu(){
            SwitchToGameScreen(GameScreen.Title);
        }
        public void FinishLevelIntro(){
            LevelIntro.gameObject.SetActive(false);
            levelIntroVisible=false;
        }
    }
}