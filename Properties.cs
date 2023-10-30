
using System.Collections.Generic;
using UnityEngine;
using System.IO;

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
        if (paramDomain.saturation.Length > 1)
        {
            double res = saturation + (1 - saturation) * (double)normal_dist.CumulativeDistribution(x);
            return (res);
        }
        else
        {
            double res = gamma + (1 - gamma - lambda) * (double)normal_dist.CumulativeDistribution(x);
            return (res);
        }


    }

    //public Prior Next()
    public void Init() // initializing the likelihoods and priors
    {
        //float[][][] likelihoods;

        double[,,] likelihood = new double[stimDomain.Length, paramDomain.Length, respDomain.Length];

        for (int s = 0; s < stimDomain.Length; s++) //based on stimulus and parameter domain size
        {
            int index = 0;
            if (paramDomain.saturation.Length > 1)
            {
                for (int saturation_idx = 0; saturation_idx < paramDomain.saturation.Length; saturation_idx++)
                {
                    for (int sigma_idx = 0; sigma_idx < paramDomain.sigma.Length; sigma_idx++)
                    {

                        for (int mu_idx = 0; mu_idx < paramDomain.mu.Length; mu_idx++)
                        {
                            likelihood[s, index, 1] = Funktion(stimDomain[s], paramDomain.mu[mu_idx], paramDomain.sigma[sigma_idx], saturation: paramDomain.saturation[saturation_idx]); //function calculates the likelihoods based on each simuli, each mu and sigma for resp domain = 0, i.e. correct answer
                            likelihood[s, index, 0] = 1 - likelihood[s, index, 1];//for the resp domains second value, likelihood is 1 - the other likelihood
                            index++;
                        }
                    }
                }
            }
            else
            {
                 for (int lambda_idx = 0; lambda_idx < paramDomain.lambda.Length; lambda_idx++)
                    {
                    for (int gamma_idx = 0; gamma_idx < paramDomain.gamma.Length; gamma_idx++)
                    {
                        for (int sigma_idx = 0; sigma_idx < paramDomain.sigma.Length; sigma_idx++)
                        {

                            for (int mu_idx = 0; mu_idx < paramDomain.mu.Length; mu_idx++)
                            {
                                likelihood[s, index, 1] = Funktion(stimDomain[s], paramDomain.mu[mu_idx], paramDomain.sigma[sigma_idx], paramDomain.gamma[gamma_idx], paramDomain.lambda[lambda_idx]); //function calculates the likelihoods based on each simuli, each mu and sigma for resp domain = 0, i.e. correct answer
                                likelihood[s, index, 0] = 1 - likelihood[s, index, 1];//for the resp domains second value, likelihood is 1 - the other likelihood
                                index++;
                                Debug.Log("stimdomain " + stimDomain[s] + " paramdomain.mu[mu_idx] " + paramDomain.mu[mu_idx] + " paramDomain.sigmaindex " + paramDomain.sigma[sigma_idx] + " paramDomain.gamma[gamma_idx] " + paramDomain.gamma[gamma_idx] + " paramDomain.lambda[lambda_idx] " + paramDomain.lambda[lambda_idx]); //function calculates the likelihoods based on each simuli, each mu and sigma for resp domain = 0, i.e. correct answe
                            }
                        }
                    }
                }
            }                                                                                                                                          //Debug.Log("stim/param " + stimDomain[s] + "/" + paramDomain.mu[p] + "   " + likelihood[s, p, 0]);
        }
        for (int response = 0; response < respDomain.Length; response++)
        {
            Debug.Log(response + " response");
            for (int zeile = 0; zeile < stimDomain.Length; zeile++)
            {
                Debug.Log(zeile + " Zeile");
                for (int spalte = 0; spalte < paramDomain.Length; spalte++)
                {
                    Debug.Log(likelihood[zeile, spalte, response]);
                }
            }
        }


        this.likelihoods = likelihood;
        this.posterior = paramDomain;
        this.posterior.MakePrior();
    }

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
                for (int p = 0; p < this.posterior.mu.Length; p++)
                {
                    //Debug.Log(this.posterior.mu_propabilities[p]);
                    postTimesL[s, p, r] = this.posterior.mu[p] * likelihoods[s, p, r]; //postTimesL takes the current mu probabilities and multiplies them with the predefined likelihoods
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
                    for (int p = 0; p < this.posterior.mu.Length; p++)
                    {
                        newPosteriors[s, p, r] = postTimesL[s, p, r] / pk[s, r]; //see above
                        posteriortemp[s, p, r] = newPosteriors[s, p, r] * System.Math.Log(newPosteriors[s, p, r]); //the new  posteriortemp values are then multiplied with their logarithm to form the negative shannon information
                        //Debug.Log(" Log von postTimesL  " + posteriortemp[s, p, r]);
                    }
                }
                else // in case a pk value reaches zero, they are treated as NaN to not influence further calculations
                {
                    for (int p = 0; p < this.posterior.mu.Length; p++)
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
                for (int p = 0; p < this.posterior.mu.Length; p++)
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
            this.posterior.mu[p] = this.posterior.mu[p] * this.likelihoods[this.current_stim_ID, p, response_int];
            sum_mu += this.posterior.mu[p];
            this.posterior.sigma[p] = this.posterior.sigma[p] * this.likelihoods[this.current_stim_ID, p, response_int];
            sum_sigma += this.posterior.sigma[p];

        }
        for (int p = 0; p < paramDomain.Length; p++) //muss nicht gleich lang wie 
        {
            //normalize
            this.posterior.mu[p] = this.posterior.mu[p] / sum_mu;
            this.posterior.sigma[p] = this.posterior.sigma[p] / sum_sigma;
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

        for (int m = 0; m < this.posterior.mu.Length; m++)
        {
            //Debug.Log(this.posterior.mu_propabilities[m] + " * " + this.paramDomain.mu[m] + " * " + this.paramDomain.mu[m] + " = " + (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]));
            sum1 += (this.posterior.mu[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]); //mu
            sum2 += (this.posterior.mu[m] * this.paramDomain.mu[m]); //mu
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
        for (int m = 0; m < this.posterior.mu.Length; m++)
        {
            //Debug.Log(this.posterior.mu_propabilities[m] + " * " + this.paramDomain.mu[m] + " * " + this.paramDomain.mu[m] + " = " + (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]));
            this.current_estimate_mu += this.posterior.mu[m] * this.paramDomain.mu[m];
        }
        for (int n = 0; n < this.posterior.sigma.Length; n++)
        {
            //Debug.Log(this.posterior.mu_propabilities[m] + " * " + this.paramDomain.mu[m] + " * " + this.paramDomain.mu[m] + " = " + (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]));
            this.current_estimate_sigma += this.posterior.sigma[n] * this.paramDomain.sigma[n];
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