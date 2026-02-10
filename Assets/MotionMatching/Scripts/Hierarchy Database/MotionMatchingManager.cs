using MMSystem;
using System.Collections.Generic;
using UnityEngine;

public class MotionMatchingManager : MonoBehaviour
{
    private MotionMatchingManager() { }


    private static MotionMatchingManager instance;
    private static readonly object m_lock = new object();

    public static MotionMatchingManager Instance()
    {
        if (instance == null) 
        {

            lock (m_lock) 
            {
                if (instance == null) 
                {
                    instance = new MotionMatchingManager();
                }
            }

        }

        return instance;
    }

    //Variables
    private static PoseSearch m_PS = new PoseSearch();
    private static Dictionary<string, Dataset> m_DataSet = new Dictionary<string, Dataset>();
    private static HierarchyDatabase m_DB = new HierarchyDatabase();

    public static void AddDataSet(MMPlayerController pc) 
    {
        if (m_DataSet.ContainsKey(pc.m_ArchType.name)) 
        {
            return;
        }
        Dataset dataset = new Dataset();
        m_DB.MakeDatabase(pc.anim, pc.m_ArchType, dataset, pc.RootBone, pc.LFoot, pc.RFoot);
        m_DataSet.Add(pc.m_ArchType.name, dataset);
    }

    public static MMSystem.Pose GetPose(string name, MotionTypeEnum type, MotionEnum motion, FeatureVector fv, Goal goal, Weightings weights) 
    {
        MMSystem.Pose pose = new MMSystem.Pose();
        Dataset dataset;

        m_DataSet.TryGetValue(name, out dataset);
        dataset.m_Features.TryGetValue(type, out var data);
        data.TryGetValue(motion, out var features);

        fv.trajectories = goal.m_Trajectories;

        FeatureVector feature = m_PS.SearchFeatureArray(features, fv, weights, dataset.m_Magnitude);

        pose = dataset.m_Poses[feature.poseIndex];
        pose.feature = feature;
        return pose;
    }

    public static FeatureVector GetStartingPose(string name, MotionTypeEnum type, MotionEnum motion) 
    {
        m_DataSet.TryGetValue(name, out var data);
        data.m_Features.TryGetValue(type, out var motions);
        motions.TryGetValue(motion, out var root);
        return root[0];
    }

    public static MMSystem.Pose GetPoseWithIndex(string name, int index) 
    {
        m_DataSet.TryGetValue(name, out var data);
        return data.m_Poses[index];
    }

    public static MMSystem.Pose[] GetPoseArray(string name) 
    {
        m_DataSet.TryGetValue(name, out var data);
        return data.m_Poses;
    }

}
