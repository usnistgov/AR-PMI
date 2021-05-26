using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class TrackingScript : MonoBehaviour
{

    //public GameObject PMIObject1, PMIObject2, PMIObject3;

    public GameObject toggleContainer;
    public GameObject toggleContainerQIF;
    public GameObject textProductDefinition, textInspectionData;
    public Toggle toggle;
    public Toggle toggleQIF;
    GameObject currentlyTrackedObject = null;
    GameObject oldObject;
    public Button buttonViewQIF;
    private TrackableBehaviour mTrackableBehaviour;

    bool trackingChanged = false;

    void Start()
    {
        mTrackableBehaviour = GetComponent<TrackableBehaviour>();
        //buttonViewQIF.onClick.AddListener(ShowQIF);
    }

    // Update is called once per frame
    void Update()
    {


        OnTrackingFound();

        if (!(mTrackableBehaviour.CurrentStatus == TrackableBehaviour.Status.TRACKED || mTrackableBehaviour.CurrentStatus == TrackableBehaviour.Status.EXTENDED_TRACKED))
            trackingChanged = true;

    }

    void GenerateViewsToggle()
    {
        textProductDefinition.SetActive(true);
        DeletePreviousToggles(toggleContainer);

        for (int i = 0; i < currentlyTrackedObject.transform.childCount; i++)
        {
            string name = currentlyTrackedObject.transform.GetChild(i).name;
            if (name == "QIF")
                continue;

            Toggle newToggle = Instantiate(toggle, toggle.transform.parent);
            newToggle.gameObject.SetActive(true);
            newToggle.GetComponentInChildren<Text>().text = currentlyTrackedObject.transform.GetChild(i).transform.name;
            newToggle.transform.position += new Vector3(0, i * -40, 0);

            newToggle.onValueChanged.AddListener(delegate
            {
                SwitchView(newToggle.isOn, newToggle.GetComponentInChildren<Text>().text);
            });

            if (currentlyTrackedObject.transform.GetChild(i).transform.name == "swPart0") //TODO: fix to not be hardcoded
            {
                for(int j=0; j< currentlyTrackedObject.transform.GetChild(i).transform.childCount; j++)
                {
                    name = currentlyTrackedObject.transform.GetChild(i).transform.GetChild(j).name;
                    Toggle newToggle2 = Instantiate(toggle, toggle.transform.parent);
                    newToggle2.gameObject.SetActive(true);
                    if (name == "Face")
                        newToggle2.GetComponentInChildren<Text>().text = "Geometry";
                    else if (name == "Line")
                        newToggle2.GetComponentInChildren<Text>().text = "Edges";
                    else if (name == "QIF")
                        continue;
                    else
                        newToggle2.GetComponentInChildren<Text>().text = name;

                    newToggle2.transform.position += new Vector3(40, (currentlyTrackedObject.transform.childCount + j) * -40, 0);

                    newToggle2.onValueChanged.AddListener(delegate
                    {
                        SwitchView(newToggle2.isOn, name, newToggle.GetComponentInChildren<Text>().text);
                    });
                }
            }
        }
    }

    void GenerateQIFToggle()
    {
        textInspectionData.SetActive(true);
        DeletePreviousToggles(toggleContainerQIF);

       
        GameObject qifObject = currentlyTrackedObject.transform.Find("QIF").gameObject;

        for (int i = 0; i < qifObject.transform.childCount; i++)
        {

            Toggle newToggle = Instantiate(toggleQIF, toggleQIF.transform.parent);
            newToggle.gameObject.SetActive(true);
            newToggle.GetComponentInChildren<Text>().text = qifObject.transform.GetChild(i).transform.name;
            //qifObject.transform.GetChild(i).gameObject.SetActive(false); //disable QIF by default
            newToggle.transform.position += new Vector3(0, i * -40, 0);
            //newToggle.isOn = false;

            newToggle.onValueChanged.AddListener(delegate
            {
                SwitchQIFView(newToggle.isOn, newToggle.GetComponentInChildren<Text>().text);
            });
        }
    }
    void DeletePreviousToggles(GameObject toggleContainer)
    {
        for (int i = 1; i < toggleContainer.transform.childCount; i++)
        {
            Destroy(toggleContainer.transform.GetChild(i).gameObject);
        }

        //reset annotation visibility
        for (int i = 0; i < currentlyTrackedObject.transform.childCount; i++)
        {
            currentlyTrackedObject.transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    void SwitchView(bool toggleValue, string toggleText, string parent="")
    {
        if(parent=="")
            currentlyTrackedObject.transform.Find(toggleText).gameObject.SetActive(toggleValue);
       else
            currentlyTrackedObject.transform.Find(parent).Find(toggleText).gameObject.SetActive(toggleValue);
    }

    void SwitchQIFView(bool toggleValue, string toggleText, string parent = "")
    {
        GameObject qifObject = currentlyTrackedObject.transform.Find("QIF").gameObject;

        if (parent == "")
            qifObject.transform.Find(toggleText).gameObject.SetActive(toggleValue);
        else
            qifObject.transform.Find(parent).Find(toggleText).gameObject.SetActive(toggleValue);
    }

    private void OnTrackingFound()
    {
        if (mTrackableBehaviour && 
           (mTrackableBehaviour.CurrentStatus == TrackableBehaviour.Status.TRACKED || mTrackableBehaviour.CurrentStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
           && trackingChanged)
        {
            currentlyTrackedObject = mTrackableBehaviour.gameObject.transform.Find("X3D-PMI").gameObject;
            GenerateViewsToggle();
            GenerateQIFToggle();
            trackingChanged = false;
        }    
    }
}
