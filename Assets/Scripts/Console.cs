using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Console
{
    private string str = "";
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Write(string segment)
    {
        str += segment;
        return;
    }

    public void Write2Console(string segment = "")
    {
        str += segment;
        Debug.Log(str);
        str = "";
        return ;
    }
}
