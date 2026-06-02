using System.Collections.Generic;
using UnityEngine;

public class Logging
{
    private Logging() { }
    private static Logging instance;
    private static readonly object m_lock = new object();

    public static Logging Instance()
    {
        if (instance == null)
        {

            lock (m_lock)
            {
                if (instance == null)
                {
                    instance = new Logging();
                }
            }

        }

        return instance;
    }

    private static List<long> m_Bytes = new List<long>();
    private static List<long> m_AverageRunTimeBytes = new List<long>();

    public static void Log(string value) 
    {
        Debug.Log(value);
    }

    public static void LogMemory(string objectName, long bytes, float time) 
    {
        m_Bytes.Add(bytes);
        Log($"GameObject: {objectName} Bytes: {bytes}");
        long total = 0;
        for (int i = 0; i < m_Bytes.Count; i++) 
        {
            total += m_Bytes[i];
        }
        Log($"Bytes: {total}");
    }

    public static void LogMemory2(long bytes, float time)
    {
        m_Bytes.Add(bytes);
        long total = 0;
        for (int i = 0; i < m_Bytes.Count; i++)
        {
            total += m_Bytes[i];
        }
        Log($"Bytes: {total}");
    }

    public static void LogForAverage(long bytes, float time)
    {
        m_AverageRunTimeBytes.Add(bytes);
        long total = 0;
        for (int i = 0; i < m_AverageRunTimeBytes.Count; i++)
        {
            total += m_AverageRunTimeBytes[i];
        }
        total /= m_AverageRunTimeBytes.Count;
        Log($"Bytes: {total}");
    }

    public static void LogFile() 
    {

    }

}
