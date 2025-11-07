using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Readme", menuName = "Custom/Readme")]
public class Readme : ScriptableObject
{
    public Texture2D icon;
    public string title;
    public Section[] sections = new Section[0];
    public bool loadedLayout;

    [Serializable]
    public class Section
    {
        public string heading, text, linkText, url;
    }
}
