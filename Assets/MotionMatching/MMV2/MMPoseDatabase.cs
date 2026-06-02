using UnityEngine;

namespace MMSystemV2
{
    public class MMPoseDatabase : MonoBehaviour
    {
        public void CreateDatabase(MMCharacterProfile characterProfile)
        {
            if (!ValidateProfile(characterProfile))
                return;

            StorePosiitonData(characterProfile);
        }

        private void StorePosiitonData(MMCharacterProfile profile) 
        {
            int currentPoseIndex = 0;
            int poseCount = GetPoseCount(profile);

            profile.m_Poses = new MMPose[poseCount];

            foreach (AnimationClip clip in profile.m_Animations)
            {
                float frameTime = 0.0f;
                int frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);

                Vector3 startingPosition = new Vector3();

                for (int frame = 0; frame < frameCount; frame++) 
                {
                    frameTime = frame / clip.frameRate;
                    clip.SampleAnimation(profile.m_GameObject, frameTime);

                    profile.m_Poses[currentPoseIndex] = new MMPose();
                    profile.m_Poses[currentPoseIndex].m_Bones = new MMBone[profile.m_FeatureBones.Length];

                    StoreBones(profile, currentPoseIndex);
                    StoreDeltaPositions(profile, frame, currentPoseIndex, ref startingPosition);

                    currentPoseIndex++;
                }
            }
        }

        private void StoreBones(MMCharacterProfile profile, int poseIndex) 
        {
            for (int i = 0; i < profile.m_FeatureBones.Length; i++)
            {
                MMBone currentBone = new MMBone();

                currentBone.m_Name = profile.m_FeatureBones[i].name;
                currentBone.m_Position = profile.m_GameObject.transform.InverseTransformPoint(profile.m_FeatureBones[i].position);
                currentBone.m_Rotation = profile.m_FeatureBones[i].localRotation;

                profile.m_Poses[poseIndex].m_Bones[i] = currentBone;
            }
        }

        public void CreateFeatureVector(MMCharacterProfile characterProfile)
        {

        }

        private void StoreDeltaPositions(MMCharacterProfile profile, int currentFrame, int currentPoseIndex, ref Vector3 startingPos) 
        {
            if (currentFrame == 0)
            {
                startingPos = profile.m_GameObject.transform.position;
                profile.m_Poses[currentPoseIndex].m_DeltaPosition = Vector2.zero;
                profile.m_Poses[currentPoseIndex].m_RelativePosition = profile.m_GameObject.transform.position - startingPos;
            }
            else if (currentFrame == 1)
            {
                profile.m_Poses[currentPoseIndex].m_DeltaPosition = CalculateDeltaPositions(startingPos, profile, currentPoseIndex);
                profile.m_Poses[currentPoseIndex - 1].m_DeltaPosition = profile.m_Poses[currentPoseIndex].m_DeltaPosition;
            }
            else
            {
                profile.m_Poses[currentPoseIndex].m_DeltaPosition = CalculateDeltaPositions(startingPos, profile, currentPoseIndex);
            }
        }

        private Vector2 CalculateDeltaPositions(Vector3 startPos, MMCharacterProfile profile, int poseIndex) 
        {
            Vector3 currentPos = profile.m_GameObject.transform.position;
            Vector3 previousRelativePosition = profile.m_Poses[poseIndex - 1].m_RelativePosition;
            profile.m_Poses[poseIndex].m_RelativePosition = currentPos - startPos;

            Vector3 Delta = profile.m_GameObject.transform.InverseTransformDirection((currentPos - startPos) - previousRelativePosition);
            return new Vector2(Delta.x, Delta.z);
        }

        private bool ValidateProfile(MMCharacterProfile profile)
        {
            if (profile.m_FeatureBones == null) 
            {
                Debug.LogError("Missing Feature Bones");
                return false;
            }

            if(profile.m_Animations == null)
            {
                Debug.LogError("Missing Animations");
                return false;
            }

            if (profile.m_Anim == null)
            {
                Debug.LogError("Missing Animator");
                return false;
            }

            return true;
        }

        private int GetPoseCount(MMCharacterProfile profile) 
        {
            int count = 0;

            foreach (AnimationClip clip in profile.m_Animations) 
            {
                count += (int)(clip.length * clip.frameRate);
            }

            return count;
        }
    }
}