using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ByteArray
{
    private const int DEFAULT_SIZE = 1024;
    private const int MOVE_MAX_SIZE = 10;

    private int initSize = 0;
    private int capaticy = 0;

    public byte[] byteData = null;
    public int readIndex = 0;
    public int writeIndex = 0;

    public int ReadableLength { get { return writeIndex - readIndex; } }
    public int WriteableLength { get { return capaticy - writeIndex; } }

    public ByteArray(int size = DEFAULT_SIZE)
    {
        byteData = new byte[size];
        initSize = size;
        capaticy = size;
    }

    public ByteArray(byte[] byteArray)
    {
        byteData = byteArray;
        initSize = byteArray.Length;
        capaticy = byteArray.Length;
        writeIndex = byteArray.Length;
    }

    public void Resize(int size)
    {
        if (size < ReadableLength)
        {
            return;
        }

        if (size < initSize)
        {
            Array.Copy(byteData, readIndex, byteData, 0, ReadableLength);
        }
        else
        {
            int newCapacity = 1;
            while (newCapacity < size)
            {
                newCapacity *= 2;
            }

            capaticy = newCapacity;
            byte[] newByteData = new byte[newCapacity];
            Array.Copy(byteData, readIndex, newByteData, 0, ReadableLength);
            byteData = newByteData;
        }

        writeIndex = ReadableLength;
        readIndex = 0;
    }

    public int Write(byte[] byteArray, int startIndex, int count)
    {
        if (WriteableLength < count)
        {
            Resize(ReadableLength + count);
        }

        Array.Copy(byteArray, startIndex, byteData, writeIndex, count);
        writeIndex += count;
        return count;
    }

    public int Read(byte[] byteArray, int startIndex, int count)
    {
        count = Math.Min(count, ReadableLength);
        Array.Copy(byteData, readIndex, byteArray, startIndex, count);
        readIndex += count;

        CheckAndMoveByteData();

        return count;
    }

    public void CheckAndMoveByteData()
    {
        if (ReadableLength < MOVE_MAX_SIZE)
        {
            MoveByteData();
        }
    }

    public void MoveByteData()
    {
        Array.Copy(byteData, readIndex, byteData, 0, ReadableLength);
        writeIndex = ReadableLength;
        readIndex = 0;
    }

    public Int16 ReadInt16()
    {
        Int16 ret = BitConverter.ToInt16(byteData, readIndex);
        readIndex += 2;

        CheckAndMoveByteData();

        return ret;
    }

    public Int32 ReadInt32()
    {
        Int32 ret = BitConverter.ToInt32(byteData, readIndex);
        readIndex += 4;

        CheckAndMoveByteData();

        return ret;
    }

    public override string ToString()
    {
        return BitConverter.ToString(byteData, readIndex, ReadableLength);
    }

    public string AllInfoToString()
    {
        return string.Format("byteData({0}) readIndex({1}) writeIndex({2})",
            BitConverter.ToString(byteData, 0, capaticy),
            readIndex,
            writeIndex
            );
    }
}
