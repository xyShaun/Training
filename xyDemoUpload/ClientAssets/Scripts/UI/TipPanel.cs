using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipPanel : BasePanel
{
    private Text hintText = null;
    private Button okBtn = null;

    public override void OnInit()
    {
        prefabPath = "Prefab/" + "TipPanel";
        layer = PanelManager.Layer.Tip;
    }

    public override void OnShow(params object[] objects)
    {
        hintText = prefabIns.transform.Find("HintText").GetComponent<Text>();
        okBtn = prefabIns.transform.Find("OkBtn").GetComponent<Button>();

        okBtn.onClick.AddListener(OnOkBtnClick);

        if (objects.Length == 1)
        {
            hintText.text = objects[0] as string;
        }
    }

    private void OnOkBtnClick()
    {
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
