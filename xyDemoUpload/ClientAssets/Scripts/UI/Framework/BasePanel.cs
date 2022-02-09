using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    public string prefabPath = null;
    public GameObject prefabIns = null;
    public PanelManager.Layer layer = PanelManager.Layer.Panel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init()
    {
        OnInit();

        GameObject prefab = ResourceManager.LoadPrefab(prefabPath);
        prefabIns = Instantiate<GameObject>(prefab);
    }

    public void Close()
    {
        string name = this.GetType().ToString();
        PanelManager.Close(name);
    }

    public virtual void OnInit()
    {

    }

    public virtual void OnShow(params object[] objects)
    {

    }

    public virtual void OnClose()
    {

    }
}
