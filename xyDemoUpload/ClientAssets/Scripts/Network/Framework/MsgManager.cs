using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

//public class MsgBase
//{
//    public string protocolName = "";
//}

public static class MsgManager
{
    public static string GetProtocolName(IExtensible msgBase)
    {
        return msgBase.ToString()/*.Split('.').Last()*/;
    }

    public static byte[] EncodeProtocolBody(IExtensible msgBase)
    {
        //string str = JsonUtility.ToJson(msgBase);
        //byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);

        byte[] bytes = null;
        using (var memory = new MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(memory, msgBase);
            bytes = memory.ToArray();
        }

        return bytes;
    }

    public static IExtensible DecodeProtocolBody(string protocolName, byte[] bytes, int offset, int count)
    {
        //string str = System.Text.Encoding.UTF8.GetString(bytes, offset, count);
        //MsgBase msgBase = JsonUtility.FromJson(str, Type.GetType(protocolName)) as MsgBase;

        IExtensible msgBase = null;
        using (var memory = new MemoryStream(bytes, offset, count))
        {
            Type type = Type.GetType(protocolName);
            msgBase = ProtoBuf.Serializer.NonGeneric.Deserialize(type, memory) as IExtensible;
        }

        return msgBase;
    }

    public static byte[] EncodeProtocolName(IExtensible msgBase)
    {
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(GetProtocolName(msgBase));
        Int16 nameLength = (Int16)nameBytes.Length;

        byte[] bytes = new byte[2 + nameLength];

        // little-endian
        bytes[0] = (byte)(nameLength % 256);
        bytes[1] = (byte)(nameLength / 256);
        Array.Copy(nameBytes, 0, bytes, 2, nameLength);

        return bytes;
    }

    public static string DecodeProtocolName(byte[] bytes, int offset, out int count)
    {
        count = 0;

        if (bytes.Length - offset < 2)
        {
            return "";
        }

        Int16 nameLength = (Int16)((bytes[offset + 1] << 8) | bytes[offset]);
        if (bytes.Length - offset - 2 < nameLength)
        {
            return "";
        }

        count = 2 + nameLength;
        string name = System.Text.Encoding.UTF8.GetString(bytes, offset + 2, nameLength);

        return name;
    }
}
