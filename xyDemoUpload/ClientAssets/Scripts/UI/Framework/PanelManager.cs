using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManager
{
    public enum Layer
    {
        Panel,
        Tip,
    }

    private static Dictionary<Layer, Transform> layers = new Dictionary<Layer, Transform>();

    public static Dictionary<string, BasePanel> openedPanels = new Dictionary<string, BasePanel>();
    public static Transform root = null;
    public static Transform canvas = null;

    public static void Init()
    {
        root = GameObject.Find("Root").transform;
        canvas = root.Find("Canvas");
        Transform panel = canvas.Find("Panel");
        Transform tip = canvas.Find("Tip");
        layers.Add(Layer.Panel, panel);
        layers.Add(Layer.Tip, tip);
    }

    public static void Open<T>(params object[] objects) where T : BasePanel
    {
        string name = typeof(T).ToString();
        if (openedPanels.ContainsKey(name))
        {
            return;
        }

        BasePanel panel = root.gameObject.AddComponent<T>();
        panel.Init();

        Transform layer = layers[panel.layer];
        panel.prefabIns.transform.SetParent(layer, false);

        openedPanels.Add(name, panel);
        panel.OnShow(objects);
    }

    public static void Close(string name)
    {
        if (!openedPanels.ContainsKey(name))
        {
            return;
        }

        BasePanel panel = openedPanels[name];
        panel.OnClose();

        openedPanels.Remove(name);

        GameObject.Destroy(panel.prefabIns);
        Component.Destroy(panel);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
