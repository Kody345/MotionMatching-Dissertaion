using JetBrains.Annotations;
using UnityEngine;

namespace MMSystem
{
    public class PoseDataBase
    {
        private SearchAndSaveType m_SaveType;
        private Transform[] allBones;
        private Transform LFoot;
        private Transform RFoot;
        private Transform Root;
        float fullTime = 0.0f;

        public void SaveDataType(SearchAndSaveType type) { m_SaveType = type; }

        public bool SaveData()
        {
            return true;
        }

        public void ConvertData(MMSystem.Pose[] m_poses, MMSystem.FeatureVector[] m_features, AnimationClip[] clips, Animator anim, SearchAndSaveType type, Transform lFoot, Transform rFoot, Transform RootBone, ref float magnitude)
        {
            LFoot = lFoot;
            RFoot = rFoot;
            Root = RootBone;

            allBones = anim.GetComponentsInChildren<Transform>();
            int rootBoneIndex = -1;

            //Finds rootbone index
            for (int i = 0; i < allBones.Length; i++)
            {
                if (allBones[i].name == RootBone.name)
                {
                    rootBoneIndex = i;
                    break;
                }
            }

            //True if rootbone was not found
            if (rootBoneIndex == -1) { return; }

            int poseCount = 0;
            int startingFrame = 0;

            for (int i = 0; i < clips.Length; i++)
            {
                fullTime += clips[i].length;
                for (float t = 0f; t <= clips[i].length; t += 1f / clips[i].frameRate)
                {
                    clips[i].SampleAnimation(anim.gameObject, t);
                    allBones = anim.GetComponentsInChildren<Transform>();
                    MMSystem.Pose pose = new MMSystem.Pose();
                    int boneCount = 0;


                    pose.m_Time = t;
                    pose.m_Bones = new Bone[allBones.Length - rootBoneIndex];

                    for (int j = rootBoneIndex; j < allBones.Length; j++)
                    {
                        Bone bone = new Bone();
                        bone.m_Name = allBones[j].name;
                        bone.m_BoneWorldPos = allBones[j].position;
                        bone.m_BonePos = allBones[j].localPosition;
                        bone.m_BoneRot = allBones[j].localRotation;
                        pose.m_Bones[boneCount] = bone;
                        boneCount++;
                    }

                    if (poseCount >= m_poses.Length)
                        continue;

                    pose.frame = (int)(clips[i].frameRate * pose.m_Time);
                    pose.rootBone = allBones[rootBoneIndex];


                    if (pose.frame == 0)
                    {
                        startingFrame = poseCount;
                        pose.rootPos = pose.rootBone.position;
                        pose.HipHeight = pose.rootBone.position.y;

                        pose.rootRot = pose.rootBone.rotation;

                        pose.deltaPos = Vector3.zero;
                        pose.deltaRot = Quaternion.identity;
                    }
                    else
                    {
                        pose.rootPos = pose.rootBone.position - m_poses[startingFrame].rootPos;
                        pose.HipHeight = pose.rootBone.position.y;

                        pose.rootRot = pose.rootBone.rotation;
                    }

                    pose.m_Time = t;
                    pose.m_Clip = clips[i];
                    pose.id = poseCount;
                    m_poses[poseCount] = pose;
                    m_features[poseCount].poseIndex = poseCount;

                    poseCount++;
                }
            }

            //Setting Delta Motion
            foreach (var clip in clips)
            {
                for (int i = 0; i < m_poses.Length; i++)
                {
                    if (clip.name != m_poses[i].m_Clip.name)
                        continue;

                    if (m_poses[i].frame == 0)
                    {
                        m_poses[i].rootPos = Vector3.zero;
                        m_poses[i].deltaPos = Vector3.zero;

                        m_poses[i].rootRot = Quaternion.identity;
                        m_poses[i].deltaRot = Quaternion.identity;
                        continue;
                    }

                    m_poses[i].deltaPos = m_poses[i].rootPos - m_poses[i - 1].rootPos;
                    m_poses[i].deltaRot = Quaternion.Inverse(m_poses[i - 1].rootRot) * m_poses[i].rootRot;
                }
            }

            for (int i = 0; i < m_features.Length; i++) 
            {
                SetupPos(ref m_features[i], m_poses);
                Trajectory(ref m_features[i], m_poses);
            }

            SetuptVel(m_features, m_poses);
            CalcRootToFoot(m_features, m_poses);

            magnitude = -1;

            foreach (var fv in m_features)
            {
                FindBiggestMagnitude(fv, ref magnitude);
            }

            for (int i = 0; i < m_features.Length; i++) 
            {
                int c = 0;
                m_features[i].m_FeatureVector = new float[24];
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentLFootVel, ref c);
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentRFootVel, ref c);
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentLFootPos, ref c);
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentRFootPos, ref c);
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentHipVel, ref c);

                //Trajectories
                for (int j = 0; j < 3; j++)
                {
                    AddVector3ToFeatureVector(m_features[i].m_FeatureVector, (Vector3)m_features[i].trajectories[j].m_FuturePos, ref c);
                }

                m_poses[m_features[i].poseIndex].feature = m_features[i];
            }

            NormalizeVector(m_features, magnitude);

        }

        private void NormalizeVector(FeatureVector[] features, float mag) 
        {
            for (int i = 0; i < features.Length; i++) 
            {
                for (int j = 0; j < features[i].m_FeatureVector.Length; j++) 
                {
                    features[i].m_FeatureVector[j] /= mag;
                }
            }
        }

        private void FindBiggestMagnitude(FeatureVector fv, ref float mag) 
        {
            if (mag < fv.m_CurrentLFootVel.magnitude)
                mag = fv.m_CurrentLFootVel.magnitude;
            if (mag < fv.m_CurrentRFootVel.magnitude)
                mag = fv.m_CurrentRFootVel.magnitude;
            if (mag < fv.m_CurrentLFootPos.magnitude)
                mag = fv.m_CurrentLFootPos.magnitude;
            if (mag < fv.m_CurrentRFootPos.magnitude)
                mag = fv.m_CurrentRFootPos.magnitude;
            if (mag < fv.m_CurrentHipVel.magnitude)
                mag = fv.m_CurrentHipVel.magnitude;
            for (int i = 0; i < fv.trajectories.Length; i++) 
            {
                if (mag < fv.trajectories[i].m_FuturePos.magnitude)
                    mag = fv.trajectories[i].m_FuturePos.magnitude;
            }
        }

        private void CalcRootToFoot(FeatureVector[] fv, MMSystem.Pose[] poses) 
        {
            for (int i = 0; i < fv.Length; i++) 
            {
                fv[i].m_CurrentLFootPos = fv[i].m_CurrentLFootPos - fv[i].m_CurrentHipPos;
                fv[i].m_CurrentRFootPos = fv[i].m_CurrentRFootPos - fv[i].m_CurrentHipPos;
            }
        } 

        private void SetuptVel(FeatureVector[] fv, MMSystem.Pose[] poses) 
        {
            for (int i = 0; i < fv.Length; i++) 
            {
                //if ((int)(((poses[fv[i].poseIndex].m_Clip.length * 30) - 2) - (poses[fv[i].poseIndex].m_Time * 30)) <= 0)
                //    continue;

                fv[i].m_CurrentLFootVel = new Vector3();
                fv[i].m_CurrentRFootVel = new Vector3();
                fv[i].m_CurrentHipVel = new Vector3();

                if (i >= fv.Length - 1) 
                {
                    fv[i].m_CurrentLFootVel = fv[i - 1].m_CurrentLFootPos;
                    fv[i].m_CurrentRFootVel = fv[i - 1].m_CurrentRFootPos;
                    fv[i].m_CurrentHipVel = fv[i - 1].m_CurrentHipPos;
                    continue;
                }

                //if (i - 1 < 0)
                //    continue;

                fv[i].m_CurrentLFootVel = fv[i + 1].m_CurrentLFootPos - fv[i].m_CurrentLFootPos;
                fv[i].m_CurrentRFootVel = fv[i + 1].m_CurrentRFootPos - fv[i].m_CurrentRFootPos;
                fv[i].m_CurrentHipVel = fv[i + 1].m_CurrentHipPos - fv[i].m_CurrentHipPos;

                //fv[i].m_CurrentLFootVel = fv[i].m_CurrentLFootPos - fv[i - 1].m_CurrentLFootPos;
                //fv[i].m_CurrentRFootVel = fv[i].m_CurrentRFootPos - fv[i - 1].m_CurrentRFootPos;
                //fv[i].m_CurrentHipVel = fv[i].m_CurrentHipPos - fv[i - 1].m_CurrentHipPos;

            }
        }

        private void SetupPos(ref FeatureVector fv, MMSystem.Pose[] poses) 
        {
            foreach (var bone in poses[fv.poseIndex].m_Bones)
            {
                if (bone.m_Name == LFoot.name)
                {
                    fv.m_CurrentLFootPos = bone.m_BoneWorldPos;
                    continue;
                }
                if (bone.m_Name == RFoot.name) 
                {
                    fv.m_CurrentRFootPos = bone.m_BoneWorldPos;
                    continue;
                }
                if (bone.m_Name == Root.name) 
                {
                    fv.m_CurrentHipPos = bone.m_BoneWorldPos;
                    continue;
                }
            }
        }

        private void Trajectory(ref FeatureVector fv, MMSystem.Pose[] poses) 
        {
            int tCount = 0;
            int frameCount = 0;
            Vector3 fullDelta = new Vector3();

            fv.trajectories = new Trajectory[3];

            for (int i = 0; i < fv.trajectories.Length; i++) 
            {
                fv.trajectories[i].m_FuturePos = new Vector3();
            }

            for (float i = (fv.poseIndex / 30f) + 1f / 30f; i < fullTime; i += 1f / 30f)
            {
                if ((int)(i * 30f) >= poses.Length)
                    return;
                fullDelta += poses[(int)(i * 30f)].deltaPos;

                if (frameCount == 3)
                {
                    fv.trajectories[tCount].m_FuturePos = new Vector3(fullDelta.x, 0f, fullDelta.z);
                    //fv.trajectories[tCount].m_FutureTime = frameCount;
                    tCount++;
                }

                if (frameCount == 6) 
                {
                    fv.trajectories[tCount].m_FuturePos = new Vector3(fullDelta.x, 0f, fullDelta.z);
                    //fv.trajectories[tCount].m_FutureTime = frameCount;
                    tCount++;
                }

                if (frameCount == 9)
                {
                    fv.trajectories[tCount].m_FuturePos = new Vector3(fullDelta.x, 0f, fullDelta.z);
                    //fv.trajectories[tCount].m_FutureTime = frameCount;
                    tCount++;
                }

                if (tCount == 3)
                    break;

                frameCount++;
            }

            if (tCount == 0) 
            {
                fv.trajectories[0].m_FuturePos = new Vector3(fv.m_CurrentHipPos.x, 0f, fv.m_CurrentHipPos.z);
                tCount++;
            }
            if (tCount == 1) 
            {
                fv.trajectories[1].m_FuturePos = fv.trajectories[0].m_FuturePos;
                tCount++;
            }

            if (tCount == 2) 
            {
                fv.trajectories[2].m_FuturePos = fv.trajectories[1].m_FuturePos;
                tCount++;
            }
            
        }

        private void AddVector3ToFeatureVector(float[] fv, Vector3 v, ref int i) 
        {
            fv[i++] = v.x;
            fv[i++] = v.y;
            fv[i++] = v.z;
        }

        private void AddQuaternianToFeatureVector(float[] fv, Quaternion q, ref int i)
        {

        }

        private void AddVector2ToFeatureVector(float[] fv, Vector2 v, ref int i)
        {

        }

    }
}