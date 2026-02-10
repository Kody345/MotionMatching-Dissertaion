using MMSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Archtype", menuName = "Motion Mathcing/Archtype")]
public class Archtype : ScriptableObject
{
    public string name;
    public MotionType[] m_MotionTypes;
}
