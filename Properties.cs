
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Linq;

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
    public double[] posterior;

    public List<float> history_stim;
    public List<float> history_resp;
    public List<double> history_estimate_mu;
    public List<double> history_estimate_sigma;
    public List<double> history_estimate_gamma;
    public List<double> history_estimate_lambda;
    public List<double> history_estimate_saturation;
    public List<float> history_se;

    public double current_se;
    public int current_stim_ID;
    public double current_estimate_mu;
    public double current_estimate_sigma;
    public double current_estimate_gamma;
    public double current_estimate_lambda;
    public double current_estimate_saturation;
    public int trials;
    public bool all_done;
    public string start_mode;

    public double Funktion(double x, double mu, double sigma, double gamma = 0, double lambda = 0, double saturation = 0) // function that builds the used cumulative distribution based on x (the stimulus), mu (the subjects ability) and sigma (standard deviation)
    {
        MathNet.Numerics.Distributions.Normal normal_dist = new MathNet.Numerics.Distributions.Normal(mu, sigma);
        if (paramDomain.saturation.Length > 0)
        {
            double res = saturation + (1 - 2 * saturation) * (double)normal_dist.CumulativeDistribution(x);
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
            if (paramDomain.saturation.Length > 0)
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
                                //Debug.Log("stimdomain " + stimDomain[s] + " paramdomain.mu[mu_idx] " + paramDomain.mu[mu_idx] + " paramDomain.sigmaindex " + paramDomain.sigma[sigma_idx] + " paramDomain.gamma[gamma_idx] " + paramDomain.gamma[gamma_idx] + " paramDomain.lambda[lambda_idx] " + paramDomain.lambda[lambda_idx]); //function calculates the likelihoods based on each simuli, each mu and sigma for resp domain = 0, i.e. correct answe
                            }
                        }
                    }
                }
            }                                                                                                                                          //Debug.Log("stim/param " + stimDomain[s] + "/" + paramDomain.mu[p] + "   " + likelihood[s, p, 0]);
        }
        //for (int response = 0; response < respDomain.Length; response++)
        //{
        //    Debug.Log(response + " response");
        //    for (int zeile = 0; zeile < stimDomain.Length; zeile++)
        //    {
        //        Debug.Log(zeile + " Zeile");
        //        for (int spalte = 0; spalte < paramDomain.Length; spalte++)
        //        {
        //            Debug.Log(likelihood[zeile, spalte, response]);
        //        }
        //    }
        //}


        this.likelihoods = likelihood;

        this.posterior = new double[paramDomain.Length]; //empty prior
        for (int i = 0; i < paramDomain.Length; i++) //fill prior
        {
            this.posterior[i] = 1f / paramDomain.Length;
            //Debug.Log("prior:" + i + " " + this.posterior[i]);
        }
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
                for (int p = 0; p < this.posterior.Length; p++)
                {
                    //Debug.Log(this.posterior.mu_propabilities[p]);
                    postTimesL[s, p, r] = this.posterior[p] * likelihoods[s, p, r]; //postTimesL takes the current mu probabilities and multiplies them with the predefined likelihoods
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
                    for (int p = 0; p < this.posterior.Length; p++)
                    {
                        newPosteriors[s, p, r] = postTimesL[s, p, r] / pk[s, r]; //see above
                        posteriortemp[s, p, r] = newPosteriors[s, p, r] * System.Math.Log(newPosteriors[s, p, r]); //the new  posteriortemp values are then multiplied with their logarithm to form the negative shannon information
                        //Debug.Log(" Log von postTimesL  " + posteriortemp[s, p, r]);
                    }
                }
                else // in case a pk value reaches zero, they are treated as NaN to not influence further calculations
                {
                    for (int p = 0; p < this.posterior.Length; p++)
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
                for (int p = 0; p < this.posterior.Length; p++)
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
        List<int> equalEH = new List<int>();
        for (int s = 0; s < this.stimDomain.Length; s++)
        {
            if (EH[s] <= tempValue)
            {
                // Debug.Log(EH[s] + " is smaller " + s);
                //equalEH.Add(s);
                tempValue = EH[s];
                //Debug.Log(tempValue);
                //idx = s;

            }
        }
        for (int s = 0; s < this.stimDomain.Length; s++)
        {
           // Debug.Log(" "+(EH[s] == tempValue )+ EH[s] + tempValue);
            if (EH[s] == tempValue)
            {
                // Debug.Log(EH[s] + " is smaller " + s);
                equalEH.Add(s);
                //tempValue = EH[s];
                //idx = s;
            }
        }
        //Debug.Log("Count: "+equalEH.Count);

        

        switch (start_mode)
        {
            case "median":
                idx = equalEH[(int)((equalEH.Count - 1) / 2)];
                break;
            case "min":
                idx = equalEH[0];
                break;
            case "max":
                idx = equalEH[(equalEH.Count - 1)];
                break;
            case "1_quartil":
                idx = equalEH[(int)((equalEH.Count - 1) / 4)];
                //Debug.Log("1," + (int)((equalEH.Count - 1) / 4));
                break;
            case "3_quartil":
                idx = equalEH[(int)((equalEH.Count - 1) * 0.75)];
                //Debug.Log("3,"+ (int)((equalEH.Count - 1) * 0.75));
                break;
            default:
                Debug.LogWarning("No start_mode selected. Defaulting to median.");
                idx = equalEH[(int)((equalEH.Count - 1) / 2)];
                break;
        }
        TargetStimulus res = new TargetStimulus(stimDomain[idx], idx); //the resulting target stimulus is returned as (stimulus value, stimulus index)
        this.current_stim_ID = idx;
        return (res);

    }


    public bool UpdateEverything(bool response) // 
    {
        if (paramDomain.sigma.Length == 1 & (paramDomain.saturation.Length == 1 | (paramDomain.gamma.Length == 1 & paramDomain.lambda.Length == 1))) // calculating a standard error only makes sense if we only estimate mu
        {
            UpdateSe();
        }
        int response_int = response ? 1 : 0;
        double sum = 0;

        for (int p = 0; p < paramDomain.Length; p++)
        {
            this.posterior[p] = this.posterior[p] * this.likelihoods[this.current_stim_ID, p, response_int];
            sum += this.posterior[p];
        }
        for (int p = 0; p < paramDomain.Length; p++) //muss nicht gleich lang wie 
        {
            //normalize
            this.posterior[p] = this.posterior[p] / sum;
        }

        this.history_stim.Add(this.stimDomain[this.current_stim_ID]);
        this.history_resp.Add(response_int);
        Estimates();
        return (IsFinished());

    }

    private void UpdateSe()
    {

        double sum1 = 0;
        double sum2 = 0;

        for (int m = 0; m < this.posterior.Length; m++)
        {
            //Debug.Log(this.posterior.mu_propabilities[m] + " * " + this.paramDomain.mu[m] + " * " + this.paramDomain.mu[m] + " = " + (this.posterior.mu_propabilities[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]));
            sum1 += (this.posterior[m] * this.paramDomain.mu[m] * this.paramDomain.mu[m]); //mu
            sum2 += (this.posterior[m] * this.paramDomain.mu[m]); //mu
        }
        //Debug.Log(sum1);
        //Debug.Log(sum2);

        current_se = System.Math.Sqrt(sum1 - System.Math.Pow(sum2, 2));
        history_se.Add((float)current_se);
    }

    private double[] Estimates()
    {
        double[] estimate;
        double[,] temp;
        double[] mean = new double[this.posterior.Length];

        if (paramDomain.saturation.Length > 0)
        {
            estimate = new double[3];
            temp = new double[3, posterior.Length];
        }
        else
        {
            estimate = new double[4];
            temp = new double[4, posterior.Length];
        }

        int index = 0;
        if (paramDomain.saturation.Length > 0)
        {
            for (int saturation_idx = 0; saturation_idx < paramDomain.saturation.Length; saturation_idx++)
            {
                for (int sigma_idx = 0; sigma_idx < paramDomain.sigma.Length; sigma_idx++)
                {

                    for (int mu_idx = 0; mu_idx < paramDomain.mu.Length; mu_idx++)
                    {
                        estimate[0] += this.posterior[index] * this.paramDomain.mu[mu_idx]; // posterior ist nicht unbedingt auch so lang wie paramDomain.mu und insbesondere können sigma und mu ja unterschiedlich viele Werte haben
                        estimate[1] += this.posterior[index] * this.paramDomain.sigma[sigma_idx];
                        estimate[2] += this.posterior[index] * this.paramDomain.saturation[saturation_idx];
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
                            estimate[0] += this.posterior[index] * this.paramDomain.mu[mu_idx]; // posterior ist nicht unbedingt auch so lang wie paramDomain.mu und insbesondere können sigma und mu ja unterschiedlich viele Werte haben
                            estimate[1] += this.posterior[index] * this.paramDomain.sigma[sigma_idx];
                            estimate[2] += this.posterior[index] * this.paramDomain.gamma[gamma_idx];
                            estimate[3] += this.posterior[index] * this.paramDomain.lambda[lambda_idx];
                            index++;
                        }
                    }
                }
            }
        }

        //for (int p = 0; p < this.posterior.Length; p++)
        //{
        //    estimate[0] += this.posterior[p] * this.paramDomain.mu[p]; // posterior ist nicht unbedingt auch so lang wie paramDomain.mu und insbesondere können sigma und mu ja unterschiedlich viele Werte haben
        //    estimate[1] += this.posterior[p] * this.paramDomain.sigma[p];

        //    if (paramDomain.saturation.Length > 0)
        //    {
        //        estimate[2] += this.posterior[p] * this.paramDomain.saturation[p];
        //    }
        //    else
        //    {
        //        estimate[2] += this.posterior[p] * this.paramDomain.gamma[p];
        //        estimate[3] += this.posterior[p] * this.paramDomain.lambda[p];
        //    }
        //}

        index = 0;
        if (paramDomain.saturation.Length > 0)
        {
            for (int saturation_idx = 0; saturation_idx < paramDomain.saturation.Length; saturation_idx++)
            {
                for (int sigma_idx = 0; sigma_idx < paramDomain.sigma.Length; sigma_idx++)
                {

                    for (int mu_idx = 0; mu_idx < paramDomain.mu.Length; mu_idx++)
                    {
                        temp[0, index] = System.Math.Pow(this.paramDomain.mu[mu_idx] - estimate[0], 2);
                        temp[1, index] = System.Math.Pow(this.paramDomain.sigma[sigma_idx] - estimate[1], 2);
                        temp[2, index] = System.Math.Pow(this.paramDomain.saturation[saturation_idx] - estimate[2], 2);

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
                            temp[0, index] = System.Math.Pow(this.paramDomain.mu[mu_idx] - estimate[0], 2);
                            temp[1, index] = System.Math.Pow(this.paramDomain.sigma[sigma_idx] - estimate[1], 2);
                            temp[2, index] = System.Math.Pow(this.paramDomain.gamma[gamma_idx] - estimate[2], 2);
                            temp[3, index] = System.Math.Pow(this.paramDomain.lambda[lambda_idx] - estimate[3], 2);
                            //Debug.Log(this.paramDomain.mu[mu_idx] - estimate[0]);
                            //Debug.Log(temp[0, index] + "," + temp[1, index] + "," + temp[2, index] + "," + temp[3, index]);
                            index++;
                        }
                    }
                }
            }
        }

        //for (int p = 0; p < this.posterior.Length; p++)
        //{
        //    temp[0, p] = System.Math.Pow(this.paramDomain.mu[p] - estimate[0], 2);
        //    temp[1, p] = System.Math.Pow(this.paramDomain.sigma[p] - estimate[0], 2);

        //    if (paramDomain.saturation.Length > 0)
        //    {
        //        temp[2, p] = System.Math.Pow(this.paramDomain.saturation[p] - estimate[0], 2);
        //    }
        //    else
        //    {
        //        temp[2, p] = System.Math.Pow(this.paramDomain.gamma[p] - estimate[0], 2);
        //        temp[3, p] = System.Math.Pow(this.paramDomain.lambda[p] - estimate[0], 2);
        //    }
        //}
        //Debug.Log(estimate[0] + ", " + estimate[1] + ", " + estimate[2] + "," + estimate[3]);

        for (int p = 0; p < this.posterior.Length; p++)
        {

            if (paramDomain.saturation.Length > 0)
            {
                mean[p] = (temp[0, p] + temp[1, p] + temp[2, p]) / 3f;
            }
            else
            {
                mean[p] = (temp[0, p] + temp[1, p] + temp[2, p] + temp[3, p]) / 4f;
                //Debug.Log(p);
                //Debug.Log(mean[p]);
            }

            mean[p] = System.Math.Sqrt(mean[p]);
        }

        double current_value = double.MaxValue;
        int current_index = 0;


        for (int p = 0; p < this.posterior.Length; p++)
        {
            if (mean[p] < current_value)
            {
                current_index = p;
                current_value = mean[p];
            }
        }
        //Debug.Log(current_index + "current index");

        int index2 = 0;
        if (paramDomain.saturation.Length > 0)
        {
            for (int saturation_idx = 0; saturation_idx < paramDomain.saturation.Length; saturation_idx++)
            {
                for (int sigma_idx = 0; sigma_idx < paramDomain.sigma.Length; sigma_idx++)
                {
                    for (int mu_idx = 0; mu_idx < paramDomain.mu.Length; mu_idx++)
                    {

                        if (index2 == current_index)
                        {
                            current_estimate_mu = paramDomain.mu[mu_idx];
                            current_estimate_sigma = paramDomain.sigma[sigma_idx];
                            current_estimate_saturation = paramDomain.saturation[saturation_idx];
                            this.history_estimate_mu.Add(current_estimate_mu);
                            this.history_estimate_sigma.Add(current_estimate_sigma);
                            this.history_estimate_saturation.Add(current_estimate_saturation);

                            return (new double[3] { current_estimate_mu, current_estimate_sigma, current_estimate_saturation });
                            //  break;
                        }
                        index2++;
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
                            if (index2 == current_index)
                            {
                                //Debug.Log(history_estimate_mu.Count);
                                current_estimate_mu = paramDomain.mu[mu_idx];
                                current_estimate_sigma = paramDomain.sigma[sigma_idx];
                                current_estimate_gamma = paramDomain.gamma[gamma_idx];
                                current_estimate_lambda = paramDomain.lambda[lambda_idx];
                                this.history_estimate_mu.Add(current_estimate_mu);
                                this.history_estimate_sigma.Add(current_estimate_sigma);
                                this.history_estimate_gamma.Add(current_estimate_gamma);
                                this.history_estimate_lambda.Add(current_estimate_lambda);


                                return (new double[4] { current_estimate_mu, current_estimate_sigma, current_estimate_gamma, current_estimate_lambda });

                            }
                            index2++;
                        }
                    }
                }
            }
        }

        return (new double[0]);

    }

    public bool IsFinished()
    {
        if (history_stim.Count >= this.maxNTrials)
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

    public void History(string filename, string subject_code = "", int session = 0, int quest_id = 0, bool persistent = false)
    {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        if (this.all_done == true)
        {
            this.trials = this.history_stim.Count;
        }
        var info = "";
        string header = "";

        if (paramDomain.saturation.Length > 0)
        {
            header = "trial, subject_code, session, quest_id, stimuli, response,  mu_estimated, sigma_estimated, saturation_estimated \n ";
            info = info + header;
            for (int i = 0; i < history_stim.Count; i++)
            {
                info = info + $"{i},{subject_code},{session},{quest_id},{this.history_stim[i]},{this.history_resp[i]},{this.history_estimate_mu[i]},{this.history_estimate_sigma[i]},{this.history_estimate_saturation[i]}\n";
            }
        }
        else
        {
            header = "trial, subject_code, session, quest_id, stimuli, response,  mu_estimated, sigma_estimated, gamma_estimated, lambda_estimated \n";
            info = info + header;
            for (int i = 0; i < history_stim.Count; i++)
            {
                info = info + $"{i},{subject_code},{session},{quest_id},{this.history_stim[i]},{this.history_resp[i]},{this.history_estimate_mu[i]},{this.history_estimate_sigma[i]},{this.history_estimate_gamma[i]}, {this.history_estimate_lambda[i]}\n";
            }
        }

        //Saving files
        if (persistent)
        {
            File.WriteAllText(Application.persistentDataPath + "/" + filename + ".csv", info);
            Debug.Log("Quest File saved to: " + Application.persistentDataPath);
        }
        else
        {
            File.WriteAllText(Application.streamingAssetsPath + "/" + filename + ".csv", info);
            Debug.Log("Quest File saved to: " + Application.streamingAssetsPath);
        }
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
        posterior = new double[paramDomain.Length];
        history_stim = new List<float> { };
        history_resp = new List<float> { };
        history_estimate_mu = new List<double> { };
        history_estimate_sigma = new List<double> { };
        history_estimate_gamma = new List<double> { };
        history_estimate_lambda = new List<double> { };
        history_estimate_saturation = new List<double> { };
        history_se = new List<float> { };
        current_se = 0;
        current_stim_ID = 0;
        current_estimate_mu = 0;
        current_estimate_sigma = 0;
        current_estimate_gamma = 0;
        current_estimate_lambda = 0;
        current_estimate_saturation = 0;
        trials = 0;
        all_done = false;
        start_mode = "";
    }

    //Constructor
    public Properties(float[] stimDomain, ParamDomain paramDomain, float[] respDomain, string[] stopRule, float stopCriterion, float minNTrials, float maxNTrials, double[,,] likelihood, double[] posterior, List<float> history_stim, List<float> history_resp, List<double> history_estimate_mu, List<double> history_estimate_sigma, List<double> history_estimate_gamma, List<double> history_estimate_lambda, List<double> history_estimate_saturation, List<float> history_se, string start_mode = "")
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
        this.history_estimate_gamma = history_estimate_gamma;
        this.history_estimate_lambda = history_estimate_lambda;
        this.history_estimate_saturation = history_estimate_saturation;
        this.history_se = history_se;
        this.current_se = 0;
        this.current_stim_ID = 0;
        this.current_estimate_mu = 0;
        this.current_estimate_sigma = 0;
        current_estimate_gamma = 0;
        current_estimate_lambda = 0;
        current_estimate_saturation = 0;
        this.trials = 0;
        this.all_done = false;
        this.start_mode = start_mode;
    }
    public Properties(float[] stimDomain, ParamDomain paramDomain, float[] respDomain, string[] stopRule, float stopCriterion, float minNTrials = 1, float maxNTrials = 10, string start_mode = "")
    {

        this.stimDomain = stimDomain;
        this.paramDomain = paramDomain;
        this.respDomain = respDomain;
        this.stopRule = stopRule;
        this.stopCriterion = stopCriterion;
        this.minNTrials = minNTrials;
        this.maxNTrials = maxNTrials;
        this.posterior = new double[paramDomain.Length];
        this.likelihoods = new double[0, 0, 0];
        this.history_resp = new List<float>();
        this.history_stim = new List<float>();
        this.history_estimate_mu = new List<double>();
        this.history_estimate_sigma = new List<double>();
        this.history_estimate_gamma = new List<double>();
        this.history_estimate_lambda = new List<double>();
        this.history_estimate_saturation = new List<double>();
        this.history_se = new List<float>();
        this.current_se = 0;
        this.current_stim_ID = 0;
        this.current_estimate_mu = 0;
        this.current_estimate_sigma = 0;
        current_estimate_gamma = 0;
        current_estimate_lambda = 0;
        current_estimate_saturation = 0;
        this.trials = 0;
        this.all_done = false;
        this.start_mode = start_mode;
    }
}