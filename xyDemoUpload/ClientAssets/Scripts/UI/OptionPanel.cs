using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionPanel : BasePanel
{
    private InputField ipInput = null;
    private InputField portInput = null;
    private Button backBtn = null;

    public override void OnInit()
    {
        prefabPath = "Prefab/" + "OptionPanel";
        layer = PanelManager.Layer.Panel;
    }

    public override void OnShow(params object[] objects)
    {
        ipInput = prefabIns.transform.Find("IpInput").GetComponent<InputField>();
        portInput = prefabIns.transform.Find("PortInput").GetComponent<InputField>();
        backBtn = prefabIns.transform.Find("BackBtn").GetComponent<Button>();

        backBtn.onClick.AddListener(OnBackBtnClick);

        ipInput.text = GameMain.ip;
        portInput.text = GameMain.port.ToString();
    }

    private void OnBackBtnClick()
    {
        GameMain.ip = ipInput.text;
        GameMain.port = int.Parse(portInput.text);

        PanelManager.Open<MainPanel>();
        Close();
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
