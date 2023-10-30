using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using UnityEditor;
using System.Data;
using UnityEngine.Analytics;

public class QP : MonoBehaviour //QP is a translation for the matlab-based QuestPlus algorithm from mathematica to c# language
{

    void Start() //upon starting the program in a unity-based environment the key input QP needs need to be given
    {
        // //Properties prop = new Properties(Enumerable.Range(0,10).ToArray(), Enumerable.Range(0,10).ToArray(), new float[2] {0,1} , new string[1] { "stdev" } , 2,1, 10, new double [9,30,2], new Priors() ,new List<float>() , new List<float>());
        // //float[] stimDomain = new float[9] { -Mathf.PI / 15, -Mathf.PI / 30, -Mathf.PI / 60, 0, Mathf.PI / 120, Mathf.PI / 60, Mathf.PI / 40, Mathf.PI / 30, Mathf.PI / 15 }; // stimulus domain // the range of values that is presented as stimuli to the subject. Can be any typpe of array
        // float stimDomainMin = -Mathf.PI / 15;
        // float stimDomainMax = Mathf.PI / 15;
        // const int splitValue = 30;
        // float[] stimDomain = new float[splitValue];
        // float step = (stimDomainMin -stimDomainMax) / (splitValue-1);
        // for (int i = 0; i < splitValue; i++)
        // {
        //     stimDomain[i] = stimDomain[i] + step;
        // }
        // float[] respDomain = new float[2] { 0, 1 }; // response domain // the range of possible answers given by the subject upon being presented with the above stimulus. Currently only the 2AFC- scenario is supported
        // string[] stopRule = new string[1] { "stdev" }; // stop rule used to force the end of the presentation-update-cycle that estimates the subjects probable mu-value. Currently only the standard error as a stop rule is supported
        // float stopCriterion = 0.5f; // The value corresponding to the aforementioned stop rule 
        // float minNTrials = 1; // the minimum amount of run trials before aborting measurement
        // float maxNTrials = 10; // if the stop rule "maxtrials" is used, maxNTrials is used to determine the maximum amount of trials
        // float[,,] likelihood = new float[splitValue, 30, 2]; // the dimensions for the likelihood
        // Prior posterior = new Prior(); //new Prior object for the posterior values 
        // List<float> history_stim = new List<float>(); //list for the given stimulus over the course of the experiment
        // List<float> history_resp = new List<float>(); //list for the given responses over the course of the experiment
        // List<float> history_est = new List<float>(); //list of estimated mus for each trial over the course of the experiment

        // float mu_start = 0; // when estimating the true mu value, a starting point for this mu has to be given
        // float mu_end = 10; // also, an end point has to be given, to form the range of possible mu values
        // int mu_steps = 30; // the sensitivity of the mu measurement, i.e. how many steps are in the mu range
        // float sigma = 1; // the deviation value for the density-functions used in this program

        // Prior prior = new Prior(mu_start, mu_end, mu_steps, sigma, 0, 0); // builds a new Prior object for the evenly distributed probabilities for each possible mu
        // ParamDomain paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma, 0, 0); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu


        // Properties prop = new Properties(stimDomain, paramDomain, respDomain, stopRule, stopCriterion, minNTrials, maxNTrials, likelihood, posterior, history_stim, history_resp, history_est); // builds a new prop object containing the parameters set above
        // //Debug.Log(prop);
        // prop.Init(); // used to initialise the mu measurement pipeline by calculating likelihood and prior probabilities

        // TargetStimulus currentStimulus = new TargetStimulus();

        // //for (int i = 0; i < 20; i++)
        // //{
        // //    Debug.Log(i);
        // //    prop.getTargetStim();
        // //    Debug.Log("is finished? " + prop.UpdateEverything(true));
        // //    Debug.Log("Fehler " + prop.current_se);
        // //    Debug.Log("Last Stimulus " + prop.history_stim[prop.history_stim.Count - 1]);
        // //    Debug.Log("Estimate " + prop.current_estimate);
        //     //bool a = prop.UpdateEverything(true);
        //// }


        // //prop.Update(prior);

    }

    // Update is called once per frame
    void Update()
    {

    }
}

