using MMSystem;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class MMController : MonoBehaviour
{
    //private PoseDataBase m_PDB = new PoseDataBase();

    [Header("Setup")]
    [SerializeField] private AnimationClip[] clips;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform m_RootBone;
    [SerializeField] private Transform m_LFootBone;
    [SerializeField] private Transform m_RFootBone;
    [SerializeField] private SearchAndSaveType m_SST;

    [Header("Weightings")]
    [SerializeField] private float _PoseFavourWeight;
    [SerializeField] private float speed = 0;
    [SerializeField] private float TrajectoryWeighting = 1f;
    [SerializeField] private float VelocityWeighting = 1f;
    [SerializeField] private float PoseWeighting = 1f;
    [SerializeField] private float ContinuityWeight = 1f;
    [SerializeField] private float backwardPenaltyScale = 1f;

    private MMSystem.Pose pose = new MMSystem.Pose();
    private Transform[] bones;
    private MMSystem.Pose[] m_Poses;
    private FeatureVector[] m_Features;
    private Coroutine testCo;
    private int i = 0;
    private Vector3 direct;
    private float time = (1f / 30f);
    private float magnitude = -1;

    private Vector3 m_LastLFootPos, m_LastRFootPos, m_LastHipPos;
    private bool m_HasLastPos = false;

    private void OnValidate()
    {
        //AnimationClip Data grabbing
        if (clips.Length != 0)
        {
            float frames = 0f;
            for (int i = 0; i < clips.Length; i++)
            {
                frames += clips[i].frameRate * clips[i].length;
            }
            m_Poses = new MMSystem.Pose[(int)frames];
            m_Features = new MMSystem.FeatureVector[(int)frames];
            //UnityEngine.Debug.Log("efeffffufeuh");

            PoseDataBase test = new PoseDataBase();
            //long before = GC.GetTotalMemory(true);
            test.ConvertData(m_Poses, m_Features, clips, anim, m_SST, m_LFootBone, m_RFootBone, m_RootBone, ref magnitude);
            float[] weights = { VelocityWeighting, PoseWeighting, TrajectoryWeighting };
            test.ApplyWeightings(m_Poses, weights);
            test.NormalizeVector(m_Features);

            //long after = GC.GetTotalMemory(true);
            //Logging.LogMemory(gameObject.name, after - before, 0.0f);
        }
    }

    private void Start()
    {
        bones = anim.GetComponentsInChildren<Transform>();
        pose = m_Poses[0];
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(AnimImplement(Vector3.zero));
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
        // ... timer setup ...

        float interval = 1f / 30f;
        var timer = new Stopwatch();
        timer.Start();
        float nextTime = 0;
        MMSystem.FeatureVector vector = new MMSystem.FeatureVector();

        direct = dir;

        if (pose.id > 100000)
        {
        }

        if (i >= m_Poses.Length)
            i = 0;

        vector = pose.feature;

        FeatureVector currentVec = new FeatureVector();
        currentVec.m_FeatureVector = new float[24];

        Transform root = m_RootBone;
        Quaternion invRot = Quaternion.Inverse(root.rotation);

        // -- Velocities (local space) --
        Vector3 lFootVelLocal = invRot * pose.feature.m_CurrentLFootVel;
        Vector3 rFootVelLocal = invRot * pose.feature.m_CurrentRFootVel;
        Vector3 hipVelLocal = invRot * pose.feature.m_CurrentHipVel;

        // -- Foot positions relative to root (local space) --
        Vector3 lFootPosLocal = invRot * (m_LFootBone.position - root.position);
        Vector3 rFootPosLocal = invRot * (m_RFootBone.position - root.position);

        // -- Trajectory: predict future root positions in local space --
        // These represent where the character WANTS to be at t+20, t+40, t+60 frames
        // dir is in world space — project into character local space
        Vector3 dirLocal = invRot * dir.normalized;

        float dt = 1f / 30f;
        Vector3 t1 = dirLocal * speed * dt * 6f;  // ~0.67s ahead
        Vector3 t2 = dirLocal * speed * dt * 9f;  // ~1.33s ahead
        Vector3 t3 = dirLocal * speed * dt * 12f;  // ~2.0s ahead

        // -- Pack into flat array (index = boneNum * 3) --
        PackVec3(currentVec.m_FeatureVector, lFootVelLocal, 0);   // [0..2]
        PackVec3(currentVec.m_FeatureVector, rFootVelLocal, 1);   // [3..5]
        PackVec3(currentVec.m_FeatureVector, hipVelLocal, 2);   // [6..8]
        PackVec3(currentVec.m_FeatureVector, lFootPosLocal, 3);   // [9..11]
        PackVec3(currentVec.m_FeatureVector, rFootPosLocal, 4);   // [12..14]
        PackVec3(currentVec.m_FeatureVector, t1, 5);   // [15..17]
        PackVec3(currentVec.m_FeatureVector, t2, 6);   // [18..20]
        PackVec3(currentVec.m_FeatureVector, t3, 7);   // [21..23]

        // -- Apply weightings ONCE before the search --
        ScaleVec3(currentVec.m_FeatureVector, VelocityWeighting, 0);
        ScaleVec3(currentVec.m_FeatureVector, VelocityWeighting, 1);
        ScaleVec3(currentVec.m_FeatureVector, VelocityWeighting, 2);
        ScaleVec3(currentVec.m_FeatureVector, PoseWeighting, 3);
        ScaleVec3(currentVec.m_FeatureVector, PoseWeighting, 4);
        ScaleVec3(currentVec.m_FeatureVector, TrajectoryWeighting, 5);
        ScaleVec3(currentVec.m_FeatureVector, TrajectoryWeighting, 6);
        ScaleVec3(currentVec.m_FeatureVector, TrajectoryWeighting, 7);

        // -- Normalise the query vector using the DATABASE mean/stddev --
        // Use the first pose's stats as a proxy (or store global stats)
        float[] mean = m_Poses[0].feature.m_mean;
        float[] stdDev = m_Poses[0].feature.m_stdDevs;
        float[] normalisedQuery = new float[24];
        for (int k = 0; k < 24; k++)
            normalisedQuery[k] = (currentVec.m_FeatureVector[k] - mean[k]) / stdDev[k];

        // -- Search --
        float dist = float.MaxValue;
        foreach (var p in m_Poses)
        {
            FeatureVector fv = p.feature;

            int diff = fv.poseIndex - pose.feature.poseIndex;

            // Strongly favour the natural next frame (continuity)
            float continuityBonus = 0f;
            if (diff == 1 || diff == 0)
            {
                continuityBonus = -ContinuityWeight; // negative = cheaper = preferred
            }

            // Penalise going backwards in the clip (prevents 8->0 snapping)
            float backwardPenalty = 0f;
            if (diff < 0)
            {
                backwardPenalty = Mathf.Abs(diff) * backwardPenaltyScale;
            }

            float totalCost = CalcCost(normalisedQuery, p.feature.m_FeatureVector);

            //totalCost += backwardPenalty + continuityBonus;

            // ... pose favour penalty ...
            if (totalCost < dist)
            { 
                dist = totalCost;
                i = p.feature.poseIndex;
                UnityEngine.Debug.Log($"Cost: {totalCost}");
            }
        }
        UnityEngine.Debug.Log("----------------------------------------------------------");


        // ... apply pose to bones ...

        foreach (var bone in bones)
        {
            if (m_Poses[i].rootBone.name == bone.name)
            {
                bone.rotation = m_Poses[i].rootRot;
                bone.position = new Vector3(bone.position.x + m_Poses[i].deltaPos.x, m_Poses[i].HipHeight, bone.position.z + m_Poses[i].deltaPos.z);
                continue;
            }

            for (int j = 0; j < m_Poses[i].m_Bones.Length; j++)
            {
                if (m_Poses[i].m_Bones[j].m_Name != bone.name)
                    continue;

                bone.localPosition = m_Poses[i].m_Bones[j].m_BonePos;
                bone.localRotation = m_Poses[i].m_Bones[j].m_BoneRot;
            }
        }

        // In AnimImplement, after applying the pose:
        Vector3 currentLFootPos = invRot * (m_LFootBone.position - root.position);
        Vector3 currentRFootPos = invRot * (m_RFootBone.position - root.position);
        Vector3 currentHipPos = Vector3.zero;

        Vector3 lFootVelLocal2, rFootVelLocal2, hipVelLocal2;

        if (m_HasLastPos)
        {
            lFootVelLocal2 = (currentLFootPos - m_LastLFootPos) / dt;
            rFootVelLocal2 = (currentRFootPos - m_LastRFootPos) / dt;
            hipVelLocal2 = (currentHipPos - m_LastHipPos) / dt;
        }
        else
        {
            lFootVelLocal2 = rFootVelLocal2 = hipVelLocal2 = Vector3.zero;
        }

        m_LastLFootPos = currentLFootPos;
        m_LastRFootPos = currentRFootPos;
        m_LastHipPos = currentHipPos;
        m_HasLastPos = true;

        pose = m_Poses[i];
        nextTime += interval;
        float wait = nextTime - (float)timer.Elapsed.TotalSeconds;

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

    void PackVec3(float[] arr, Vector3 v, int slot)
    {
        int s = slot * 3;
        arr[s] = v.x; arr[s + 1] = v.y; arr[s + 2] = v.z;
    }

    void ScaleVec3(float[] arr, float w, int slot)
    {
        int s = slot * 3;
        arr[s] *= w; arr[s + 1] *= w; arr[s + 2] *= w;
    }

    /*
    IEnumerator AnimImplement(Vector3 dir)
    {
        float interval = 1f / 30f;
        var timer = new Stopwatch();
        timer.Start();
        float nextTime = 0;
        MMSystem.FeatureVector vector = new MMSystem.FeatureVector();

        direct = dir;

        float dist = 100000000f;

        if (pose.id > 100000)
        {
        }

        if (i >= m_Poses.Length)
            i = 0;

        vector = pose.feature;

        FeatureVector currentVec = new FeatureVector();
        currentVec.m_FeatureVector = new float[24];
        currentVec.trajectories = new Trajectory[3];

        //Build Feature
        BuildAFeature(currentVec, vector.m_CurrentLFootVel, 0);
        BuildAFeature(currentVec, vector.m_CurrentRFootVel, 1);
        BuildAFeature(currentVec, vector.m_CurrentHipVel, 2);

        BuildAFeature(currentVec, vector.m_CurrentLFootPos, 3);
        BuildAFeature(currentVec, vector.m_CurrentRFootPos, 4);

        BuildAFeature(currentVec, (direct * speed * time * 6f), 5);
        BuildAFeature(currentVec, (direct * speed * time * 9f), 6);
        BuildAFeature(currentVec, (direct * speed * time * 12f), 7);

        //Weight Feature
        WeightFeature(currentVec, VelocityWeighting, vector.m_CurrentLFootVel, 0);
        WeightFeature(currentVec, VelocityWeighting, vector.m_CurrentRFootVel, 1);
        WeightFeature(currentVec, VelocityWeighting, vector.m_CurrentHipVel, 2);

        WeightFeature(currentVec, PoseWeighting, vector.m_CurrentLFootPos, 3);
        WeightFeature(currentVec, PoseWeighting, vector.m_CurrentRFootPos, 4);

        WeightFeature(currentVec, TrajectoryWeighting, currentVec.trajectories[0].m_FuturePos, 5);
        WeightFeature(currentVec, TrajectoryWeighting, currentVec.trajectories[1].m_FuturePos, 6);
        WeightFeature(currentVec, TrajectoryWeighting, currentVec.trajectories[2].m_FuturePos, 7);

        for (int i = 0; i < currentVec.m_FeatureVector.Length; i++)
        {
            currentVec.m_FeatureVector[i] = (currentVec.m_FeatureVector[i] - vector.m_mean[i]) / vector.m_stdDevs[i];
        }

        foreach (var p in m_Poses)
        {
            float totalCost = 0.0f;
            FeatureVector fv = p.feature;

            int currentIndex = vector.poseIndex;
            int testingIndex = fv.poseIndex;
            float diff = testingIndex - currentIndex;

            if (diff <= 0 && diff >= -15)
            {
                diff = 20f / _PoseFavourWeight;
            }
            else if (diff > 10)
            {
                diff = 16f / _PoseFavourWeight;
            }
            else
            {
                diff = 0f;
            }

            for (int i = 0; i < currentVec.m_FeatureVector.Length; i++) 
            {
                currentVec.m_FeatureVector[i] = (currentVec.m_FeatureVector[i] - p.feature.m_mean[i]) / p.feature.m_stdDevs[i];
            }

            totalCost = CalcCost(currentVec.m_FeatureVector, p.feature.m_FeatureVector);
            totalCost += diff;

            if (dist > totalCost)
            {
                dist = totalCost;
                i = p.feature.poseIndex;
            }
            UnityEngine.Debug.Log($"Option: {p.feature.poseIndex} Cost: {totalCost}");
        }

        UnityEngine.Debug.Log("***************************************************************");
        UnityEngine.Debug.Log($"Chosen: {i} Cost: {dist}");
        UnityEngine.Debug.Log("***************************************************************");

        foreach (var bone in bones)
        {
            if (m_Poses[i].rootBone.name == bone.name)
            {
                bone.rotation = m_Poses[i].rootRot;
                bone.position = new Vector3(bone.position.x + m_Poses[i].deltaPos.x, m_Poses[i].HipHeight, bone.position.z + m_Poses[i].deltaPos.z);
                continue;
            }

            for (int j = 0; j < m_Poses[i].m_Bones.Length; j++)
            {
                if (m_Poses[i].m_Bones[j].m_Name != bone.name)
                    continue;

                bone.localPosition = m_Poses[i].m_Bones[j].m_BonePos;
                bone.localRotation = m_Poses[i].m_Bones[j].m_BoneRot;
            }
        }

        pose = m_Poses[i];
        nextTime += interval;
        float wait = nextTime - (float)timer.Elapsed.TotalSeconds;

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
        */

    private void AddVector3Cost(float weighting, Vector3 c, Vector3 t, ref float total)
    {
        total += weighting * Mathf.Sqrt(((c.x - t.x) * (c.x - t.x)));
        total += weighting * Mathf.Sqrt(((c.y - t.y) * (c.y - t.y)));
        total += weighting * Mathf.Sqrt(((c.z - t.z) * (c.z - t.z)));
    }

    private float CalcCost(float[] current, float[] target)
    {
        float cost = 0.0f;
        for (int i = 0; i < current.Length; i++)
        {
            float diff = current[i] - target[i];
            cost += diff * diff; // squared L2 — no sqrt needed
        }
        return cost;
    }

    private void BuildAFeature(FeatureVector vec, Vector3 bone, int boneNum) 
    {
        int startPoint = (boneNum * 3) - 1;
        if (boneNum == 0)
            startPoint = 0;

        vec.m_FeatureVector[startPoint] = bone.x;
        vec.m_FeatureVector[startPoint + 1] = bone.y;
        vec.m_FeatureVector[startPoint + 2] = bone.z;
    }

    private void WeightFeature(FeatureVector vec, float weight, Vector3 bone, int boneNum)
    {
        int startPoint = boneNum * 3;
        if (boneNum == 0)
            startPoint = 0;

        vec.m_FeatureVector[startPoint] *= weight;
        vec.m_FeatureVector[startPoint+1] *= weight;
        vec.m_FeatureVector[startPoint+2] *= weight;
    }

    private void OnDrawGizmos()
    {
        float size = 0.05f;
        float origin = 0.1f;
        Gizmos.DrawCube((direct * speed * time * 6f) + gameObject.transform.position, new Vector3(origin, origin, origin));
        Gizmos.DrawCube((direct * speed * time * 12f) + gameObject.transform.position, new Vector3(origin, origin, origin));
        Gizmos.DrawCube((direct * speed * time * 18f) + gameObject.transform.position, new Vector3(origin, origin, origin));
    }

}
