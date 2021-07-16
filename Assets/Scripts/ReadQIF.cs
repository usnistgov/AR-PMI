using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Xml.Serialization;
using System.Xml;
using System;

[RequireComponent(typeof(ReadX3D))]
public class ReadQIF : MonoBehaviour
{
    public string qifFile = "3.0/QIF30_BoxResults_19_samples_May20.QIF";
    List<CharacteristicItem> characteristicItemList;
    List<CharacteristicActual> characteristicActualList;
    Dictionary<string, Annotation> annotationDict = new Dictionary<string, Annotation>();
    Dictionary<string, Color> colorDict = new Dictionary<string, Color>();
    ReadX3D x3dScript;
    public List<Annotation> annotationList = new List<Annotation>();

    class CharacteristicItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string KeyCharacteristic { get; set; }
        public string FeatureItemId { get; set; }
        public string MeasurmentDeviceId { get; set; }
        public string CharacteristicNominalId { get; set; }

        public CharacteristicItem(string id, string name, string keyCharacteristic, string featureItemId, string measurmentDeviceId, string characteristicNominalId)
        {
            this.Id = id;
            this.Name = name;
            this.KeyCharacteristic = keyCharacteristic;
            this.FeatureItemId = featureItemId;
            this.MeasurmentDeviceId = measurmentDeviceId;
            this.CharacteristicNominalId = characteristicNominalId;
        }

