using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationScaling: MonoBehaviour
{
    public GameObject Geometry;
    public GameObject Target;
    // Start is called before the first frame update

     //3d 
     //TODO remove this method and call ConstructCharacter() from the script that instantiates this object
     void Start() {
 
     }
     
    
 
     /// <summary>
     /// Character construction method.
     /// </summary>
     void CombineMesh() {
 
     // 1. reset object to zero-position to avoid issues with world/local during combine
         Vector3 originalPosition = this.transform.position;
         this.transform.position = Vector3.zero;
 

 
     // 2. get base mesh by name 
         SkinnedMeshRenderer[] smRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
 
         SkinnedMeshRenderer smrBase = smRenderers[0];
 
         if(smrBase != null)
             Debug.Log("Base mesh successfully loaded with " + smrBase.bones.Length + " bones.");
         else
             Debug.LogError("Base mesh is invalid.");
 
     // 3. keep a list of objects to destroy (at the end of the script)
         List<SkinnedMeshRenderer> toDestroy = new List<SkinnedMeshRenderer>();
 
 
     // 4. get bone information from base and destroy it
         List<Transform> bones = new List<Transform>();        
         Hashtable bonesByHash = new Hashtable();
         List<BoneWeight> boneWeights = new List<BoneWeight>();        
 
         //keep bone info
         int boneIndex = 0;
         foreach(Transform bone in smrBase.bones) {
             
             bones.Add(bone);
             bonesByHash.Add(bone.name, boneIndex);
             boneIndex++;
         }
 

         //destroy
         //toDestroy.Add(smrBase);
 
 
     // 5. Keep a list of combine instances as we start digging into objects.
     
         List<CombineInstance> combineInstances = new List<CombineInstance>();
 
 
     // 6. get body smr, alter UVs, insert into combine, delete
 
         Vector2[] uvs;
         List<Vector2> totalUVs = new List<Vector2>();
 

         foreach (SkinnedMeshRenderer skinnedMesh in smRenderers){
 
         uvs = skinnedMesh.sharedMesh.uv;
         
         for(int n = 0; n < uvs.Length; n++)
             uvs[n] = new Vector2(uvs[n].x * 0.5f, uvs[n].y);
 
         skinnedMesh.sharedMesh.uv = uvs;
         totalUVs.AddRange(uvs);
 
         InsertSMRToCombine(skinnedMesh, bonesByHash, boneWeights, combineInstances);
 
         toDestroy.Add(skinnedMesh);
         }

         //combine
         //add an empty skinned mesh renderer, and combine meshes into it
         SkinnedMeshRenderer r = gameObject.AddComponent<SkinnedMeshRenderer>();
         
         r.sharedMesh = new Mesh();
         r.sharedMesh.CombineMeshes(combineInstances.ToArray());
         r.sharedMesh.uv = totalUVs.ToArray();
         
         r.bones = bones.ToArray();
         r.rootBone = bones[0]; // TODO we can search bonehash for the name of the root node
         r.sharedMesh.boneWeights = boneWeights.ToArray();

         //late destroy all skinnedmeshrenderers
         
 
         //then all smrs
         foreach (SkinnedMeshRenderer t in toDestroy) {
 
             // TODO destroy unnecessary bips
 //            Transform bipRoot = t.gameObject.transform.FindChild("Bip001");
 //
 //            if(bipRoot != null) 
 //                Object.Destroy(bipRoot);
 
             Object.Destroy(t.gameObject);
         }
 
         //recalculate bounds and return to original position
         //r.sharedMesh.RecalculateBounds();
         this.transform.position = originalPosition;
     }
 
     #region extra methods
 
     private void InsertSMRToCombine (SkinnedMeshRenderer smr, Hashtable boneHash, 
                                      List<BoneWeight> boneWeights, List<CombineInstance> combineInstances) {
 
         BoneWeight[] meshBoneweight = smr.sharedMesh.boneWeights;
         
         // remap bone weight bone indexes to the hashtable obtained from base object
         foreach(BoneWeight bw in meshBoneweight) {
             
             BoneWeight bWeight = bw;
             
             bWeight.boneIndex0 = (int)boneHash[smr.bones[bw.boneIndex0].name];
             bWeight.boneIndex1 = (int)boneHash[smr.bones[bw.boneIndex1].name];
             bWeight.boneIndex2 = (int)boneHash[smr.bones[bw.boneIndex2].name];
             bWeight.boneIndex3 = (int)boneHash[smr.bones[bw.boneIndex3].name];
             
             boneWeights.Add(bWeight);
         }
 
         //add the smr to the combine list; also add to destroy list
         CombineInstance ci = new CombineInstance();
         ci.mesh = smr.sharedMesh;
         
         ci.transform = smr.transform.localToWorldMatrix;
         combineInstances.Add(ci);
     }
 
 
     /// <summary>
     /// Finds a SkinnedMeshRenderer in the list.
     /// </summary>
     /// <returns>Found SMR.</returns>
     /// <param name="source">Source array to search.</param>
     /// <param name="name">Name of the SMR to be searched.</param>
     private SkinnedMeshRenderer FindByName(SkinnedMeshRenderer[] source, string name) {
 
         SkinnedMeshRenderer target = null;
 
         foreach(SkinnedMeshRenderer s in source) {
             
             if(s.name.Contains(name)) {
                 
                 target = s;
                 break;
             }
         }
         
         if(target == null)
             Debug.LogError("SkinnedMeshRenderer " + name + " not found.");
 
         return target;
     }
 
     #endregion
 
    public void Scale()
    {
        if (Geometry)
        {
            if (Geometry.GetComponent<SkinnedMeshRenderer>())
            {
                
                float scaleToSizeX = Geometry.GetComponent<SkinnedMeshRenderer>().bounds.size.x; //TODO: also check y and z
                float scaleToScaleX = Geometry.transform.localScale.x;
                if (Target){
                    GameObject partGeometry = Target; //TODO: automate this
                
                float partSizeX = partGeometry.GetComponent<MeshFilter>().mesh.bounds.size.x;
                float partScaleX = partGeometry.transform.localScale.x;

                float scaleDifference = scaleToSizeX * scaleToScaleX / partSizeX * partScaleX;

                this.gameObject.transform.localScale = new Vector3(scaleDifference, scaleDifference, scaleDifference);
                }
            }
            else
            {
                Debug.Log("Could not scale to target object because the object does not have a mesh.");
            }

        }
    }
}
