using MMSystem;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class MMController : MonoBehaviour
{
    private PoseDataBase m_PDB = new PoseDataBase();

    [Header("Setup")]
    [SerializeField] private AnimationClip clip;
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

    private MMSystem.Pose pose = new MMSystem.Pose();
    private Transform[] bones;
    private MMSystem.Pose[] m_Poses;
    private FeatureVector[] m_Features;
    private Coroutine testCo;
    private int i = 0;
    private Vector3 direct;
    private float time = (1f / 30f);

    private void OnValidate()
    {
        //AnimationClip Data grabbing
        if (clip != null)
        {
            float frame = clip.length * clip.frameRate;
            float frames = 0f;

            for (int i = 0; i < clips.Length; i++)
            {
                frames += clips[i].frameRate * clips[i].length;
            }

            m_Poses = new MMSystem.Pose[(int)frames];
            m_Features = new MMSystem.FeatureVector[(int)frames];

            m_PDB.ConvertData(m_Poses, m_Features, clips, anim, m_SST, m_LFootBone, m_RFootBone, m_RootBone);

        }
    }

    

    private void Start()
    {
        bones = anim.GetComponentsInChildren<Transform>();
        pose.id = 1000000000;
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
        float interval = 1f / 30f;
        var timer = new Stopwatch();
        timer.Start();
        float nextTime = 0;
        MMSystem.FeatureVector vector = new MMSystem.FeatureVector();

        direct = dir;

        float dist = 100000000f;

        if (pose.id > 1000)
        {
            pose = m_Poses[0];
        }

        if (i >= m_Poses.Length)
            i = 0;

        vector = pose.feature;

        foreach (var p in m_Poses)
        {
            float totalCost = 0.0f;
            FeatureVector fv = p.feature;

            int currentIndex = vector.poseIndex;
            int testingIndex = fv.poseIndex;
            float diff = testingIndex - currentIndex;

            if (diff <= 0 && diff >= -15)
            {
                diff = 10f / _PoseFavourWeight;
            }
            else if (diff > 5)
            {
                diff = 8f / _PoseFavourWeight;
            }
            else
            {
                diff = 0f;
            }
            
            totalCost += diff;

            AddVector3Cost(VelocityWeighting, vector.m_CurrentLFootVel, fv.m_CurrentLFootVel, ref totalCost);
            AddVector3Cost(VelocityWeighting, vector.m_CurrentRFootVel, fv.m_CurrentRFootVel, ref totalCost);
            AddVector3Cost(1f, vector.m_CurrentRFootPos, fv.m_CurrentRFootPos, ref totalCost);
            AddVector3Cost(1f, vector.m_CurrentLFootPos, fv.m_CurrentLFootPos, ref totalCost);
            AddVector3Cost(VelocityWeighting, vector.m_CurrentHipVel, fv.m_CurrentHipVel, ref totalCost);

            UnityEngine.Debug.Log($"Current Index: {vector.poseIndex} Testing Index: {fv.poseIndex} Before: {totalCost}");
            AddVector3Cost(TrajectoryWeighting, direct * speed * time * 6f, fv.m_CurrentHipVel, ref totalCost);
            AddVector3Cost(TrajectoryWeighting, direct * speed * time * 12, fv.m_CurrentHipVel, ref totalCost);
            AddVector3Cost(TrajectoryWeighting, direct * speed * time * 18, fv.m_CurrentHipVel, ref totalCost);
            UnityEngine.Debug.Log($"Current Index: {vector.poseIndex} Testing Index: {fv.poseIndex} After: {totalCost}");

            if (dist > totalCost)
            {
                dist = totalCost;
                i = p.feature.poseIndex;
            }
        }

        UnityEngine.Debug.Log($"Chosen Index: {i}");


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

    private float Cost(FeatureVector currentPose, FeatureVector testingPose)
    {
        float total = 0f;

        int currentIndex = currentPose.poseIndex;
        int testingIndex = testingPose.poseIndex;
        float diff = testingIndex - currentIndex;

        if (diff <= 0 && diff >= -8)
        {
            diff = 15 / _PoseFavourWeight;
        }
        else if (diff > 5)
        {
            diff = 8 / _PoseFavourWeight;
        }
        else
        {
            diff = 0;
        }

        for (int j = 0; j < currentPose.m_FeatureVector.Length; j++)
        {
            //UnityEngine.Debug.Log($"INdex: {j} Total: {total} Extra Cost: {diff} Current: {currentPose.m_FeatureVector[j]} Testing: {testingPose.m_FeatureVector[j]}");
            total += Mathf.Sqrt((currentPose.m_FeatureVector[j] - testingPose.m_FeatureVector[j]) * (currentPose.m_FeatureVector[j] - testingPose.m_FeatureVector[j]));
        }

        total += diff;

        UnityEngine.Debug.Log($"T: {total} Exta Cost: {diff} Diff : {testingIndex - currentIndex} Current: {currentIndex} Testing: {testingIndex}");

        return total;
    }

    private void AddVector3ToFeatureVector(float[] fv, Vector3 v, ref int i)
    {
        fv[i++] = v.x;
        fv[i++] = v.y;
        fv[i++] = v.z;
    }

    private void AddVector3Cost(float weighting, Vector3 c, Vector3 t, ref float total) 
    {
        total += weighting * Mathf.Sqrt(((c.x - t.x) * (c.x - t.x)));
        total += weighting * Mathf.Sqrt(((c.y - t.y) * (c.y - t.y)));
        total += weighting * Mathf.Sqrt(((c.z - t.z) * (c.z - t.z)));
    }

    private void OnDrawGizmos()
    {
        float size = 0.05f;
        float origin = 0.1f;
        Gizmos.DrawCube((direct * speed * time * 6f) + gameObject.transform.position, new Vector3(origin, origin, origin));
        Gizmos.DrawCube((direct * speed * time * 12f) + gameObject.transform.position, new Vector3(origin, origin, origin));
        Gizmos.DrawCube((direct * speed * time * 18f) + gameObject.transform.position, new Vector3(origin, origin, origin));

        for (int j = 0; j < m_Features[i].trajectories.Length; j++)
        {
            Gizmos.DrawCube((Vector3)m_Features[i].trajectories[j].m_FuturePos, new Vector3(size, size, size));
        }
    }

}
