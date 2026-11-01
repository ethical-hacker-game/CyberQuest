using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AppInfo 
{
    public string appID;
    public string appScenarioID;
    public List<string> stages = new List<string>();

    public AppInfo() { }
    public AppInfo(string appID, string appScenarioID)
    {
        this.appID = appID;
        this.appScenarioID = appScenarioID;
    }
}
