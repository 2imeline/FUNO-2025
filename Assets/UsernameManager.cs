using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UsernameManager : MonoBehaviour
{
    public string username;
    public void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    public void setUsername(string user)
    {
        username = user;
    }

}
