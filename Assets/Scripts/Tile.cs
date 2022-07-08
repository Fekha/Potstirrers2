using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool isDangerZone;
    public bool hasSpoon;
    public bool hasSpatula;
    public Stack<Ingredient> ingredients = new Stack<Ingredient>();

    //[CustomEditor(typeof(Tile))]
    //public class StackPreview : Editor
    //{
    //    public override void OnInspectorGUI()
    //    {

    //        // get the target script as TestScript and get the stack from it
    //        var ts = (Tile)target;
    //        var stack = ts.ingredients;

    //        // some styling for the header, this is optional
    //        var bold = new GUIStyle();
    //        bold.fontStyle = FontStyle.Bold;
    //        foreach (var s in stack)
    //        {
    //            GUILayout.Label("Top Ingredient " + s, bold);
    //        }
            
    //    }
    //}
}