        override public string ToString()
        {
            return "CharacteristicItem Id: " + Id + " Name: " + Name + " KeyCharacteristic: " + KeyCharacteristic + " FeatureItemId: " + FeatureItemId +
                   " MeasurmentDeviceId: " + MeasurmentDeviceId + " CharacteristicNominalId: " + CharacteristicNominalId;
        }
    }

    class CharacteristicActual
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string CharacteristicItemId { get; set; }
        public string Value { get; set; }

        public CharacteristicActual(string id, string status, string characteristicItemId, string value)
        {
            this.Id = id;
            this.Status = status;
            this.CharacteristicItemId = characteristicItemId;
            this.Value = value;
        }

        override public string ToString()
        {
            return "CharacteristicActual Id: " + Id + " CharacteristicItemId: " + CharacteristicItemId + " Status: " + Status + " Value: " + Value;
        }

    }


    void Start()
    {
        try
        {
            x3dScript = gameObject.GetComponent<ReadX3D>();
        }
        catch
        {
            Debug.LogError("Unable to find the ReadX3D.cs script.");
        }
        

        string filePath = Application.streamingAssetsPath + "/QIF/" + qifFile;
        characteristicItemList = new List<CharacteristicItem>();
        characteristicActualList = new List<CharacteristicActual>();

        string xml = ReadXML(filePath);
        ParseQIF(xml);

        GenerateDictionary();
        AssignColors();
        HighlightResults();

    }

    void Update()
    {

    }

    string ReadXML(string filePath)
    {
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(filePath);
        unityWebRequest.SendWebRequest();
        while (!unityWebRequest.isDone)
        {
            //Waiting for request
        }

        return unityWebRequest.downloadHandler.text;
    }

    void ParseQIF(string xml)
    {
        XmlDocument xmlDoc = new XmlDocument();
        XmlNode characteristicItemsNode, measurementResultsSetNode;

        xmlDoc.LoadXml(xml);

        XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        xmlnsManager.AddNamespace("def", "http://qifstandards.org/xsd/qif3");

        characteristicItemsNode = xmlDoc.SelectSingleNode("//def:CharacteristicItems", xmlnsManager);

        measurementResultsSetNode = xmlDoc.SelectSingleNode("//def:Results/def:MeasurementResultsSet", xmlnsManager);

        ParseCharacteristicItems(characteristicItemsNode, xmlnsManager);

        ParseMeasurementResultsSet(measurementResultsSetNode, xmlnsManager);

    }

    void ParseCharacteristicItems(XmlNode characteristicItemsNode, XmlNamespaceManager xmlnsManager)
    {
        XmlNodeList characteristicItemsList = characteristicItemsNode.SelectNodes("*"); //direct children

        foreach (XmlNode characteristicItem in characteristicItemsList)
        {
            ParseCharacteristicItem(characteristicItem, xmlnsManager);
        }
    }

    CharacteristicItem ParseCharacteristicItem(XmlNode characteristicItemNode, XmlNamespaceManager xmlnsManager)
    {
        string id = "", name = "", keyCharacteristic = "", featureItemId = "", measurmentDeviceId = "", characteristicNominalId = "";
        XmlNode nameNode, keyCharacteristicNode, featureItemIdNode, measurmentDeviceIdNode, characteristicNominalIdNode;

        if (characteristicItemNode.Attributes["id"] != null)
        {
            id = characteristicItemNode.Attributes["id"].Value;
        }

        nameNode = characteristicItemNode.SelectSingleNode("def:Name", xmlnsManager);

        if (nameNode != null)
            name = nameNode.InnerText;

        keyCharacteristicNode = characteristicItemNode.SelectSingleNode("def:KeyCharacteristic/def:Designator", xmlnsManager);

        if (keyCharacteristicNode != null)
            keyCharacteristic = keyCharacteristicNode.InnerText;

        featureItemIdNode = characteristicItemNode.SelectSingleNode("def:FeatureItemIds/def:Id", xmlnsManager);

        if (featureItemIdNode != null)
            featureItemId = featureItemIdNode.InnerText;

        measurmentDeviceIdNode = characteristicItemNode.SelectSingleNode("def:MeasurementDeviceIds/def:Id", xmlnsManager);

        if (measurmentDeviceIdNode != null)
            measurmentDeviceId = measurmentDeviceIdNode.InnerText;

        characteristicNominalIdNode = characteristicItemNode.SelectSingleNode("def:CharacteristicNominalId", xmlnsManager);

        if (characteristicNominalIdNode != null)
            characteristicNominalId = characteristicNominalIdNode.InnerText;


        CharacteristicItem characteristicItem = new CharacteristicItem(id, name, keyCharacteristic, featureItemId, measurmentDeviceId, characteristicNominalId);
        characteristicItemList.Add(characteristicItem);

        return characteristicItem;
    }

    void ParseMeasurementResultsSet(XmlNode measurementResultsSetList, XmlNamespaceManager xmlNamespaceManager)
    {
        XmlNodeList measuredCharacteristicsList;
        //measuredCharacteristicsList = measurementResultsSetList.SelectNodes("def:MeasurementResults/def:MeasuredCharacteristics/def:CharacteristicActuals", xmlNamespaceManager);
        measuredCharacteristicsList = measurementResultsSetList.SelectNodes("def:MeasurementResults/def:MeasuredCharacteristics/def:CharacteristicMeasurements", xmlNamespaceManager);

        foreach (XmlNode measuredCharacteristics in measuredCharacteristicsList)
        {
            ParseMeasuredCharacteristicsNode(measuredCharacteristics, xmlNamespaceManager);
        }
    }

    void ParseMeasuredCharacteristicsNode(XmlNode measuredCharacteristics, XmlNamespaceManager xmlNamespaceManager)
    {
        XmlNodeList characteristicActualList = measuredCharacteristics.SelectNodes("*");
        foreach (XmlNode characteristicActualNode in characteristicActualList)
        {
            ParseCharacteristicActualNode(characteristicActualNode, xmlNamespaceManager);
        }
    }
    CharacteristicActual ParseCharacteristicActualNode(XmlNode characteristicActualNode, XmlNamespaceManager xmlNamespaceManager)
    {
        string id = "", status = "", characteristicItemId = "", value = "";
        XmlNode statusNode, characteristicItemIdNode, valueNode;

        if (characteristicActualNode.Attributes["id"] != null)
        {
            id = characteristicActualNode.Attributes["id"].Value;
        }

        statusNode = characteristicActualNode.SelectSingleNode("def:Status/def:CharacteristicStatusEnum", xmlNamespaceManager);

        if (statusNode != null)
            status = statusNode.InnerText;

        characteristicItemIdNode = characteristicActualNode.SelectSingleNode("def:CharacteristicItemId", xmlNamespaceManager);

        if (characteristicItemIdNode != null)
            characteristicItemId = characteristicItemIdNode.InnerText;

        valueNode = characteristicActualNode.SelectSingleNode("def:Value", xmlNamespaceManager);

        if (valueNode != null)
            value = valueNode.InnerText;

        CharacteristicActual characteristicActual = new CharacteristicActual(id, status, characteristicItemId, value);

        characteristicActualList.Add(characteristicActual);

        return characteristicActual;
    }

    public void HighlightResults()
    {
        GameObject parent = this.gameObject;

        GameObject qifObject = new GameObject("QIF");
        qifObject.transform.SetParent(parent.transform);
        //qifObject.transform.localScale = this.gameObject.transform.localScale; // Get scale of annotations
        qifObject.transform.localScale = new Vector3(1, 1, 1);
        qifObject.transform.localPosition = new Vector3(0, 0, 0);
        qifObject.transform.rotation = this.gameObject.transform.rotation;

        GameObject failsObject = new GameObject("Fails");
        failsObject.transform.SetParent(qifObject.transform);
        HelperFunctions.ResetTransform(failsObject);


        GameObject passesObject = new GameObject("Passes");
        passesObject.transform.SetParent(qifObject.transform);
        HelperFunctions.ResetTransform(passesObject);

        GameObject inconclusiveObject = new GameObject("Inconclusive");
        inconclusiveObject.transform.SetParent(qifObject.transform);
        HelperFunctions.ResetTransform(inconclusiveObject);

        foreach (KeyValuePair<string, Color> assignedColor in colorDict)
        {
            if (annotationDict.ContainsKey(assignedColor.Key))
            {

                if (assignedColor.Value.g == 0)
                {
                    annotationDict[assignedColor.Key].annotationObject.transform.SetParent(failsObject.transform);
                }
                else if (assignedColor.Value.r == 0)
                {
                    annotationDict[assignedColor.Key].annotationObject.transform.SetParent(passesObject.transform);
                }
                else
                {
                    annotationDict[assignedColor.Key].annotationObject.transform.SetParent(inconclusiveObject.transform);
                }

                //annotationDict[assignedColor.Key].annotationObject.transform.localPosition = new Vector3(0, 0, 0);
                //annotationDict[assignedColor.Key].annotationObject.transform.localScale = new Vector3(1, 1, 1);
                //annotationDict[assignedColor.Key].annotationObject.transform.localRotation = new Quaternion(0, 0, 0, 0);
                HelperFunctions.ResetTransform(annotationDict[assignedColor.Key].annotationObject);

                try
                {
                    annotationDict[assignedColor.Key].annotationObject.gameObject.GetComponent<MeshRenderer>().material.color = assignedColor.Value;
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to assign a color to " + assignedColor.Key + ": " + e.Message);
                }
            }
        }


    }
    void GenerateDictionary()
    {
        for (int i = 0; i < x3dScript.annotationList.Count; i++)
        {
            for (int j = 0; j < characteristicItemList.Count; j++)
            {
                string[] split = characteristicItemList[j].Name.Split('.'); // If WIDTH6.3 -> only look for WIDTH6

                if (x3dScript.annotationList[i].name.ToUpper().Contains(split[0])
                    && !annotationDict.ContainsKey(characteristicItemList[j].Name))
                {
                    GameObject x3dAnnotation = x3dScript.annotationList[i].annotationObject;
                    GameObject newAnnotationInstance = Instantiate(x3dAnnotation, x3dAnnotation.transform.position, x3dAnnotation.transform.rotation);
                    
                    
                    Annotation qifAnnoatation = new Annotation(characteristicItemList[j].Id, "QIF Annotation", characteristicItemList[j].Name, newAnnotationInstance);
                    annotationList.Add(qifAnnoatation);
                    
                    if(x3dScript.annotationList[i].surface != null)
                    {
                        qifAnnoatation.SetSurface(x3dScript.annotationList[i].surface);
                    }

                    newAnnotationInstance.transform.localScale = new Vector3(1, 1, 1);
                    newAnnotationInstance.name = qifAnnoatation.id + " | " + qifAnnoatation.description + " | " + qifAnnoatation.name;

                    annotationDict.Add(characteristicItemList[j].Name, qifAnnoatation);
                }
            }
        }
    }

    void AssignColors()
    {
        Color color = new Color(1, 1, 1);
        for (int i = 0; i < characteristicItemList.Count; i++)
        {
            int passCount = 0, failCount = 0;

            for (int j = 0; j < characteristicActualList.Count; j++)
            {
                if (characteristicActualList[j].CharacteristicItemId == characteristicItemList[i].Id)
                {
                    if (characteristicActualList[j].Status == "PASS")
                        passCount++;
                    else
                        failCount++;
                }
            }

            //float red = 2 * ((float)failCount / (float)(passCount + failCount));
            //float green = 2 * ((float)passCount / (float)(passCount + failCount));

            if (passCount == 0)
                color = new Color(1, 0, 0);
            else if (failCount == 0)
                color = new Color(0, 1, 0);
            else
                color = new Color(1, 1, 0);

            colorDict.Add(characteristicItemList[i].Name, color);

            //Debug.Log("Characterisitc " + characteristicItemList[i].Name + ", ID: " + characteristicItemList[i].Id + ". (FAILS: " + failCount + " / PASSES: " + passCount + ")");
        }
    }
}