using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelDoneUI : MonoBehaviour
{
    [SerializeField] private TMP_Text labelTxt;
    [SerializeField] private Transform replayBtnT;
    [SerializeField] private Transform nextLevelBtnT;
    [SerializeField] private Button nextLevelBtn;
    [SerializeField] private Button replayBtn;

    public string NextLevelSceneName { get; private set; }

    public event Action OnReplayPressed;
    public event Action<string> OnNextLevelPressed;
    
    public void SetNextLevelSceneName(string lv)
    {
        NextLevelSceneName = lv;
    }
    public void SetWin()
    {
        labelTxt.text = "You win!";

        replayBtnT.gameObject.SetActive(true);

        if(!string.IsNullOrEmpty(NextLevelSceneName))
            nextLevelBtnT.gameObject.SetActive(true);
        else
            nextLevelBtnT.gameObject.SetActive(false);
    }
    public void SetLose()
    {
        labelTxt.text = "You lose!";

        replayBtnT.gameObject.SetActive(true);
        nextLevelBtnT.gameObject.SetActive(false);
    }

    public void NextLevel()
    {
        OnNextLevelPressed?.Invoke(NextLevelSceneName);
    }
    public void Replay()
    {
        OnReplayPressed?.Invoke();
    }
}
