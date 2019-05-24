using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoVsync : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 900;
        QualitySettings.vSyncCount = 0;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
