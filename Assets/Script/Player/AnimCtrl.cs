using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimCtrl : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        UpdateSkillInput();
    }

    private void UpdateSkillInput()
    {
       if(Input.GetKeyDown(KeyCode.K))
        {
            CastSkill();
        }
    }

    private void CastSkill()
    {

    }
}
