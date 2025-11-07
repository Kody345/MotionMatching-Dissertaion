using UnityEngine;

namespace MMSystem
{
    public class PoseDataBase
    {
        private SearchAndSaveType m_SaveType;

        public void SaveDataType(SearchAndSaveType type) { m_SaveType = type; }

        public bool SaveData() 
        {
            return true;
        }


    }
}