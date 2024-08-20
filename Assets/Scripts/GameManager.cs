using System;
using System.Collections.Generic;
using ScaleSokoban;
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
        public TextAsset DemoLevel;

        public RectTransform TitleScreen;
        public LevelSelector LevelSelector;
        public static GameManager Instance;

        private int currentLevel=0;
        private GameScreen currentScreen=GameScreen.Title;
        private void Awake()
        {
            Instance = this;
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
            }else if(gameScreen==GameScreen.Puzzle){
                PuzzleManager.Instance.LoadTextLevel(Levels[currentLevel].text,LevelMode.Puzzle);
            }
            TitleScreen.gameObject.SetActive(gameScreen==GameScreen.Title);
            currentScreen=gameScreen;
        }
        public void CompleteLevel(){
            SwitchToGameScreen(GameScreen.Title);
        }
    }
}