using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
{
    public static GameObject LoadPrefab(string path)
    {
        return Resources.Load<GameObject>(path);
    }
}