//[InitializeOnLoad]
public class Properties
{
    public float[] stimDomain;
    public ParamDomain paramDomain;
    public float[] respDomain;
    public string[] stopRule;
    public float stopCriterion;


    public float minNTrials;
    public float maxNTrials;

    public double[,,] likelihoods;
    public ParamDomain posterior;

    public List<float> history_stim;
    public List<float> history_resp;
    public List<double> history_estimate_mu;
    public List<double> history_estimate_sigma;
    public List<float> history_se;

    public double current_se;
    public int current_stim_ID;
    public double current_estimate_mu;
    public double current_estimate_sigma;
    public int trials;
    public bool all_done;

    public double Funktion(double x, double mu, double sigma, double gamma = 0, double lambda = 0, double saturation = 0) // function that builds the used cumulative distribution based on x (the stimulus), mu (the subjects ability) and sigma (standard deviation)
    {
        MathNet.Numerics.Distributions.Normal normal_dist = new MathNet.Numerics.Distributions.Normal(mu, sigma);
        double res = saturation + (1 - saturation) * (double)normal_dist.CumulativeDistribution(x);
        return (res);
    }

    //public Prior Next()


    public TargetStimulus getTargetStim() //used to get the next stimulus to present to the subject based on the maximum information available at the time
    {
        double[,,] postTimesL = new double[stimDomain.Length, paramDomain.Length, respDomain.Length]; // declaring a bunch of important objects used to calculate the priors and posteriors
        double[,] pk = new double[stimDomain.Length, respDomain.Length];
        double[,,] newPosteriors = new double[stimDomain.Length, paramDomain.Length, respDomain.Length];
        double[,] H = new double[stimDomain.Length, respDomain.Length];
        double[,,] posteriortemp = new double[stimDomain.Length, paramDomain.Length, respDomain.Length];
        double[] EH = new double[stimDomain.Length];

        //Debug.Log("TargetStim is called");

        for (int r = 0; r < respDomain.Length; r++) // this 3 for-arguments result in a data spread of [s, p, r] so [stimulus domain, parameter domain, response domain]
        {
            for (int s = 0; s < this.stimDomain.Length; s++)
            {
                for (int p = 0; p < this.posterior.mu_propabilities.Length; p++)
                {
                    //Debug.Log(this.posterior.mu_propabilities[p]);
                    postTimesL[s, p, r] = this.posterior.mu_propabilities[p] * likelihoods[s, p, r]; //postTimesL takes the current mu probabilities and multiplies them with the predefined likelihoods
                    pk[s, r] = pk[s, r] + postTimesL[s, p, r]; // this step together with the one below essentially normalizes the results calculated above


                }
                //Debug.Log("pk  s/r" + s + "/" + r + "   " + pk[s, r]);
            }
        }

        for (int r = 0; r < respDomain.Length; r++)
        {
            for (int s = 0; s < this.stimDomain.Length; s++)
            {
                if (pk[s, r] != 0) //pk values can result in 0 when their probability is 0
                {
                    //Debug.Log("pk != 0");
                    for (int p = 0; p < this.posterior.mu_propabilities.Length; p++)
                    {
                        newPosteriors[s, p, r] = postTimesL[s, p, r] / pk[s, r]; //see above
                        posteriortemp[s, p, r] = newPosteriors[s, p, r] * System.Math.Log(newPosteriors[s, p, r]); //the new  posteriortemp values are then multiplied with their logarithm to form the negative shannon information
                        //Debug.Log(" Log von postTimesL  " + posteriortemp[s, p, r]);
                    }
                }
                else // in case a pk value reaches zero, they are treated as NaN to not influence further calculations
                {
                    for (int p = 0; p < this.posterior.mu_propabilities.Length; p++)
                    {
                        //Debug.Log("Something goes NaN");
                        newPosteriors[s, p, r] = double.NaN;
                        posteriortemp[s, p, r] = double.NaN;
                    }
                }
            }
        }

        for (int r = 0; r < respDomain.Length; r++)
        {
            for (int s = 0; s < this.stimDomain.Length; s++)
            {
                // Debug.Log("posteriortemp existst");
                for (int p = 0; p < this.posterior.mu_propabilities.Length; p++)
                {
                    if (!double.IsNaN(posteriortemp[s, p, r]))
                    {

                        H[s, r] = H[s, r] + posteriortemp[s, p, r]; // H is build by essentially summing up the posteriortemp values
                    }
                }
                //Debug.Log("s/r   H " + s + "/" + r + "  " + H[s, r]);
                H[s, r] = -H[s, r]; // for calculative purposes H is then made to be all negative values
            }
        }


        for (int s = 0; s < this.stimDomain.Length; s++)
        {
            // Debug.Log("creating EH[s]");
            for (int r = 0; r < respDomain.Length; r++)
            {
                EH[s] = EH[s] + (H[s, r] * pk[s, r]); // the value EH is build from pk and H values to form the entropy score for each H. the next item will be chosen based on this value
                                                      //  Debug.Log(s + " ; " + EH[s]);
            }

        }


        int idx = 0;
        double tempValue = double.PositiveInfinity; // this for argument uses a very high float (positive infinity) and then cycles through all EH values to find the smallest one. It is brute-force and I like it
        for (int s = 0; s < this.stimDomain.Length; s++)
        {
            //Debug.Log(EH[s] + " entropy an der stelle " + s);
            if (EH[s] <= tempValue)
            {
                // Debug.Log(EH[s] + " is smaller " + s);
                tempValue = EH[s];
                idx = s;

            }
        }
        // Debug.Log(tempValue);

        TargetStimulus res = new TargetStimulus(stimDomain[idx], idx); //the resulting target stimulus is returned as (stimulus value, stimulus index)
        this.current_stim_ID = idx;
        return (res);

    }

