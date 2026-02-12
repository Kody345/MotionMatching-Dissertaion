using MMSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Archtype", menuName = "Motion Mathcing/Archtype")]
public class Archtype : ScriptableObject
{
    public string name;
    public Archtype Parent;
    public MotionType[] m_MotionTypes;
}
