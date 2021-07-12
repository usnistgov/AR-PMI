using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Text.RegularExpressions;
using System;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine.Networking;

public class ReadX3D : MonoBehaviour
{
    public float lineWidth = 0.5f;
    public string x3dFile = "NIST_MTC_CRADA_PLATE_REV-A-sfa.x3d";
    public bool flipYZ = true, combineSubmeshes = true, saveMeshes = false, addMeshColliders = true;
    IDictionary<string, string> dictionary = new Dictionary<string, string>();
    IDictionary<string, Material> matDictionary = new Dictionary<string, Material>();
    public GameObject scaleToTarget;
    int assetIncrement = 0;
    public List<Annotation> annotationList = new List<Annotation>();

    void Start()
    {
        string filePath = Application.streamingAssetsPath + "/X3D/" + x3dFile;


        string xml = ReadXML(filePath);
        ParseXML(xml);

        ScaleToTarget(); //TODO: test this more.
    }

    #region XML Parsing
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
    void ParseXML(string xml)
    {
        XmlDocument xmlDoc = new XmlDocument();
        XmlNodeList switchList;

        xmlDoc.LoadXml(xml);
        switchList = xmlDoc.SelectNodes("X3D/Scene/Switch");

        foreach (XmlNode switchNode in switchList)
        {
            GameObject viewObject = ParseSwitchNode(switchNode);
            
        }
    }

    /* Description: Creates and returns a GameObject for each <Switch> element */
    GameObject ParseSwitchNode(XmlNode switchNode)
    {
        XmlNodeList groupList, transformList;
        string switchId = "Switch";

        if(switchNode.Attributes["id"] != null)
        {
            switchId = switchNode.Attributes["id"].Value;
        }

        GameObject viewObject = new GameObject(switchId);

        groupList = switchNode.SelectNodes("Group");
        foreach (XmlNode groupNode in groupList)
        {
            //GameObject groupObject = new GameObject("Group"); //TODO: this causes issues with QIF

            //if (groupNode.Attributes["id"] != null)
            //    groupObject.name = groupNode.Attributes["id"].Value;

            //groupObject.transform.SetParent(viewObject.transform);
            //ResetTransform(groupObject);

            ParseGroupNode(groupNode, viewObject, switchId); // (x, groupObject)
        }

        viewObject.transform.SetParent(this.gameObject.transform);
        ResetTransform(viewObject);

        transformList = switchNode.SelectNodes("Transform");
        foreach(XmlNode transformNode in transformList)
        {
            ParseTransformNode(transformNode, viewObject, switchId);
        }

        

        return viewObject;
    }

