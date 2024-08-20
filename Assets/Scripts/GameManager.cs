using System;
using System.Collections.Generic;
using ScaleSokoban;
using UnityEngine;


namespace ScaleSokoban{
    public class GameManager : MonoBehaviour
    {
        public List<TextAsset> Levels=new List<TextAsset>();
        public TextAsset TutorialTexts;
        public TextAsset DemoLevel;

        public RectTransform TitleScreen;
        public LevelSelector LevelSelector;
        public static GameManager Instance;

        private int currentLevel=0;
        private void Awake()
        {
            Instance = this;
        }
        void Start()
        {
            PuzzleManager.Instance.LoadTextLevel(DemoLevel.text,LevelMode.Demo);
            UpdateLevelSelector();
        }

        void Update()
        {

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
    }
}