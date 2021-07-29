using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationControl : MonoBehaviour
{
    [SerializeField] private GameObject Model;
    // Start is called before the first frame update
    void Start()
    {

    }

    public void HideObject(GameObject obj){

        obj.SetActive(false);

    }

    public void ShowObject(GameObject obj){
        obj.SetActive(true);
    }

    public void hideBadStuff(GameObject obj){
        GameObject partGeometry = GameObject.Find("swPRT");

        

        MeshRenderer[] meshes = partGeometry.GetComponentsInChildren<MeshRenderer>();

        meshes[0].gameObject.SetActive(false);
        
        GameObject.Find("swView0").SetActive(false);
        GameObject.Find("swView0").SetActive(false);
    }
    public void PlayAnim(){
        Animator[] anims = GetComponentsInChildren<Animator>();
        foreach (Animator anim in anims){
            anim.SetTrigger("PlayAnim");
        }
    }
     public void ResetAnim(){
        Animator[] anims = GetComponentsInChildren<Animator>();
        foreach (Animator anim in anims){
            anim.SetTrigger("Reset");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
