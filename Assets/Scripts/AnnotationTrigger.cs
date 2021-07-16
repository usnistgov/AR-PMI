using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnnotationTrigger : MonoBehaviour
{
    ReadX3D x3dScript;
    ReadQIF qifScript;
    List<Annotation> annotationList = new List<Annotation>();
    void Start()
    {
        GameObject parentObject;
        string[] splitName = this.gameObject.name.Split('|');
        if (splitName.Length > 2 && splitName[1].Trim() == "QIF Annotation")
            parentObject = this.gameObject.transform.parent.parent.parent.gameObject;
        else
            parentObject = this.gameObject.transform.parent.parent.gameObject;

        try
        {
            x3dScript = parentObject.GetComponent<ReadX3D>();
        }
        catch
        {
            x3dScript = null;
            Debug.LogError("Unable to find the ReadX3D.cs script.");
        }

        try
        {
            qifScript = parentObject.GetComponent<ReadQIF>();
        }
        catch
        {
            qifScript = null;
            Debug.LogError("Unable to find the ReadQIF.cs script.");
        }

        if (splitName.Length > 2 && splitName[1].Trim() == "QIF Annotation" && qifScript != null)
        {
            annotationList = qifScript.annotationList;
        }
        else if (x3dScript != null)
        {
            annotationList = x3dScript.annotationList;
        }
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {

            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                //print("Touch has Began");
            }
            if (Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                RaycastHit raycastHit;
                if (Physics.Raycast(raycast, out raycastHit))
                {
                    if (raycastHit.collider.name == this.gameObject.name)
                    {
                        this.OnMouseDown();    
                    }
                }
            }
        }
    }
    private void OnMouseDown()
    {
        List<int> indexes = new List<int>();

        //Debug.Log("TRIGGERED " + this.gameObject.name);
        string[] splitName = this.gameObject.name.Split('|');

        if (splitName.Length > 2)
            indexes = FindAnnotations(splitName[2]);
        else if (splitName.Length == 1)
            indexes = FindSurfaces(splitName[0]);

        //TODO: fix this. In QIF multiple annotations use the same reference for a surface. This gives errors.
        foreach (int index in indexes)
        {
            if (annotationList[index].surface != null)
            {
                if (annotationList[index].surface.GetComponent<MeshRenderer>().material.renderQueue != 1)
                {
                    ShowAnnotations();
                }
                else
                {
                    HideAnnotations(index);
                }
            }
        }
    }

    List<int> FindAnnotations(string annotationName)
    {
        List<int> indexes = new List<int>();

        for (int i = 0; i < annotationList.Count; i++)
        {
            if (annotationList[i].name.ToUpper().Split('.')[0].Trim() == annotationName.ToUpper().Split('.')[0].Trim())
            {
                indexes.Add(i);
            }
        }

        return indexes;
    }
    List<int> FindSurfaces(string annotationName)
    {
        List<int> indexes = new List<int>();
        for (int i = 0; i < annotationList.Count; i++)
        {
            if (annotationList[i].surface != null)
            {
                if (annotationList[i].surface.name.ToUpper().Trim() == annotationName.ToUpper().Trim())
                {
                    indexes.Add(i);
                }
            }

        }

        return indexes;
    }

    void HideAnnotations(int exceptionIndex)
    {
        Debug.Log("Hiding annotations.");
        for (int i = 0; i < annotationList.Count; i++)
        {
            if (i != exceptionIndex)
            {
                annotationList[i].annotationObject.SetActive(false);

                if (annotationList[i].surface != null)
                {
                    annotationList[i].surface.GetComponent<MeshRenderer>().material.renderQueue = 1;
                    annotationList[i].surface.GetComponent<MeshRenderer>().enabled = false;
                }
            }
            else
            {
                if (annotationList[i].surface != null)
                {
                    annotationList[i].surface.GetComponent<MeshRenderer>().material.renderQueue = 2000;
                    annotationList[i].surface.GetComponent<MeshRenderer>().enabled = true;
                }
                else
                    Debug.Log("No surface.");
            }
        }
    }

    void ShowAnnotations()
    {
        Debug.Log("Showing annotations.");
        for (int i = 0; i < annotationList.Count; i++)
        {
            annotationList[i].annotationObject.SetActive(true);
            if (annotationList[i].surface != null)
            {
                annotationList[i].surface.GetComponent<MeshRenderer>().material.renderQueue = 1;
                annotationList[i].surface.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }
}
