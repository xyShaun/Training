using System;
using System.Collections;
using System.Collections.Generic;
using proto.BattleMsg;
using proto.SyncMsg;
using ProtoBuf;
using UnityEngine;

public class BattleManager
{
    public static Dictionary<string, BaseCharacter> characters = new Dictionary<string, BaseCharacter>();

    public static void Init()
    {
        NetManager.AddMsgListener("MsgEnterBattle", OnMsgEnterBattle);
        NetManager.AddMsgListener("MsgBattleResult", OnMsgBattleResult);
        NetManager.AddMsgListener("MsgLeaveBattle", OnMsgLeaveBattle);

        NetManager.AddMsgListener("MsgSyncCharacter", OnMsgSyncCharacter);
        NetManager.AddMsgListener("MsgFire", OnMsgFire);
        NetManager.AddMsgListener("MsgHit", OnMsgHit);
    }

    private static void OnMsgHit(IExtensible msgBase)
    {
        MsgHit msgHit = msgBase as MsgHit;

        BaseCharacter character = GetCharacter(msgHit.targetId);
        if (character == null)
        {
            return;
        }

        character.WasAttacked(msgHit.damage);
    }

    private static void OnMsgFire(IExtensible msgBase)
    {
        MsgFire msgFire = msgBase as MsgFire;

        if (msgFire.id == GameMain.id)
        {
            return;
        }

        SyncCharacter character = GetCharacter(msgFire.id) as SyncCharacter;
        if (character == null)
        {
            return;
        }

        character.SyncFire(msgFire);
    }

    private static void OnMsgSyncCharacter(IExtensible msgBase)
    {
        MsgSyncCharacter msgSyncCharacter = msgBase as MsgSyncCharacter;

        if (msgSyncCharacter.id == GameMain.id)
        {
            return;
        }

        SyncCharacter character = GetCharacter(msgSyncCharacter.id) as SyncCharacter;
        if (character == null)
        {
            return;
        }

        character.SyncPos(msgSyncCharacter);
    }

    private static void OnMsgLeaveBattle(IExtensible msgBase)
    {
        MsgLeaveBattle msgLeaveBattle = msgBase as MsgLeaveBattle;

        BaseCharacter character = GetCharacter(msgLeaveBattle.id);
        if (character == null)
        {
            return;
        }

        RemoveCharacter(msgLeaveBattle.id);
        MonoBehaviour.Destroy(character.gameObject);
    }

    private static void OnMsgBattleResult(IExtensible msgBase)
    {
        MsgBattleResult msgBattleResult = msgBase as MsgBattleResult;

        bool isWin = false;
        BaseCharacter character = GetCtrlCharacter();
        if (character != null && msgBattleResult.winCamp == character.camp)
        {
            isWin = true;
        }

        PanelManager.Open<ResultPanel>(isWin);
    }

    private static void OnMsgEnterBattle(IExtensible msgBase)
    {
        MsgEnterBattle msgEnterBattle = msgBase as MsgEnterBattle;
        EnterBattle(msgEnterBattle);
    }

    private static void EnterBattle(MsgEnterBattle msgEnterBattle)
    {
        Reset();

        PanelManager.Close("RoomPanel");
        PanelManager.Close("ResultPanel");

        for (int i = 0; i < msgEnterBattle.characters.Count; ++i)
        {
            GenerateCharacterItem(msgEnterBattle.characters[i]);
        }
    }

    private static void GenerateCharacterItem(proto.BattleMsg.CharacterInfo characterInfo)
    {
        string objName = "Character_" + characterInfo.id;
        GameObject characterObj = new GameObject(objName);

        BaseCharacter character = null;
        if (characterInfo.id == GameMain.id)
        {
            character = characterObj.AddComponent<CtrlCharacter>();
            characterObj.AddComponent<CameraFollow>();
        }
        else
        {
            character = characterObj.AddComponent<SyncCharacter>();
        }

        character.camp = characterInfo.camp;
        character.id = characterInfo.id;
        character.hp = characterInfo.hp;
        character.transform.position = new Vector3(characterInfo.x, characterInfo.y, characterInfo.z);
        character.transform.eulerAngles = new Vector3(characterInfo.ex, characterInfo.ey, characterInfo.ez);

        if (characterInfo.camp == 0)
        {
            character.Init("Prefab/" + "Ethan0");
        }
        else
        {
            character.Init("Prefab/" + "Ethan1");
        }

        AddCharacter(characterInfo.id, character);
    }

    public static void AddCharacter(string id, BaseCharacter character)
    {
        characters[id] = character;
    }

    public static void RemoveCharacter(string id)
    {
        characters.Remove(id);
    }

    public static BaseCharacter GetCharacter(string id)
    {
        if (characters.ContainsKey(id))
        {
            return characters[id];
        }

        return null;
    }

    public static BaseCharacter GetCtrlCharacter()
    {
        return GetCharacter(GameMain.id);
    }

    public static void Reset()
    {
        foreach (BaseCharacter character in characters.Values)
        {
            MonoBehaviour.Destroy(character.gameObject);
        }

        characters.Clear();
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
