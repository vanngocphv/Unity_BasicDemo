using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupUI : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        HideGameObject();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void HideGameObject()
    {
        this.gameObject.SetActive(false);
    }
    public void ShowGameObject()
    {
        this.gameObject.SetActive(true);
    }
}