    public void Init() // initializing the likelihoods and priors
    {
        //float[][][] likelihoods;

        double[,,] likelihood = new double[stimDomain.Length, paramDomain.Length, respDomain.Length];

        for (int s = 0; s < stimDomain.Length; s++) //based on stimulus and parameter domain size
        {
            int index = 0;

            for (int mu_idx = 0; mu_idx < paramDomain.mu.Length; mu_idx++)
            {
                for (int sigma_idx = 0; sigma_idx < paramDomain.sigma.Length; sigma_idx++)
                {
                    if (paramDomain.saturation.Length > 1)
                    {
                        for (int saturation_idx = 0; saturation_idx < paramDomain.saturation.Length; saturation_idx++)
                        {
                            likelihood[stimDomain.Length - s - 1, paramDomain.Length - index - 1, 0] = Funktion(stimDomain[s], paramDomain.mu[mu_idx], paramDomain.sigma[sigma_idx], saturation: paramDomain.saturation[saturation_idx]); //function calculates the likelihoods based on each simuli, each mu and sigma for resp domain = 0, i.e. correct answer
                            likelihood[stimDomain.Length - s - 1, paramDomain.Length - index - 1, 1] = 1 - likelihood[stimDomain.Length - s - 1, paramDomain.Length - index - 1, 0];//for the resp domains second value, likelihood is 1 - the other likelihood
                            index++;
                        }
                    }
                    else
                    { 
                        for (int gamma_idx = 0; gamma_idx < paramDomain.gamma.Length; gamma_idx++)
                        {
                            for (int lambda_idx = 0; lambda_idx < paramDomain.lambda.Length; lambda_idx++)
                            {
                                likelihood[stimDomain.Length - s - 1, paramDomain.Length - index - 1, 0] = Funktion(stimDomain[s], paramDomain.mu[mu_idx], paramDomain.sigma[sigma_idx], paramDomain.gamma[gamma_idx], paramDomain.lambda[lambda_idx]); //function calculates the likelihoods based on each simuli, each mu and sigma for resp domain = 0, i.e. correct answer
                                likelihood[stimDomain.Length - s - 1, paramDomain.Length - index - 1, 1] = 1 - likelihood[stimDomain.Length - s - 1, paramDomain.Length - index - 1, 0];//for the resp domains second value, likelihood is 1 - the other likelihood
                                index++;
                            }
                        }
                    }
                }
            }                                                                                                                                          //Debug.Log("stim/param " + stimDomain[s] + "/" + paramDomain.mu[p] + "   " + likelihood[s, p, 0]);
        }



        //Debug.Log(prior.mu_propabilities[0]);

        this.likelihoods = likelihood;
        this.posterior = paramDomain;
        this.posterior.MakePrior();
    }



