using System.Collections.Generic;
using ScaleSokoban;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<TextAsset> Levels=new List<TextAsset>();
    public TextAsset TutorialTexts;
    public TextAsset DemoLevel;
    public static GameManager Instance;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        PuzzleManager.Instance.LoadTextLevel(DemoLevel.text,LevelMode.Demo);
    }

    void Update()
    {

    }
}
