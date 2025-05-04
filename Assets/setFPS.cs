using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class setFPS : MonoBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        Application.targetFrameRate = 165;
    }
    public void set75(){
        Application.targetFrameRate = 75;
    }

}
