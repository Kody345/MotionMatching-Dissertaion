using UnityEngine;

[CreateAssetMenu(fileName = "MotionType", menuName = "Motion Mathcing/MotionType")]
public class MotionType : ScriptableObject
{
    public MMSystem.MotionTypeEnum m_MotionType;
    public Motion[] m_Motions;
}