using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCubeScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        List<int> test = new List<int>();
        test.Add(0);
        test.Add(1);
        test.Add(2);
        test.Add(3);
        test.Add(4);
        test.Add(5);
        test.Add(6);
        test.Add(7);
        test.Add(8);
        test.Add(9);
        Debug.Log($"Range of list first: {test.Count}");
        Debug.Log($"el3: {test[3]}");
        test.RemoveRange(2,2);
        Debug.Log($"el3: {test[3]}");
        Debug.Log($"Range of list after: {test.Count}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
