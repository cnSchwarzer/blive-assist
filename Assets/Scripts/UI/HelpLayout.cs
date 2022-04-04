using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpLayout : MonoBehaviour
{
    public GameObject[] sections;
    public ButtonGroupLayout sectionGroup;
    
    void Start()
    { 
        sectionGroup.OnSelectChanged += i => {
            for (var k = 0; k < sections.Length; ++k) {
                sections[k].SetActive(i == k);
            }
        };
        sectionGroup.SetSelectedAndDispatch(0);
    }
 
}
