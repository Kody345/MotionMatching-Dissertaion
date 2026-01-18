using System.Collections.Generic;
using UnityEngine;

namespace MMSystem
{
    public struct Trajectory 
    {
        public Vector3 m_FuturePos;
        public Vector3 m_ForwardDir;
        public float m_FutureTime;
    }

    public struct FeatureVector 
    {
        public Vector3 m_CurrentHipVel;
        public Vector3 m_CurrentLFootVel;
        public Vector3 m_CurrentRFootVel;

        public Vector3 m_CurrentLFootPos;
        public Vector3 m_CurrentRFootPos;
        public Vector3 m_CurrentHipPos;

        public Trajectory[] trajectories;

        public float[] m_FeatureVector;
        //public float[][] m_FeatureVector;
        public int poseIndex;
    }

    public struct Bone 
    {
        public string m_Name;
        public Vector3 m_BonePos;
        public Quaternion m_BoneRot;
        public Vector3 m_BoneWorldPos;
        //Vector3 m_Velocity;
    }

    public struct Pose
    {
        public int id;
        public AnimationClip m_Clip;
        public int frame;
        public float m_Time;

        public Transform rootBone;
        public Vector3 rootPos;
        public Vector3 deltaPos;
        public Quaternion rootRot;
        public Quaternion deltaRot;

        public float testHipHeight;

        public Bone[] m_Bones;
        public Dictionary<string, Transform> BoneMap;
        public Trajectory[] m_FuturePos;
        public Vector3 dir;
        public FeatureVector feature;
    }

    //DesiredPath
    public struct Goal 
    {
        Trajectory[] m_Trajectories;
    }

    public enum SearchAndSaveType
    {
        Regular,
        Split,
        Context,
        Heirarchy
    }
}