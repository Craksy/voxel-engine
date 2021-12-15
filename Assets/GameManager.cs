using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static bool gamePaused;

    public Transform pauseMenu;

    // Start is called before the first frame update
    void Start()
    {
        SetPausedState(false);
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Escape")){
            TogglePauseMenu();
        }
    }

    public void SetPausedState(bool paused){
        gamePaused = paused;
        pauseMenu.gameObject.SetActive(paused);
        Cursor.visible = paused;
    }

    public void TogglePauseMenu(){
        SetPausedState(!gamePaused);
    }

    public void ExitGame(){
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void GotoMainMenu(){
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
