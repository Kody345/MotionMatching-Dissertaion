using MMSystem;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class MMPlayerController : MonoBehaviour
{
    [SerializeField] private SearchAndSaveType m_DatabaseType;
    public Archtype m_ArchType;
    public Transform LFoot;
    public Transform RFoot;
    public Transform RootBone;
    public Animator anim;

    [SerializeField] private float speed;
    [SerializeField] private float m_PoseFavourWeight;
    [SerializeField] private float m_PosWeight;
    [SerializeField] private float m_VelWeight;
    [SerializeField] private float m_TrajectoryWeighting;

    [SerializeField] private int m_frame1;
    [SerializeField] private int m_frame2;
    [SerializeField] private int m_frame3;

    [SerializeField] private bool trip;

    private Transform[] m_bones;
    private MMSystem.Pose currentPose = new MMSystem.Pose();
    private FeatureVector currentVector = new FeatureVector();
    private Coroutine testCo;
    private Weightings m_Weightings = new Weightings();

    private MotionTypeEnum type;
    private MotionEnum motion;
    private Goal m_goal = new Goal();
    private Vector3 test = new Vector3();

    private void OnValidate()
    {
        long before = GC.GetTotalMemory(true);
        if (m_ArchType == null)
            return;

        //UnityEngine.Debug.Log("efefffufeuh");

        MotionMatchingManager.AddDataSet(this);
        long after = GC.GetTotalMemory(true);

        Logging.LogMemory(gameObject.name, after - before, 0.0f);

    }

    private void Start()
    {
        m_bones = anim.GetComponentsInChildren<Transform>();
        type = MotionTypeEnum.Normal;
        motion = MotionEnum.Walking;

        m_Weightings.PosWeight = m_PosWeight;
        m_Weightings.VelWeight = m_VelWeight;
        m_Weightings.PoseFavourWeight = m_PoseFavourWeight;
        m_Weightings.TrajectoryWeighting = m_TrajectoryWeighting;

        currentVector = MotionMatchingManager.GetStartingPose(m_ArchType.name, type, motion);
        currentPose = MotionMatchingManager.GetPoseWithIndex(m_ArchType.name, currentVector.poseIndex);
        foreach (var bone in m_bones)
        {
            if (currentPose.rootBone.name == bone.name)
            {
                bone.rotation = currentPose.rootRot;
                bone.position = new Vector3(bone.position.x + currentPose.deltaPos.x, currentPose.HipHeight, bone.position.z + currentPose.deltaPos.z);
                continue;
            }

            for (int j = 0; j < currentPose.m_Bones.Length; j++)
            {
                if (currentPose.m_Bones[j].m_Name != bone.name)
                    continue;

                bone.localPosition = currentPose.m_Bones[j].m_BonePos;
                bone.localRotation = currentPose.m_Bones[j].m_BoneRot;
            }
        }
    }

    private void LateUpdate()
    {
        if (trip)
            return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(AnimImplement(Vector3.zero));
        }

        if (Input.GetKey(KeyCode.G))
        {
            //MovementImplement(Vector3.back);
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.forward));
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            type = MotionTypeEnum.Injured;
            motion = MotionEnum.Walking;
            speed = 1.5f;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            type = MotionTypeEnum.Drunk;
            motion = MotionEnum.Walking;
            speed = 1.5f;
        }


        if (Input.GetKey(KeyCode.W))
        {
            //MovementImplement(Vector3.back);
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.forward));
        }
        if (Input.GetKey(KeyCode.A))
        {
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.left));
        }
        if (Input.GetKey(KeyCode.D))
        {
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.right));
        }
        if (Input.GetKey(KeyCode.S))
        {
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.back));
        }

    }

    IEnumerator AnimImplement(Vector3 dir)
    {
        long before = GC.GetTotalMemory(true);

        float interval = 1f / 30f;
        var timer = new Stopwatch();
        timer.Start();
        float nextTime = 0;
        MMSystem.FeatureVector vector = new MMSystem.FeatureVector();
        test = dir;
        vector = currentVector;
        m_goal.m_Trajectories = new Trajectory[3];
        m_goal.m_Trajectories[0].m_FuturePos = (dir * speed * (m_frame1 * (1f / 30f)));
        m_goal.m_Trajectories[1].m_FuturePos = (dir * speed * (m_frame2 * (1f / 30f)));
        m_goal.m_Trajectories[2].m_FuturePos = (dir * speed * (m_frame3 * (1f / 30f)));

        currentPose = MotionMatchingManager.GetPose(m_ArchType.name, type, motion, currentVector, m_goal, m_Weightings);
        currentVector = currentPose.feature;

        foreach (var bone in m_bones)
        {
            if (currentPose.rootBone.name == bone.name)
            {
                bone.rotation = currentPose.rootRot;
                bone.position = new Vector3(bone.position.x + currentPose.deltaPos.x, currentPose.HipHeight, bone.position.z + currentPose.deltaPos.z);
                continue;
            }

            for (int j = 0; j < currentPose.m_Bones.Length; j++)
            {
                if (currentPose.m_Bones[j].m_Name != bone.name)
                    continue;

                bone.localPosition = currentPose.m_Bones[j].m_BonePos;
                bone.localRotation = currentPose.m_Bones[j].m_BoneRot;
            }
        }

        nextTime += interval;
        float wait = nextTime - (float)timer.Elapsed.TotalSeconds;

        long after = GC.GetTotalMemory(true);
        UnityEngine.Debug.Log(after - before);
        Logging.LogForAverage(after - before, 0.0f);

        if (wait > 0f)
        {
            yield return new WaitForSecondsRealtime(wait);
            testCo = null;
        }
        else
        {
            yield return null;
            testCo = null;
        }

    }

    private void OnDrawGizmos()
    {
        //float size = 0.05f;
        //float origin = 0.1f;

        //Gizmos.DrawCube((test * speed * (m_frame2 * (1f / 30f))), new Vector3(origin, origin, origin));
        //Gizmos.DrawCube((test * speed * (m_frame3 * (1f / 30f))), new Vector3(origin, origin, origin));
        //Gizmos.DrawCube((test * speed * (m_frame1 * (1f / 30f))), new Vector3(origin, origin, origin));
        //Gizmos.DrawCube(currentVector.trajectories[0].m_FuturePos, new Vector3(origin, origin, origin));
        //Gizmos.DrawCube(currentVector.trajectories[1].m_FuturePos, new Vector3(origin, origin, origin));
        //Gizmos.DrawCube(currentVector.trajectories[2].m_FuturePos, new Vector3(origin, origin, origin));
    }

}
