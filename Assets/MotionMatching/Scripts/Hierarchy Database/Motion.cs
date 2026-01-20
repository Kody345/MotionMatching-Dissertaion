using UnityEngine;

[CreateAssetMenu(fileName = "Motion", menuName = "Motion Mathcing/Motion")]
public class Motion : ScriptableObject
{
    public MMSystem.MotionEnum m_MotionType;
    public AnimationClip[] m_Clips;
}
