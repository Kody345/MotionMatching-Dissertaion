using System.Collections.Generic;
using UnityEngine;

namespace MMSystem
{
    public struct Weightings 
    {
        public float PosWeight;
        public float VelWeight;
        public float PoseFavourWeight;
        public float TrajectoryWeighting;
    }

    public class Dataset 
    {
        public Dataset m_Parent;
        public string m_Name;
        public Dictionary<MotionTypeEnum, Dictionary<MotionEnum, FeatureNode>> m_Types = new Dictionary<MotionTypeEnum, Dictionary<MotionEnum, FeatureNode>>();
        public Dictionary<MotionTypeEnum, Dictionary<MotionEnum, FeatureVector[]>> m_Features = new Dictionary<MotionTypeEnum, Dictionary<MotionEnum, FeatureVector[]>>();
        public Pose[] m_Poses;
        public float m_Magnitude = 0;
    };

    public class FeatureNode 
    {
        public FeatureVector m_Feature;
        public FeatureNode m_LeftNode;
        public FeatureNode m_RightNode;
    };
    public class FeatureGroup
    {
        public FeatureVector m_Root;
        public FeatureVector[] m_features;
    };


    public enum MotionEnum 
    {
        None = 0,
        Walking = 1,
        CrouchWalking = 2,
        Running = 3,
        Specific = 4
    };

    public enum MotionTypeEnum 
    {
        None = 0,
        Drunk = 1,
        Normal = 2,
        Specific = 3
    };

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
        public int poseIndex;
    }

    public struct Bone 
    {
        public string m_Name;
        public Vector3 m_BonePos;
        public Quaternion m_BoneRot;
        public Vector3 m_BoneWorldPos;
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
        public float HipHeight;

        public Bone[] m_Bones;
        public Trajectory[] m_FuturePos;
        public Vector3 dir;
        public FeatureVector feature;
    }

    //DesiredPath
    public struct Goal 
    {
        public Trajectory[] m_Trajectories;
    }

    public enum SearchAndSaveType
    {
        Regular,
        Context,
        Heirarchy
    }
}