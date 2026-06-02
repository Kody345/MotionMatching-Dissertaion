using JetBrains.Annotations;
using System;
using UnityEngine;

namespace MMSystem
{
    public class PoseDataBase
    {
        private Transform[] allBones;
        private Transform LFoot;
        private Transform RFoot;
        private Transform Root;
        float fullTime = 0.0f;

        public float[] m_mean;
        public float[] m_stdDevs;

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

            SetupVel(m_features, m_poses);
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
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentHipVel, ref c);
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentLFootPos, ref c);
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentRFootPos, ref c);

                //Trajectories
                for (int j = 0; j < 3; j++)
                {
                    AddVector3ToFeatureVector(m_features[i].m_FeatureVector, (Vector3)m_features[i].trajectories[j].m_FuturePos, ref c);
                }

            }

            for (int i = 0; i < m_features.Length; i++) 
            {
                m_poses[m_features[i].poseIndex].feature = m_features[i];
            }

        }

        public void ApplyWeightings(Pose[] poses, float[] weights)
        {
            // Layout: [0..8] vel (3 bones * 3), [9..14] pos (2 bones * 3), [15..23] traj (3 * 3)
            foreach (Pose p in poses)
            {
                for (int i = 0; i < 9; i++) p.feature.m_FeatureVector[i] *= weights[0]; // velocity
                for (int i = 9; i < 15; i++) p.feature.m_FeatureVector[i] *= weights[1]; // pose
                for (int i = 15; i < 24; i++) p.feature.m_FeatureVector[i] *= weights[2]; // trajectory
            }
        }

        public void NormalizeVector(FeatureVector[] features) 
        {
            //Calc mean and stadard deviation
            //Use to normalize the vector

            int vectorLength = features[0].m_FeatureVector.Length;
            float[] means = new float[vectorLength];
            float[] stdDevs = new float[vectorLength];

            // Compute per-channel mean
            for (int ch = 0; ch < vectorLength; ch++)
            {
                float sum = 0f;
                for (int i = 0; i < features.Length; i++)
                    sum += features[i].m_FeatureVector[ch];
                means[ch] = sum / features.Length;
            }

            // Compute per-channel std dev
            for (int ch = 0; ch < vectorLength; ch++)
            {
                float variance = 0f;
                for (int i = 0; i < features.Length; i++)
                {
                    float diff = features[i].m_FeatureVector[ch] - means[ch];
                    variance += diff * diff;
                }
                //Fix this, makes the trajectories wonky
                
                stdDevs[ch] = Mathf.Sqrt(variance / features.Length) + 0.00001f;
            }

            // Normalize each feature vector
            for (int i = 0; i < features.Length; i++)
                for (int ch = 0; ch < vectorLength; ch++)
                    features[i].m_FeatureVector[ch] = (features[i].m_FeatureVector[ch] - means[ch]) / stdDevs[ch];

            // Store so the query vector can be normalized the same way at runtime
            m_mean = means;
            m_stdDevs = stdDevs;

            for (int i = 0; i < features.Length; i++) 
            {
                features[i].m_mean = new float[24];
                features[i].m_mean = means;
                features[i].m_stdDevs = new float[24];
                features[i].m_stdDevs = stdDevs;
            }

            /*for (int i = 0; i < features.Length; i++)
            {
                for (int j = 0; j < features[i].m_FeatureVector.Length; j++)
                {
                    features[i].m_FeatureVector[j] /= mag;
                }
            }*/
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

        private void SetupVel(FeatureVector[] fv, MMSystem.Pose[] poses)
        {
            float dt = 1f / 30f;

            for (int i = 0; i < fv.Length; i++)
            {
                bool isLastFrame = i >= fv.Length - 1;
                bool isClipBoundary = !isLastFrame &&
                                      poses[i + 1].m_Clip.name != poses[i].m_Clip.name;

                if (isLastFrame || isClipBoundary)
                {
                    // Don't bleed across clips — copy previous frame's velocity
                    fv[i].m_CurrentLFootVel = fv[i - 1].m_CurrentLFootVel;
                    fv[i].m_CurrentRFootVel = fv[i - 1].m_CurrentRFootVel;
                    fv[i].m_CurrentHipVel = fv[i - 1].m_CurrentHipVel;
                    continue;
                }

                fv[i].m_CurrentLFootVel = (fv[i + 1].m_CurrentLFootPos - fv[i].m_CurrentLFootPos) / dt;
                fv[i].m_CurrentRFootVel = (fv[i + 1].m_CurrentRFootPos - fv[i].m_CurrentRFootPos) / dt;
                fv[i].m_CurrentHipVel = (fv[i + 1].m_CurrentHipPos - fv[i].m_CurrentHipPos) / dt;
            }
        }

        private void SetupPos(ref FeatureVector fv, MMSystem.Pose[] poses)
        {
            Pose p = poses[fv.poseIndex];
            Vector3 rootWorldPos = Vector3.zero;
            Quaternion rootWorldRot = Quaternion.identity;

            // First pass: get root transform
            foreach (var bone in p.m_Bones)
            {
                if (bone.m_Name == Root.name)
                {
                    rootWorldPos = bone.m_BoneWorldPos;
                    rootWorldRot = p.rootRot;
                    break;
                }
            }

            Quaternion invRot = Quaternion.Inverse(rootWorldRot);

            // Second pass: compute positions in root-local space
            foreach (var bone in p.m_Bones)
            {
                if (bone.m_Name == LFoot.name)
                    fv.m_CurrentLFootPos = invRot * (bone.m_BoneWorldPos - rootWorldPos);
                else if (bone.m_Name == RFoot.name)
                    fv.m_CurrentRFootPos = invRot * (bone.m_BoneWorldPos - rootWorldPos);
                else if (bone.m_Name == Root.name)
                    fv.m_CurrentHipPos = Vector3.zero; // root is origin by definition
            }
        }

        private void Trajectory(ref FeatureVector fv, MMSystem.Pose[] poses)
        {
            fv.trajectories = new Trajectory[3];
            int[] lookaheadFrames = { 6, 12, 18 }; // 0.2s, 0.4s, 0.6s at 30fps
            string currentClip = poses[fv.poseIndex].m_Clip.name;

            Quaternion invRot = Quaternion.Inverse(poses[fv.poseIndex].rootRot);
            Vector3 accumulated = Vector3.zero;
            int tCount = 0;
            int frameOffset = 0;

            for (int i = fv.poseIndex + 1; i < poses.Length && tCount < 3; i++)
            {
                // Stop at clip boundary
                if (poses[i].m_Clip.name != currentClip)
                {
                    // Fill remaining trajectories with last known point
                    while (tCount < 3)
                        fv.trajectories[tCount++].m_FuturePos = fv.trajectories[Mathf.Max(0, tCount - 1)].m_FuturePos;
                    return;
                }

                accumulated += poses[i].deltaPos;
                frameOffset++;

                if (frameOffset == lookaheadFrames[tCount])
                {
                    // Store in character-local space, Y ignored
                    Vector3 localDelta = invRot * accumulated;
                    fv.trajectories[tCount].m_FuturePos = new Vector3(localDelta.x, 0f, localDelta.z);
                    tCount++;
                }
            }

            // Pad if clip ended before all 3 points were reached
            while (tCount < 3)
            {
                fv.trajectories[tCount] = tCount > 0
                    ? fv.trajectories[tCount - 1]
                    : new Trajectory { m_FuturePos = Vector3.zero };
                tCount++;
            }
        }

        private void AddVector3ToFeatureVector(float[] fv, Vector3 v, ref int i) 
        {
            fv[i++] = v.x;
            fv[i++] = v.y;
            fv[i++] = v.z;
        }
    }
}