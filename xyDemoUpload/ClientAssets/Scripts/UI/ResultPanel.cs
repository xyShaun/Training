using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : BasePanel
{
    private Image winImage = null;
    private Image loseImage = null;
    private Button okBtn = null;

    public override void OnInit()
    {
        prefabPath = "Prefab/" + "ResultPanel";
        layer = PanelManager.Layer.Tip;
    }

    public override void OnShow(params object[] objects)
    {
        winImage = prefabIns.transform.Find("WinImage").GetComponent<Image>();
        loseImage = prefabIns.transform.Find("LoseImage").GetComponent<Image>();
        okBtn = prefabIns.transform.Find("OkBtn").GetComponent<Button>();

        okBtn.onClick.AddListener(OnOkBtnClick);

        if (objects.Length == 1)
        {
            bool isWin = (bool)objects[0];
            if (isWin)
            {
                winImage.gameObject.SetActive(true);
                loseImage.gameObject.SetActive(false);
            }
            else
            {
                winImage.gameObject.SetActive(false);
                loseImage.gameObject.SetActive(true);
            }
        }
    }

    private void OnOkBtnClick()
    {
        PanelManager.Open<RoomPanel>();
        Close();
    }

    public override void OnClose()
    {
        
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
