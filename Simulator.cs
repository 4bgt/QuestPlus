using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulator : MonoBehaviour
{
    Properties questPlus;
    public float[] mu_all_estimates;
    public float[] se_all_estimates;

    public float grid_height = 0.5f;
    public int grid_width = 10;
    public int height = 200;

    [SerializeField, Range(0, 999)]
    public int marker_index= 0;
    public float marker_se = 0;
    public float marker_mu = 0;

    // Start is called before the first frame update
    void Start()
    {
        float stimDomainMin = -Mathf.PI / 15;
        float stimDomainMax = Mathf.PI / 15;
        const int splitValue = 30;
        float[] stimDomain = new float[splitValue];
        float step = (stimDomainMin - stimDomainMax) / (splitValue - 1);
        for (int i = 0; i < splitValue; i++)
        {
            stimDomain[i] = stimDomain[i] + step;
        }

        float[] respDomain = new float[2] { 0, 1 }; // response domain // the range of possible answers given by the subject upon being presented with the above stimulus. Currently only the 2AFC- scenario is supported
        string[] stopRule = new string[1] { "stdev" }; // stop rule used to force the end of the presentation-update-cycle that estimates the subjects probable mu-value. Currently only the standard error as a stop rule is supported
        float stopCriterion = Mathf.PI / 120; // The value corresponding to the aforementioned stop rule 

        float mu_start = -Mathf.PI / 45; // when estimating the true mu value, a starting point for this mu has to be given
        float mu_end = Mathf.PI / 45; // also, an end point has to be given, to form the range of possible mu values
        int mu_steps = 90; // the sensitivity of the mu measurement, i.e. how many steps are in the mu range
        float sigma = 1f; // the deviation value for the density-functions used in this program

        float gamma = 0.5f;
        float lambda = 0.02f;

        Prior prior = new Prior(mu_start, mu_end, mu_steps, sigma, 0, 0); // builds a new Prior object for the evenly distributed probabilities for each possible mu
        ParamDomain paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma, gamma, lambda); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu

        Properties questPlus = new Properties(stimDomain, paramDomain, respDomain, stopRule, stopCriterion); // builds a new prop object containing the parameters set above

        questPlus.Init(); // used to initialise the mu measurement pipeline by creating likelihood and prior probabilities

        TargetStimulus currentStimulus = new TargetStimulus();


        bool isFinished = false;
        bool response;
        float skill = Mathf.PI/60;
        int counter = 0;
        while (counter < 1000 & !isFinished)
        {
            currentStimulus = questPlus.GetTargetStim();
            counter++;
            if (currentStimulus.value >= skill)
            {
                response = true; //rechts
                Debug.Log("Stimulus Value: "+ currentStimulus.value +" Index: "+ currentStimulus.index+ ": rechts");
            }
            else
            {
                response = false; //links
                Debug.Log("Stimulus Value: " + currentStimulus.value + " Index: " + currentStimulus.index + ": links");
            }

            isFinished = questPlus.UpdateEverything(response);

            mu_all_estimates = questPlus.history_estimate.ToArray();
            se_all_estimates = questPlus.history_se.ToArray();
            Debug.Log("is finished? " + isFinished);

        }
        questPlus.History();
    }

    // Update is called once per frame
    void Update()
    {
        marker_index = marker_index % se_all_estimates.Length;
        marker_se = se_all_estimates[marker_index];
        marker_mu = mu_all_estimates[marker_index];
    }
}
