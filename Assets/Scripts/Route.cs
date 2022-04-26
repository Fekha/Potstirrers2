using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    Transform[] childNodes;

    public List<Transform> childNodeList = new List<Transform>();
    // Start is called before the first frame update
    void Start()
    {
        FillNodes();
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    FillNodes();

    //    for (int i = 0; i < childNodeList.Count; i++)
    //    {
    //        Vector3 pos = childNodeList[i].position;
    //        if (i > 0)
    //        {
    //            Vector3 prev = childNodeList[i - 1].position;
    //            Gizmos.DrawLine(prev, pos);
    //        }
    //    }
    //}
    void FillNodes()
    {
        childNodeList.Clear();
        childNodes = GetComponentsInChildren<Transform>();
        foreach(Transform child in childNodes)
        {
            if(child != this.transform)
            {
                childNodeList.Add(child);
            }
        }
    }

    public int RequestPosition(Transform tileTransform)
    {
        return childNodeList.IndexOf(tileTransform);
    }
}
