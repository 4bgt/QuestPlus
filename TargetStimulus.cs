using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetStimulus
{
    public float value;
    public int index;

    public TargetStimulus()
    {
        this.value = 0;
        this.index = 0;
    }

    public TargetStimulus(float value, float[] stimDomain) //getting stimulus domain's stimuli indices 
    {
        this.value = value;
        for (int i = 0; i < stimDomain.Length; i++)
        {
            if (value == stimDomain[i])
            {
                index = i;
            }
        }

    }
    public TargetStimulus(float value, int index)
    {
        this.value = value;
        this.index = index;
    }
}