    void ParseGroupNode(XmlNode groupNode, GameObject viewObject, string parentSwitchId)
    {
        XmlNodeList shapeNodeList, groupNodeList, switchList;
        GameObject geometry;
        List<CombineInstance> combineList = new List<CombineInstance>();
        List<Material> materialList = new List<Material>();
        string groupId = "Group";
        //int triangleCount = 0;

        if (groupNode.Attributes["id"] != null)
            groupId = groupNode.Attributes["id"].Value;

        string[] splitGroupId = groupId.Split('|');

        switchList = groupNode.SelectNodes("Switch");
        foreach (XmlNode switchNode in switchList)
        {
            GameObject nestedViewObject = ParseSwitchNode(switchNode);
            nestedViewObject.transform.SetParent(viewObject.transform);
            ResetTransform(nestedViewObject);
        }

        groupNodeList = groupNode.SelectNodes("Group");
        foreach (XmlNode nestedGroupNode in groupNodeList)
        {
            /*GameObject groupObject = new GameObject("Group");

            if (nestedGroupNode.Attributes["id"] != null)
                groupObject.name = nestedGroupNode.Attributes["id"].Value;

            groupObject.transform.SetParent(viewObject.transform);
            ResetTransform(groupObject);*/

            ParseGroupNode(nestedGroupNode, viewObject, parentSwitchId);
        }

        shapeNodeList = groupNode.SelectNodes("Shape");
        foreach (XmlNode shapeNode in shapeNodeList)
        {
            Tuple<Material, Mesh> shape = ParseShapeNode(shapeNode);

            if (combineSubmeshes)
            {
                CombineInstance combineInstance = new CombineInstance();
                materialList.Add(shape.Item1);
                combineInstance.mesh = shape.Item2;
                combineInstance.transform = viewObject.transform.localToWorldMatrix;
                combineList.Add(combineInstance);

                //triangleCount += shape.Item2.triangles.Length;
            }
            else
            {
                geometry = new GameObject(groupId);
                geometry.AddComponent<MeshFilter>().mesh = shape.Item2;
                geometry.AddComponent<MeshRenderer>().material = shape.Item1;
                geometry.transform.SetParent(viewObject.transform);
                ResetTransform(geometry);
            }

        }

        if (combineSubmeshes && combineList.Count > 0)
        {
            Mesh combinedMesh = new Mesh();
            geometry = new GameObject(groupId);

            if (ContainsDistinct(materialList))
            {
                //Don't combine submeshes and use different materials
                combinedMesh.CombineMeshes(combineList.ToArray(), false);
                geometry.AddComponent<MeshRenderer>().materials = materialList.ToArray();
            }
            else
            {
                //Combine submeshes and use a single material
                combinedMesh.CombineMeshes(combineList.ToArray(), true);
                geometry.AddComponent<MeshRenderer>().material = materialList[0];
            }

            //TODO: Unity has a limit to the number of vertices (65534) and triangles a mesh can have. The all annotations view exceeds this. This might be the issue.
            //Debug.Log("Individual: " + triangleCount + " Combined: " + combinedMesh.triangles.Length);
            //Debug.Log(combinedMesh.vertices.Length);
            //combinedMesh.RecalculateNormals();

            geometry.AddComponent<MeshFilter>().mesh = combinedMesh;

            geometry.transform.SetParent(viewObject.transform);

            // ADD MESH COLLIDERS. DO NOT ADD COLLIDERS TO PART GEOMETRY
            if(addMeshColliders && parentSwitchId != "geometrySwitch")
            {
                geometry.AddComponent<MeshCollider>();
                geometry.AddComponent<AnnotationTrigger>();
            }
            else if(parentSwitchId == "geometrySwitch")
            {
                geometry.GetComponent<MeshRenderer>().material.renderQueue = 2000;
            }



            ResetTransform(geometry);

            if (splitGroupId.Length > 2 && parentSwitchId.Contains("swView")) //TODO: talk to Bob about this. Is this reliable?
            {
                annotationList.Add(new Annotation(splitGroupId[0], splitGroupId[1], splitGroupId[2], geometry));
            }
            else if (parentSwitchId == "surfaceSwitch") //TODO: talk to Bob about this.
            {
                int index = FindAnnotation(geometry.name);
                if(index != -1)
                {
                    annotationList[index].SetSurface(geometry);
                }

                /*List<int> indexes = FindAnnotations(geometry.name);
                foreach(int index in indexes)
                {
                    annotationList[index].SetSurface(geometry);
                }*/

            }

            //if (saveMeshes)
                //SaveMesh(combinedMesh, "Test");
        }
    }

    /* 
     * TODO: use attributes in transform element?
     * 
     */
    void ParseTransformNode(XmlNode transformNode, GameObject viewObject, string parentSwitchId)
    {
        XmlNodeList shapeList, groupList, switchList;
        string scaleStr = "";
        Vector3 scale;

        if (transformNode.Attributes["scale"] != null)
            scaleStr = transformNode.Attributes["scale"].Value;

        scale = StringToVector3(scaleStr);

        shapeList = transformNode.SelectNodes("Shape");
        foreach(XmlNode shapeNode in shapeList)
        {
            ParseShapeNode(shapeNode);
        }

        groupList = transformNode.SelectNodes("Group");
        foreach(XmlNode groupNode in groupList)
        {
            ParseGroupNode(groupNode, viewObject, parentSwitchId);
        }

        switchList = transformNode.SelectNodes("Switch");
        foreach(XmlNode switchNode in switchList)
        {
            GameObject nestedViewObject = ParseSwitchNode(switchNode);
            nestedViewObject.transform.SetParent(this.gameObject.transform);
            ResetTransform(nestedViewObject);

            nestedViewObject.transform.localScale = scale;
        }

        viewObject.transform.localScale = scale;
        print("Setting scale of " + viewObject.name + " to " + scale.x);
    }

