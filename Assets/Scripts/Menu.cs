using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public void StartGame()
    {
        // Load the game scene
        SceneManager.LoadScene("Game");
    }
    // Update is called once per frame
    public void ExitGame()
    {
        // Exit the game
        Application.Quit();
        Debug.Log("Game exited.");
    }
}
