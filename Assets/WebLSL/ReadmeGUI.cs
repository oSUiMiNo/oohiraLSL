using Mirror;
using UnityEngine;
using NobleConnect.Mirror;
using UnityEditor.Experimental.GraphView;

public class ReadmeGUI : MonoBehaviour
{
    public TextAsset textFile;
    public Texture2D textBackground;
    string text;

    [SerializeField] Vector2 position = new Vector2(920, 230);
    [SerializeField] int fontSize = 35;
    [SerializeField] RectInt padding = new RectInt(10, 10, 10, 10);
    [SerializeField] Color textColor = Color.white;
    [SerializeField] RectType rectType = RectType.NativeRect;
    [SerializeField] Vector2 textRect = new Vector2(965, 810);

    void Start()
    {
        text = textFile.text;
    }

    void OnGUI()
    {
        if (!NobleServer.active && !NetworkClient.active)
        { 
            var style = new GUIStyle("label");
            style.fontSize = fontSize;
            style.normal.textColor = textColor;
            style.padding = new RectOffset(padding.x, padding.y, padding.width, padding.height);
            style.normal.background = textBackground;
                
            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(text), style);

            if(rectType == RectType.NativeRect)
                GUI.Label(new Rect(position.x, position.y, labelRect.width, labelRect.height), text, style);
            else
                GUI.Label(new Rect(position.x, position.y, textRect.x, textRect.y), text, style);
        }
    }
}

enum RectType
{
    NativeRect,
    CustomeRect
}