    /* <Shape> Description
     * The Shape node has two elements, Appearance and a geometry node, which are used to create rendered objects in the world. 
     * The Appearance node specifies the visual attributes (e.g., material and texture) to be applied to the geometry. 
     * The specified geometry node (Box, Sphere, IndexedFaceSet) is rendered with the specified appearance nodes applied.
     */
    Tuple<Material, Mesh> ParseShapeNode(XmlNode shapeNode)
    {
        XmlNode appearanceNode, indexedLineSetNode, indexedFaceSetNode;
        Mesh geometry = new Mesh();
        Material appearance;

        appearanceNode = shapeNode.SelectSingleNode("Appearance");
        if (appearanceNode != null)
        {
            appearance = ParseAppearanceNode(appearanceNode);
        }
        else
        {
            appearance = new Material(Shader.Find("Standard"));
            Debug.Log("Expected <Appearance> node, but it was not found. Using default material.");
        }

        indexedLineSetNode = shapeNode.SelectSingleNode("IndexedLineSet");
        indexedFaceSetNode = shapeNode.SelectSingleNode("IndexedFaceSet");
        if (indexedLineSetNode != null) //Line Geometry
        {
            geometry = ParseIndexedLineSet(indexedLineSetNode);
        }
        else if (indexedFaceSetNode != null) //Face Geometry
        {
            geometry = ParseIndexedFaceSet(indexedFaceSetNode);
        }
        else //Other Geometry
        {
            //TODO: could also be box, sphere and other kinds of geometry 
            Debug.Log("Expected <IndexedLineSet> or <IndexedFaceSet> geometry but none were found. Other geometries are not currently supported.");
        }

        return new Tuple<Material, Mesh>(appearance, geometry);
    }

    /* <Appearance> Description
     * The Appearance node specifies the visual properties of a geometry.
     * An Appearance element can contain one Material node. 
     * If no Material node is given, then, lighting is off (all lights are ignored during rendering of the object that references this Appearance) 
     * and the unlit object colour is (1, 1, 1). 
     * Details of the lighting model are in 4.14, Lighting model of the VRML specs.
     */
    Material ParseAppearanceNode(XmlNode appearanceNode)
    {
        XmlNode materialNode;
        Material material;

       

        if (appearanceNode.Attributes["USE"] != null)
        {
            string use = appearanceNode.Attributes["USE"].Value;

            return  matDictionary[use];
        }

        materialNode = appearanceNode.SelectSingleNode("Material");

        if (materialNode != null)
        {
            material = ParseMaterialNode(materialNode);

            if (appearanceNode.Attributes["DEF"] != null)
            {
                string def = appearanceNode.Attributes["DEF"].Value;
                matDictionary.Add(def, material);
            }
            
            return material;
        }

        return null;
    }
    /* <Material> Description
     * The Material node specifies surface material properties for associated geometry nodes and is used by the X3D lighting equations during rendering. 
     * All of the fields in the Material node range from 0.0 to 1.0.
     * HTML Encoding and Default Values:
     * <Material ambientIntensity='0.2' 
     *           diffuseColor='0.8,0.8,0.8' 
     *           emissiveColor='0,0,0' 
     *           metadata='X3DMetadataObject' 
     *           shininess='0.2' 
     *           specularColor='0,0,0' 
     *           transparency='0' ></Material>
     */
    Material ParseMaterialNode(XmlNode materialNode)
    {
        //TODO: add support for transparency X3D attribute
        //TODO: create a custom shader to fix this?
        //use diffuseColor as main color (emissive works better?)
        Color emissiveColor = new Color(0, 0, 0), diffuseColor = new Color(0.8f, 0.8f, 0.8f), specularColor = new Color(0, 0, 0);
        float shininess = 0.2f, transparency = 0;
        string emissiveColorString = "";

        if (materialNode.Attributes["diffuseColor"] != null)
        {
            string diffuseColorString = materialNode.Attributes["diffuseColor"].Value;
            diffuseColor = ParseColor(diffuseColorString);
            //Material emissiveMat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
        }
        if (materialNode.Attributes["emissiveColor"] != null)
        {
            emissiveColorString = materialNode.Attributes["emissiveColor"].Value;
            emissiveColor = ParseColor(emissiveColorString);
        }
        if (materialNode.Attributes["specularColor"] != null)
        {
            string specularColorString = materialNode.Attributes["specularColor"].Value;
            specularColor = ParseColor(specularColorString);
        }
        if (materialNode.Attributes["shininess"] != null)
        {
            string shininessString = materialNode.Attributes["shininess"].Value;
            shininess = float.Parse(shininessString);
        }

        Material material;
        material = new Material(Shader.Find("Sprites/Default"));
        if (emissiveColorString != "")
        {

            material.color = emissiveColor;
        }
        else
        {
            material = new Material(Shader.Find("Standard"));
            material.color = diffuseColor;
        }

        return material;
    }

