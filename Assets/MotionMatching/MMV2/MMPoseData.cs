using System.Collections.Generic;
using UnityEngine;

namespace MMSystemV2 
{
    //All the information needed to create the database of the character
    public class MMCharacterProfile 
    {
        public GameObject m_GameObject;
        public Animator m_Anim;

        public MMPose[] m_Poses;
        //public List<MMPose> m_Poses;

        //The bones that will be monitored. E.g. LeftFoot, right foot
        public Transform m_HipBone;
        public Transform[] m_FeatureBones;
        public AnimationClip[] m_Animations;

        public MMFeatureInfo[] m_FeatureInfos;
    };

    //The pose of a frame from an animation
    public struct MMPose
    {
        public MMBone[] m_Bones;
        public Vector2 m_DeltaPosition;
        public Vector3 m_RelativePosition;

        public MMTrajectory[] m_Trajectories;
        public float[] m_FeatureVector;
    }
    
    //Holds the information for each bone
    public struct MMBone
    {
        public string m_Name;
        public Vector3 m_Position;
        public Quaternion m_Rotation;
    }

    public struct MMTrajectory 
    {
        public Vector2 m_Position;
    }

    //Holds the weight and normalization values for the feature vector
    public struct MMFeatureInfo
    {
        public float m_Mean;
        public float m_STDDev;
        public float m_Weight;
    }
}