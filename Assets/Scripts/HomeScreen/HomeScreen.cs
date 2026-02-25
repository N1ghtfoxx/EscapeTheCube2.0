using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeScreen : MonoBehaviour
{
    
    public void StartGame()
    {
        SceneManager.LoadScene("CharacterSelection");
    }
    
}
