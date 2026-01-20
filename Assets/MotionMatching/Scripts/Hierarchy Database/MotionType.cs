using UnityEngine;

[CreateAssetMenu(fileName = "MotionType", menuName = "Motion Mathcing/MotionType")]
public class MotionType : ScriptableObject
{
    [SerializeField] private MMSystem.MotionTypeEnum m_MotionType;
    [SerializeField] private Motion[] m_Motions;
}