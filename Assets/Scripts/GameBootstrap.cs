using Unity.NetCode;
using Unity.VisualScripting;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public class GameBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        //AutoConnectPort=7979;
        //return base.Initialize(defaultWorldName);
        return false;
    }
}