    /*
        % computed variables, set when using initialise()
        prior               % vector, containing probability of each parameter-combination
        likelihoods        	% 2D matrix, containing conditional probabilities of each outcome at each stimulus-combination/parameter-combination
        posterior          	% posterior probability distribution over domain.
        
        % measured variables, updated after each call to update()
        history_stim        = []        % vector of stims shown
        history_resp        = []        % vector of responses (1 HIT, 0 MISS)
     * 
    */
    //Default Constructor (empty)


    public Properties()
    {
        stimDomain = new float[1] { 0 };
        paramDomain = new ParamDomain();
        respDomain = new float[1] { 0 };
        stopRule = new string[1] { "" };
        stopCriterion = 0;
        minNTrials = 0;
        maxNTrials = 0;
        likelihoods = new double[1, 1, 1];
        posterior = new ParamDomain();
        history_stim = new List<float> { };
        history_resp = new List<float> { };
        history_estimate_mu = new List<double> { };
        history_estimate_sigma = new List<double> { };
        history_se = new List<float> { };
        current_se = 0;
        current_stim_ID = 0;
        current_estimate_mu = 0;
        current_estimate_sigma = 0;
        trials = 0;
        all_done = false;

    }

    //Constructor
    public Properties(float[] stimDomain, ParamDomain paramDomain, float[] respDomain, string[] stopRule, float stopCriterion, float minNTrials, float maxNTrials, double[,,] likelihood, ParamDomain posterior, List<float> history_stim, List<float> history_resp, List<double> history_estimate_mu, List<double> history_estimate_sigma, List<float> history_se)
    {

        this.stimDomain = stimDomain;
        this.paramDomain = paramDomain;
        this.respDomain = respDomain;
        this.stopRule = stopRule;
        this.stopCriterion = stopCriterion;
        this.minNTrials = minNTrials;
        this.maxNTrials = maxNTrials;
        this.posterior = posterior;
        this.likelihoods = likelihood;
        this.history_resp = history_resp;
        this.history_stim = history_stim;
        this.history_estimate_mu = history_estimate_mu;
        this.history_estimate_sigma = history_estimate_sigma;
        this.history_se = history_se;
        this.current_se = 0;
        this.current_stim_ID = 0;
        this.current_estimate_mu = 0;
        this.current_estimate_sigma = 0;
        this.trials = 0;
        this.all_done = false;
    }
    public Properties(float[] stimDomain, ParamDomain paramDomain, float[] respDomain, string[] stopRule, float stopCriterion, float minNTrials = 1, float maxNTrials = 10)
    {

        this.stimDomain = stimDomain;
        this.paramDomain = paramDomain;
        this.respDomain = respDomain;
        this.stopRule = stopRule;
        this.stopCriterion = stopCriterion;
        this.minNTrials = minNTrials;
        this.maxNTrials = maxNTrials;
        this.posterior = new ParamDomain();
        this.likelihoods = new double[0, 0, 0];
        this.history_resp = new List<float>();
        this.history_stim = new List<float>();
        this.history_estimate_mu = new List<double>();
        this.history_estimate_sigma = new List<double>();
        this.history_se = new List<float>();
        this.current_se = 0;
        this.current_stim_ID = 0;
        this.current_estimate_mu = 0;
        this.current_estimate_sigma = 0;
        this.trials = 0;
        this.all_done = false;
    }
    public bool UpdateEverything(bool response) // 
    {
        UpdateSe();
        int response_int = response ? 1 : 0;
        double sum_mu = 0;
        double sum_sigma = 0;
        for (int p = 0; p < paramDomain.Length; p++)
        {
            this.posterior.mu_propabilities[p] = this.posterior.mu_propabilities[p] * this.likelihoods[this.current_stim_ID, p, response_int];
            sum_mu += this.posterior.mu_propabilities[p];
            this.posterior.sigma_propabilities[p] = this.posterior.sigma_propabilities[p] * this.likelihoods[this.current_stim_ID, p, response_int];
            sum_sigma += this.posterior.sigma_propabilities[p];

        }
        for (int p = 0; p < paramDomain.length; p++) //muss nicht gleich lang wie 
        {
            //normalize
            this.posterior.mu_propabilities[p] = this.posterior.mu_propabilities[p] / sum_mu;
            this.posterior.sigma_propabilities[p] = this.posterior.sigma_propabilities[p] / sum_sigma;
        }

        this.history_stim.Add(this.stimDomain[this.current_stim_ID]);
        this.history_resp.Add(response_int);
        Estimates();
        return (IsFinished());

    }

