using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LockTextRotation : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro textMeshPro;

    [SerializeField]
    private Transform parent;

    // Start is called before the first frame update
    void Start()
    {
        textMeshPro = GetComponent<TextMeshPro>();
        parent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
//        textMeshPro.text = parent.GetComponent<PlayerFireController>().fireState.ToString();
        transform.rotation = Quaternion.identity;
    }
}