    Mesh ParseIndexedLineSet(XmlNode indexedLineSetNode)
    {
        XmlNodeList coordinateList;
        Mesh lineMesh;
        string coordIndexString = "", lineCoordinatesString = "";

        coordIndexString += indexedLineSetNode.Attributes["coordIndex"].Value;

        coordinateList = indexedLineSetNode.SelectNodes("Coordinate");

        foreach (XmlNode coordinateNode in coordinateList)
        {
            if (coordinateNode.Attributes["point"] != null)
            {
                lineCoordinatesString += coordinateNode.Attributes["point"].Value;

                if (coordinateNode.Attributes["DEF"] != null)
                {
                    string def = coordinateNode.Attributes["DEF"].Value;
                    dictionary.Add(def, lineCoordinatesString);
                }
            }
            else if (coordinateNode.Attributes["USE"] != null)
            {
                string use = coordinateNode.Attributes["USE"].Value;
                lineCoordinatesString += dictionary[use];
            }
        }
        lineMesh = GenerateLineMesh(lineCoordinatesString, coordIndexString);

        return lineMesh;
    }

    Mesh ParseIndexedFaceSet(XmlNode indexedFaceSetNode)
    {
        XmlNodeList coordinateList;
        Mesh faceMesh;
        string coordIndexString = "", faceCoordinatesString = "";

        coordIndexString += indexedFaceSetNode.Attributes["coordIndex"].Value;

        coordinateList = indexedFaceSetNode.SelectNodes("Coordinate");

        foreach (XmlNode coordinateNode in coordinateList)
        {
            if (coordinateNode.Attributes["point"] != null)
            {
                faceCoordinatesString += coordinateNode.Attributes["point"].Value;

                if (coordinateNode.Attributes["DEF"] != null)
                {
                    string def = coordinateNode.Attributes["DEF"].Value;
                    dictionary.Add(def, faceCoordinatesString);
                }
            }
            else if (coordinateNode.Attributes["USE"] != null)
            {
                string use = coordinateNode.Attributes["USE"].Value;
                faceCoordinatesString += dictionary[use];
            }
        }

        if(faceCoordinatesString != "" && coordIndexString != "")
        {
            faceMesh = GenerateFaceMesh(faceCoordinatesString, coordIndexString);
        }
        else
        {
            faceMesh = new Mesh();
        }
        

        return faceMesh;
    }
    #endregion

    #region Unity geometry generation
    Mesh GenerateLineMesh(string vertices, string coordIndexes)
    {
        Vector3[] positions = ParseCoordinates(vertices);
        List<int> intIndexes = new List<int>();
        List<CombineInstance> combineList = new List<CombineInstance>();
        string[] indexes = coordIndexes.Split(' ');
        Mesh combinedMesh = new Mesh();

        for (int i = 0; i < indexes.Length; i++)
        {
            if (indexes[i] != "-1")
            {
                intIndexes.Add(int.Parse(indexes[i]));
            }
            else
            {
                GameObject auxObject = new GameObject();
                LineRenderer lineRenderer = auxObject.AddComponent<LineRenderer>();
                Mesh lineMesh = new Mesh();

                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.positionCount = intIndexes.Count;

                for (int j = 0; j < intIndexes.Count; j++)
                {
                    lineRenderer.SetPosition(j, positions[intIndexes[j]]);
                }

                lineRenderer.BakeMesh(lineMesh);

                CombineInstance combineInstance = new CombineInstance
                {
                    mesh = lineMesh,
                    transform = auxObject.transform.localToWorldMatrix
                };
                combineList.Add(combineInstance);

                Destroy(auxObject);

                intIndexes.Clear();
            }
        }

        combinedMesh.CombineMeshes(combineList.ToArray());

        return combinedMesh;
    }
    Mesh GenerateFaceMesh(string vertices, string triangles)
    {
        Vector3[] verticesArray = ParseCoordinates(vertices);

        string[] sArray = triangles.Split(' ');
        List<int> trianglesList = new List<int>();

        for (int i = 0; i < sArray.Length; i++)
        {
            if (int.Parse(sArray[i]) != -1)
            {
                trianglesList.Add(int.Parse(sArray[i]));
            }
        }

        if (flipYZ)
        {
            for (int i = 0; i < trianglesList.Count; i += 3)
            {
                int t = trianglesList[i + 1];
                trianglesList[i + 1] = trianglesList[i + 2];
                trianglesList[i + 2] = t;
            }
        }

        //for(int i =0; i< trianglesList.Count; i++)
        //{
        //    Debug.Log(trianglesList[i]);
        //}

        Mesh mesh = new Mesh
        {
            vertices = verticesArray,
            triangles = trianglesList.ToArray()
        };
        mesh.RecalculateNormals();

        return mesh;
    }