    public void UpdateSe()
    {

        double sum1 = 0;
        double sum2 = 0;

        for (int m = 0; m < this.posterior.mu_propabilities.Length; m++)
        {
            //Debug.Log(this.posterior.mu_propabilities[m] + " * " + this.paramDomain.mu[m] + " * " + this.paramDomain.mu[m] + " = " + (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]));
            sum1 += (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]); //mu
            sum2 += (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m]); //mu
        }
        //Debug.Log(sum1);
        //Debug.Log(sum2);

        //hier muss die Verrechnug für Sigma mit rein

        current_se = System.Math.Sqrt(sum1 - System.Math.Pow(sum2, 2));
        history_se.Add((float)current_se);
    }

    public (double, double) Estimates()
    {
        this.current_estimate_mu = 0;
        this.current_estimate_sigma = 0;
        for (int m = 0; m < this.posterior.mu_propabilities.Length; m++)
        {
            //Debug.Log(this.posterior.mu_propabilities[m] + " * " + this.paramDomain.mu[m] + " * " + this.paramDomain.mu[m] + " = " + (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]));
            this.current_estimate_mu += this.posterior.mu_propabilities[m] * this.paramDomain.mu[m];
        }
        for (int n = 0; n < this.posterior.sigma_propabilities.Length; n++)
        {
            //Debug.Log(this.posterior.mu_propabilities[m] + " * " + this.paramDomain.mu[m] + " * " + this.paramDomain.mu[m] + " = " + (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]));
            this.current_estimate_sigma += this.posterior.sigma_propabilities[n] * this.paramDomain.sigma[n];
        }
        this.history_estimate_mu.Add(current_estimate_mu);
        this.history_estimate_sigma.Add(current_estimate_sigma);
        return (this.current_estimate_mu, this.current_estimate_sigma);

    }

    public bool IsFinished()
    {
        if (history_stim.Count > this.maxNTrials | current_se <= stopCriterion)
        {
            this.all_done = true;
            return true;
        }
        else
        {
            this.all_done = false;
            return false;
        }

    }

    public void History()
    {
        if (this.all_done == true)
        {
            this.trials = this.history_stim.Count;
        }
        string content = "history";
        int final_trials = this.trials;
        string responses_given = string.Join(",", this.history_resp);
        string stimuli_presented = string.Join(",", this.history_stim);
        string estimates_calculated_mu = string.Join(",", this.history_estimate_mu);
        string estimates_calculated_sigma = string.Join(",", this.history_estimate_sigma);
        double final_se = this.current_se;
        float final_stim_ID = this.current_stim_ID;
        double final_estimate_mu = this.current_estimate_mu;
        double final_estimate_sigma = this.current_estimate_sigma;
        var info = $"{content};{final_trials};{responses_given};{stimuli_presented};{estimates_calculated_mu};{estimates_calculated_sigma};{final_se};{final_stim_ID};{final_estimate_mu};{final_estimate_sigma}";
        File.WriteAllText("history.txt", info);
    }
}

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




public class ParamDomain
{

    public int Length;
    public double[] mu;
    public float mu_start;
    public float mu_end;
    public int mu_steps;

    public double[] sigma;
    public float sigma_start;
    public float sigma_end;
    public int sigma_steps;

    public double[] saturation;
    public float saturation_start;
    public float saturation_end;
    public int saturation_steps;

    public double[] gamma;
    public float gamma_start;
    public float gamma_end;
    public int gamma_steps;

