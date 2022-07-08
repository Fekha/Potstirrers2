using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    internal static Route i;
    internal List<Tile> FullRoute = new List<Tile>();
    public List<Transform> childNodeList = new List<Transform>();
    private void Awake()
    {
        i = this;
    }
    void Start()
    {
        CreateFullRoute();
    }
    void CreateFullRoute()
    {
        var startNodeIndex = 0;
        for (int i = 0; i < childNodeList.Count; i++)
        {
            int tempPos = startNodeIndex + i;
            tempPos %= childNodeList.Count;
            FullRoute.Add(childNodeList[tempPos].GetComponent<Tile>());
        }
    }
}
