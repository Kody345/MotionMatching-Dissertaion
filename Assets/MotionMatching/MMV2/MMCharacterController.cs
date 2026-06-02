using UnityEngine;
using MMSystemV2;

public class MMCharacterController : MonoBehaviour
{
    //Animator
    [SerializeField] private Animator m_Anim;

    //Feature Bones
    [SerializeField] private Transform m_HipBone;
    [SerializeField] private Transform[] m_FeatureBones;

    //Animations Data
    [SerializeField] private AnimationClip[] m_Animationclips;

    public void BakeDatabase()
    {
        MMPoseDatabase db = new MMPoseDatabase();
        MMCharacterProfile profile = new MMCharacterProfile();

        SetupProfile(profile);
        db.CreateDatabase(profile);
    }

    private void SetupProfile(MMCharacterProfile profile) 
    {
        profile.m_GameObject = gameObject;
        profile.m_Anim = m_Anim;
        profile.m_HipBone = m_HipBone;
        profile.m_FeatureBones = m_FeatureBones;
        profile.m_Animations = m_Animationclips;
    }
}