    public double[] lambda;
    public float lambda_start;
    public float lambda_end;
    public int lambda_steps;


    //constructor without any values (default)
    public ParamDomain()
    {
        this.mu = new double[1] { 1 };
        this.sigma = new double[1] { 1 };
        this.saturation = new double[1] { 0 };
        this.gamma = new double[1] { 0 };
        this.lambda = new double[1] { 0 };

        this.Length = mu.Length * sigma.Length * saturation.Length * gamma.Length * lambda.Length;
    }

    public ParamDomain(float mu, float mu_end, int mu_steps, float sigma = 1,float sigma_end = 1, int sigma_steps =1, float gamma = 0, float gamma_end =0, int gamma_steps =1, float lambda=0, float lambda_end =0,int lambda_steps=1 ) //fraglich, hier muss das Design aus Matlab eingebaut werden
    {
        (this.mu_start, this.mu_end, this.mu_steps, this.mu) = FillArray(mu_start, mu_end, mu_steps);

        (this.sigma_start, this.sigma_end, this.sigma_steps, this.sigma) = FillArray(sigma_start, sigma_end, sigma_steps);

        (this.gamma_start, this.gamma_end, this.gamma_steps, this.gamma) = FillArray(gamma_start, gamma_end, gamma_steps);

        (this.lambda_start, this.lambda_end, this.lambda_steps, this.lambda) = FillArray(lambda_start, lambda_end, lambda_steps);

        this.saturation = new double[1] { 0 };
        this.Length = this.mu.Length * this.sigma.Length * this.saturation.Length * this.gamma.Length * this.lambda.Length;
    }

    public ParamDomain(float mu, float mu_end, int mu_steps, float sigma=1, float sigma_end = 1, int sigma_steps = 1, float saturation = 1,float saturation_end=0,int saturation_steps=1) //fraglich, hier muss das Design aus Matlab eingebaut werden
    {
        (this.mu_start, this.mu_end, this.mu_steps, this.mu) = FillArray(mu_start, mu_end, mu_steps);

        (this.sigma_start, this.sigma_end, this.sigma_steps, this.sigma) = FillArray(sigma_start, sigma_end, sigma_steps);

        (this.saturation_start, this.saturation_end, this.saturation_steps, this.saturation) = FillArray(saturation_start, saturation_end, saturation_steps);

        this.gamma = new double[1] { 0 };
        this.lambda = new double[1] { 0 };
        this.Length = this.mu.Length * this.sigma.Length * this.saturation.Length * this.gamma.Length * this.lambda.Length;
    }

    public (float, float, int, double[]) FillArray(float start, float end, int steps)
    {
        double[] res = new double[steps];
        float step_size =0;

        if (steps==1)
        {
            step_size = 1;
        }
        else
        {
            step_size = (end - start) / (steps - 1);
            if (step_size <0)
            {
                Debug.LogWarning("Check your start and end values in parameters, end-start is negative!");
            }
        }

        //  Debug.Log(mu_step_size);
        for (int i = 0; i < steps; i++)
        {
            res[i] = start + i * step_size; //builds the possible mu values based on the range and step size by adding the steps up
        }
        return (start, end, steps, res);
    }

    public void MakePrior()
    {
        for (int i = 0; i < mu_steps; i++)
        {
            mu[i] = 1 / (float)mu_steps; //evenly distributes the base mu probabilities before showing the first stimulus
        }

        for (int i = 0; i < sigma_steps; i++)
        {
            sigma[i] = 1 / (float)sigma_steps; //evenly distributes the base mu probabilities before showing the first stimulus
        }

        for (int i = 0; i < gamma_steps; i++)
        {
            gamma[i] = 1 / (float)gamma_steps; //evenly distributes the base mu probabilities before showing the first stimulus
        }
        for (int i = 0; i < lambda_steps; i++)
        {
            lambda[i] = 1 / (float)lambda_steps; //evenly distributes the base mu probabilities before showing the first stimulus
        }
        for (int i = 0; i < saturation_steps; i++)
        {
            saturation[i] = 1 / (float)saturation_steps; //evenly distributes the base mu probabilities before showing the first stimulus
        }
    }

}


