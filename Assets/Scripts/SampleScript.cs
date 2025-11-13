using UnityEngine;

public class SampleScript : MonoBehaviour
{
    void Start()
    {
        // This uses the deprecated API that the semgrep rule replaces.
        Application.LoadLevel("Level1");
        Debug.Log("SampleScript started");
    }
}
