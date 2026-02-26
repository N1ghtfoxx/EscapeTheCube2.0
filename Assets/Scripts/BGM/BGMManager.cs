using UnityEngine;

public class BGMManager : MonoBehaviour
{
     private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
