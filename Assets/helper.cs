using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class helper : NetworkBehaviour
{
    public static helper Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // optional: protect singleton
            return;
        }
        Instance = this;
    }

    [ObserversRpc]
    public void logToEveryone(string l)
    {
        Debug.Log(l);
    }

    [ServerRpc(RequireOwnership = false)]
    public void requestSetParent(NetworkObject child, NetworkObject parent = null)
    {
        setParent(child, parent);
    }

    [ObserversRpc]
    public void setParent(NetworkObject child, NetworkObject parent)
    {
        if(parent == null)
            child.transform.parent = null;
        else
            child.transform.parent = parent.transform;
    }

    [ObserversRpc]
    public void moveObject(NetworkObject obj, Vector3 where, bool local = true)
    {
        if(local)
            obj.transform.localPosition = where;
        else
            obj.transform.position = where;
    }

    [ObserversRpc]
    public void setRotation(NetworkObject obj, Quaternion rot, bool local = true)
    {
        if(local)
            obj.transform.localRotation = rot;
        else
            obj.transform.rotation = rot;
    }
}