    #endregion

    #region Helper functions
    Vector3[] ParseCoordinates(string input)
    {
        string[] sArray = input.Split(' ');
        Vector3[] array = new Vector3[sArray.Length / 3];
        if (sArray.Length >= 3)
        {
            int j = 0;
            for (int i = 0; i < sArray.Length; i += 3)
            {
                if (flipYZ)
                {
                    array[j] = new Vector3(float.Parse(sArray[i]),
                                           float.Parse(sArray[i + 2]),
                                           float.Parse(sArray[i + 1]));
                }
                else
                {
                    array[j] = new Vector3(float.Parse(sArray[i]),
                                           float.Parse(sArray[i + 1]),
                                           float.Parse(sArray[i + 2]));
                }
                j++;
            }
        }
        else
        {
            Debug.Log("Tried to parse coordinates from string to Vector3, but there are less than 3 coordinates:" + input);
        }

        return array;
    }

    Vector3 StringToVector3(string input)
    {
        Vector3 vec3 = new Vector3(1, 1, 1);

        string[] sArray = input.Split(' ');
        if(sArray.Length == 3)
        {
            vec3 = new Vector3(float.Parse(sArray[0]),
                               float.Parse(sArray[1]),
                               float.Parse(sArray[2]));
        }
            
        return vec3;
    }

    /*public int FindAnnotation(string annotationName)
    {
        for(int i=0; i<annotationList.Count; i++)
        {
            if (annotationList[i].name.ToUpper().Trim() == annotationName.ToUpper().Trim())
            {
                return i;
            }      
        }

        return -1;
    }*/

    int FindAnnotation(string annotationName)
    {
        List<int> indexes = new List<int>();

        for (int i = 0; i < annotationList.Count; i++)
        {
            if (annotationList[i].name.ToUpper().Trim() == annotationName.ToUpper().Trim())
            {
                //indexes.Add(i);
                return i;
            }
        }

        return -1;
    }


    Color ParseColor(string input)
    {
        string[] sArray = input.Split(' ');
        Color color;
        if (sArray.Length >= 3)
        {
            color = new Color(float.Parse(sArray[0]),
                              float.Parse(sArray[1]),
                              float.Parse(sArray[2]));
        }
        else
        {
            color = new Color(1, 1, 1);
        }

        return color;
    }

    void ResetTransform(GameObject gameObject)
    {
        gameObject.transform.localPosition = new Vector3(0, 0, 0);
        gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }
    bool ContainsDistinct(List<Material> materialList)
    {
        //TODO: use different criteria for comparing materials?
        for (int i = 0; i < materialList.Count; i++)
        {
            for (int j = i + 1; j < materialList.Count; j++)
            {
                if(materialList[i] != null && materialList[j] != null)
                    if (materialList[i].color != materialList[j].color)
                        return true;
            }
        }
        return false;
    }
    #endregion

    void ScaleToTarget()
    {
        if (scaleToTarget)
        {
            if (scaleToTarget.GetComponent<MeshFilter>())
            {
                float scaleToSizeX = scaleToTarget.GetComponent<MeshFilter>().mesh.bounds.size.x; //TODO: also check y and z
                float scaleToScaleX = scaleToTarget.transform.localScale.x;

                GameObject partGeometry = GameObject.Find("swPart0").transform.GetChild(0).gameObject; //TODO: automate this

                float partSizeX = partGeometry.GetComponent<MeshFilter>().mesh.bounds.size.x;
                float partScaleX = partGeometry.transform.localScale.x;

                float scaleDifference = scaleToSizeX * scaleToScaleX / partSizeX * partScaleX;

                this.gameObject.transform.localScale = new Vector3(scaleDifference, scaleDifference, scaleDifference);
            }
            else
            {
                Debug.Log("Could not scale to target object because the object does not have a mesh.");
            }

        }
    }
}