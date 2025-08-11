using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;


public class Simulator : MonoBehaviour
{
    public List<Properties> QPProperties;
    public double[] mu_all_estimates;
    public double[] sigma_all_estimates;
    public double[] gamma_all_estimates;
    public double[] lambda_all_estimates;
    public double[] saturation_all_estimates;
    public float[] responses;
    public float[] stimuli;

    public float grid_height = 0.5f;
    public int grid_width = 10;
    public int height = 200;
    public int width = 500;

    [SerializeField, Range(0, 100)]
    public int marker = 0;
    public int marker_index = 0;
    public float marker_se = 0;
    public float marker_mu = 0;
    public float marker_sigma = 0;
    public float marker_gamma = 0;
    public float marker_lambda = 0;
    public float marker_saturation = 0;
    public float marker_stimuli = 0;

    public float true_mu = -0.1f;
    public float true_sigma = 0.1f;
    public float true_saturation = 0.05f; //0.06 als Empfehlung
    public float true_gamma = 0.5f;
    public float true_lambda = 0.03f;

    public int minNTrials = 1; // the minimum amount of run trials before aborting measurement
    public int maxNTrials = 100; // if the stop rule "maxtrials" is used, maxNTrials is used to determine the maximum amount of trials

    float mu_start; // when estimating the true mu value, a starting point for this mu has to be given
    float mu_end; // also, an end point has to be given, to form the range of possible mu values
    int mu_steps; // the sensitivity of the mu measurement, i.e. how many steps are in the mu range

    float sigma_start; // the width of the density-functions used in this program
    float sigma_end;
    int sigma_steps;

    float gamma_start;
    float gamma_end;
    int gamma_steps;

    float lambda_start;
    float lambda_end;
    int lambda_steps;

    float saturation_start;
    float saturation_end;
    int saturation_steps;


    ParamDomain paramDomain ; // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu
    ParamDomain[] paramDomains;


    // Start is called before the first frame update
    void Start()
    {
        //set up quest run
        QPProperties = new List<Properties>();

        float stimDomainMin = 0f;
        float stimDomainMax = 0.21f;
        const int splitValue = 10;

        float[] stimDomain = new float[splitValue];
        float step = (stimDomainMax - stimDomainMin) / (splitValue - 1);
        for (int i = 0; i < splitValue; i++)
        {
            stimDomain[i] = stimDomainMin + i * step;
        }
        // stimulus domain // the range of values that is presented as stimuli to the subject. Can be any typpe of array

        float[] respDomain = new float[2] { 0, 1 }; // response domain // the range of possible answers given by the subject upon being presented with the above stimulus. Currently only the 2AFC- scenario is supported

        string stopRule = "stdev"; // stop rule used to force the end of the presentation-update-cycle that estimates the subjects probable mu-value. Currently only the standard error as a stop rule is supported
        float stopCriterion = Mathf.PI / 120; // The value corresponding to the aforementioned stop rule 

        ResetParamDomain();

        float minNTrials = 1; // the minimum amount of run trials before aborting measurement
        float maxNTrials = 100; // if the stop rule "maxtrials" is used, maxNTrials is used to determine the maximum amount of trials

        float mu_start = 0f; // when estimating the true mu value, a starting point for this mu has to be given
        float mu_end = 0.21f; // also, an end point has to be given, to form the range of possible mu values
        int mu_steps = 10; // the sensitivity of the mu measurement, i.e. how many steps are in the mu range

        float sigma_start = 0; // the deviation value for the density-functions used in this program
        float sigma_end = 0.2f;
        int sigma_steps = 5;

        float gamma_start = 0.5f;
        float gamma_end = 0.5f;
        int gamma_steps = 1;

        float lambda_start = 0.06f;
        float lambda_end = 0.06f;
        int lambda_steps = 1;

        float saturation_start = 0.05f;
        float saturation_end = 0.05f;
        int saturation_steps = 1;

        float true_mu = 0.05f;
        float true_sigma = 0.1f;
        float true_saturation = 0f; //0.06 als Empfehlung
        float true_gamma = 0.5f;
        float true_lambda = 0.03f;

        ParamDomain paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, saturation_start, saturation_end, saturation_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu
        QPProperties.Add(new Properties(stimDomain, paramDomain, respDomain, stopRule, stopCriterion, minNTrials, maxNTrials)); // builds a new prop object containing the parameters set above

        QPProperties.Last().Init(); // used to initialise the mu measurement pipeline by creating likelihood and prior probabilities
        TargetStimulus currentStimulus = new TargetStimulus();

        bool isFinished = false;
        bool response;
        int counter = 0;

        //for (int r = 1; r <= 4; r++)
        //{
        //    SimulateConstantStimuliSaturation(30, r, new float[] { -0.1f,0.1f }, new float[] { 0.001f,0.1f,0.2f,1 }, new float[] { true_saturation }, stimDomain, "constantStimuliReportingError"+r, Application.persistentDataPath);
        //}
        SimulateQuestPlusSaturation(30, maxNTrials, new float[] { 0, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.09f, 0.1f, 0.11f, 0.12f, 0.13f, 0.14f, 0.15f, 0.2f }, new float[] { 0.001f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 1 }, new float[] { true_saturation }, stimDomain, paramDomains, "Quest+", Application.persistentDataPath);
    }

    void ResetParamDomain()
    {
        mu_start = -Mathf.PI / 15; // when estimating the true mu value, a starting point for this mu has to be given
        mu_end = Mathf.PI / 15; // also, an end point has to be given, to form the range of possible mu values
        mu_steps = 25; // the sensitivity of the mu measurement, i.e. how many steps are in the mu range

        sigma_start = 0.3f; // the width of the density-functions used in this program
        sigma_end = 0.3f;
        sigma_steps = 1;

        gamma_start = 0.5f;
        gamma_end = 0.5f;
        gamma_steps = 1;

        lambda_start = 0.06f;
        lambda_end = 0.06f;
        lambda_steps = 1;


        saturation_start = 0.05f;
        saturation_end = 0.05f;
        saturation_steps = 1;
        //save data to csv
        QPProperties.Last().History("Testdaten_mu0_4_sigma0_4");


        paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, saturation_start, saturation_end, saturation_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu
        paramDomains = new ParamDomain[1] { paramDomain };
    }


    /// <summary>
    /// This method is used to simulate multiple Quest+ Runs.
    /// </summary>
    /// <param name="simulationRuns">How often do we simulate.</param>
    /// <param name="trials">How many trials does each simulation have?</param>
    /// <param name="trueMu">Array of underlying true Mue values (Mean of the distribution) for each simulation.</param>
    /// <param name="trueSigma">Array of underlying true Sigma values (Width of the distribution) for each simulation.</param>
    /// <param name="trueSaturation">Array of underlying true Saturation values (upper and lower asymptote) for each simulation.</param>
    /// <param name="steps">Array of stimuli to test.</param>
    /// <param name="paramDomains">paramDomain for each parameter combination, expected length = length(trueMu)*length(truSigma)*length(trueSaturation)</param>
    /// <param name="fileName">Name of the csv file, the data is saved in.</param>
    /// <param name="path">path where result file should be located.</param>
    void SimulateQuestPlusSaturation(int simulationRuns, int trials, float[] trueMu, float[] trueSigma, float[] trueSaturation, float[] steps, ParamDomain[] paramDomains, string fileName, string path = "")
    {
        SimulationResultsQuestPlus results = new SimulationResultsQuestPlus();

        int simulationId = 0;

        for (int trueMuIdx = 0; trueMuIdx < trueMu.Length; trueMuIdx++)
        {
            float mu = trueMu[trueMuIdx];
            for (int trueSigmaIdx = 0; trueSigmaIdx < trueSigma.Length; trueSigmaIdx++)
            {
                float sigma = trueSigma[trueSigmaIdx];
                for (int trueSaturationIdx = 0; trueSaturationIdx < trueSaturation.Length; trueSaturationIdx++)
                {
                    float saturation = trueSaturation[trueSaturationIdx];
                    float saturationX2 = (2 * saturation);
                    for (int simulationRun = 0; simulationRun < simulationRuns; simulationRun++)
                    {
                        //Debug.Log(maxTrials);
                        bool isFinished;
                        Debug.Log(simulationId);
                        Properties QP = new Properties(steps, paramDomains[0], new float[2] { 0, 1 }, " ", 0, 1, trials, start_mode: "median"); // builds a new prop object containing the parameters set above
                        QP.Init();
                        for (int trial = 1; trial <= trials; trial++) // runs * steps = trials
                        {
                            //correct response comes from this:
                            double currentStimulus = QP.getTargetStim().value;
                            MathNet.Numerics.Distributions.Normal normal_dist = new MathNet.Numerics.Distributions.Normal(mu, sigma); //define normal distribution with mu and sigma
                            double res = saturation + (1 - saturationX2) * (double)normal_dist.CumulativeDistribution(currentStimulus); //get likelihood for current stimulus from cumulative distribution with true mu, sigma and saturation

                            bool response = Random.Range(0, 1f) <= res; // compare to random number between 0 and 1 to get binary response that is distributed in the same way (false = below threshold --> left, true = above threshold --> right)
                            //bool response = 0.5f <= res; // compare to random number between 0 and 1 to get binary response that is distributed in the same way (false = below threshold --> left, true = above threshold --> right)

                            //implementing the wrong response that we instructed during the experiment
                            //if (currentStimulus<0) // if stimulus was leftward 
                            //{
                            //    //turn leftward stimulus to positive, to check if it crosses threshold
                            //    normal_dist = new MathNet.Numerics.Distributions.Normal(Mathf.Abs(mu), sigma); 
                            //    res = saturation + (1 - saturationX2) * (double)normal_dist.CumulativeDistribution(Mathf.Abs((float)currentStimulus)); 

                            //    if (Random.Range(0, 1f) <= res) // if stimulus was above (leftward) threshold,
                            //    { 
                            //        response = true; //subjects answered: left (stimulus:left, response :left --> response = true)

                            //        //correction
                            //        response = false;

                            //    }else
                            //    {
                            //        response = false;
                            //    }
                            //}
                            //else // stimulus is rightward
                            //{
                            //    //response stays the same
                            //}

                            isFinished = QP.UpdateEverything(response); // Update Quest+

                            results.AddResult(simulationId,simulationRun, trial, response, currentStimulus, mu, QP.current_estimate_mu, sigma, QP.current_estimate_sigma, saturation, QP.current_estimate_saturation);
                            if (isFinished)
                            {
                                //QP.History(fileName, path, simulationRun.ToString()); //save history data to be able to check Quest+ runs individually
                                break;
                            }
                        }
                        //updating estimated parameters for live view in editor
                        mu_all_estimates = QP.history_estimate_mu.ToArray();
                        sigma_all_estimates = QP.history_estimate_sigma.ToArray();
                        stimuli = QP.history_stim.ToArray();
                        if (QP.paramDomain.saturation.Length > 0)
                        {
                            saturation_all_estimates = QP.history_estimate_saturation.ToArray();
                        }
                        else
                        {
                            gamma_all_estimates = QP.history_estimate_gamma.ToArray();
                            lambda_all_estimates = QP.history_estimate_lambda.ToArray();
                        }
                        responses = QP.history_resp.ToArray();

                    }
                    simulationId++;

                    ResetParamDomain();
                }
            }
        }
        results.Save(fileName, path);
    }


    /// <summary>
    /// This method is used to simulate constant stimuli measurements.
    /// </summary>
    /// <param name="simulationRuns">How often do we simulate.</param>
    /// <param name="runs">How often do we repeat the same trial in each simulation.</param>
    /// <param name="trueMu">Array of underlying true Mue values (Mean of the distribution) for each simulation.</param>
    /// <param name="trueSigma">Array of underlying true Sigma values (Width of the distribution) for each simulation.</param>
    /// <param name="trueSaturation">Array of underlying true Saturation values (upper and lower asymptote) for each simulation.</param>
    /// <param name="steps">Array of stimuli to test.</param>
    /// <param name="fileName">Name of the csv file, the data is saved in.</param>
    /// <param name="path">path where result file should be located.</param>
    void SimulateConstantStimuliSaturation(int simulationRuns, int runs, float[] trueMu, float[] trueSigma, float[] trueSaturation, float[] steps, string fileName, string path = "")
    {
        SimulationResultsConstantStimuli results = new();

        int simulationId = 0;

        for (int trueMuIdx = 0; trueMuIdx < trueMu.Length; trueMuIdx++)
        {
            float mu = trueMu[trueMuIdx];
            for (int trueSigmaIdx = 0; trueSigmaIdx < trueSigma.Length; trueSigmaIdx++)
            {
                float sigma = trueSigma[trueSigmaIdx];

                for (int trueSaturationIdx = 0; trueSaturationIdx < trueSaturation.Length; trueSaturationIdx++)
                {
                    float saturation = trueSaturation[trueSaturationIdx];
                    float saturationX2 = (2 * saturation);
                    for (int simulationRun = 0; simulationRun < simulationRuns; simulationRun++)
                    {
                        int trial = 1;
                        for (int r = 0; r < runs; r++) // runs * steps = trials
                        {
                            MathNet.Numerics.Distributions.Normal normal_dist = new MathNet.Numerics.Distributions.Normal(mu, sigma); //define normal distribution with mu and sigma

                            // sample constant stimuli
                            for (int stepIdx = 0; stepIdx < steps.Length; stepIdx++)
                            {
                                double res = saturation + (1 - saturationX2) * (double)normal_dist.CumulativeDistribution(steps[stepIdx]); //get likelihood for current stimulus from cumulative distribution with true mu, sigma and saturation
                                bool response = Random.Range(0, 1f) <= res; // compare to random number between 0 and 1 to get binary response that is distributed in the same way.
                                
                                // in the old experiment we would have a rightward, random (below threshold), and leftward stage
                                
                                results.AddResult(simulationId, simulationRun, trial, response, steps[stepIdx], mu, sigma, saturation); //save data
                                trial++;
                            }
                        }
                    }
                    simulationId++;
                }
            }

        }
        results.Save(fileName, path);
    }

    private void Update()
    {
        //Update marker for live view in editor
        marker_index = (int)(((marker) / 100.0f) * mu_all_estimates.Length);
        if (marker_index >= mu_all_estimates.Length - 1)
        {
            marker_index = mu_all_estimates.Length - 1;
        }
        marker_sigma = (float)sigma_all_estimates[marker_index];
        marker_mu = (float)mu_all_estimates[marker_index];
        marker_stimuli = (float)stimuli[marker_index];
    }
}
#if UNITY_EDITOR

public class SimulationResultsConstantStimuli
{
    public List<int> simulationIds;
    public List<int> simulationRuns;
    public List<int> trials;
    public List<bool> responses;
    public List<float> stimuli;

    public List<float> mus;
    public List<float> sigmas;
    public List<float> saturations;
    public List<float> gammas;
    public List<float> lambdas;

    public SimulationResultsConstantStimuli()
    {
        this.simulationIds = new List<int>();
        this.simulationRuns = new List<int>();
        this.trials = new List<int>();
        this.responses = new List<bool>();
        this.stimuli = new List<float>();

        this.mus = new List<float>();
        this.sigmas = new List<float>();
        this.saturations = new List<float>();
        this.gammas = new List<float>();
        this.lambdas = new List<float>();
    }

    /// <summary>
    /// This class is used to add constant stimuli results.
    /// </summary>
    /// <param name="simulationId">Current simulation run.</param>
    /// <param name="simulationRun">Current simulation run.</param>
    /// <param name="trial">Current Trial.</param>
    /// <param name="response">Response of this trial.</param>
    /// <param name="stimulus">Stimulus of this trial.</param>
    /// <param name="mu">True underlying mu value.</param>
    /// <param name="sigma">True underlying sigma value</param>
    /// <param name="saturation">True underlying saturation value (defaults to 0).</param>
    /// <param name="gamma">True underlying gamma value (defaults to 0).</param>
    /// <param name="lambda">True underlying lambda value (defaults to 0).</param>
    public void AddResult(int simulationId,int simulationRun, int trial, bool response, float stimulus, float mu, float sigma, float saturation = 0, float gamma = 0, float lambda = 0)
    {
        simulationIds.Add(simulationId);
        simulationRuns.Add(simulationRun);
        trials.Add(trial);
        responses.Add(response);
        stimuli.Add(stimulus);

        mus.Add(mu);
        sigmas.Add(sigma);
        saturations.Add(saturation);
        gammas.Add(gamma);
        lambdas.Add(lambda);
    }

    /// <summary>
    /// This class is used to save constant stimuli results.
    /// </summary>
    /// <param name="fileName">name of the file will be [fileName].csv.</param>
    /// <param name="path">path to file (empty defaults to persistent) .</param>
    public void Save(string fileName, string path = "")
    {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        if (path.Equals(""))
        {
            path = Application.persistentDataPath;
        }

        string header = "simulationId,simulationRun,trial,response,stimulus,trueMu,trueSigma,trueSaturation,trueGamma,trueLambda\n"; // would be really nice to also fit psychometric function in C# to these results, but there is no simple library providing this, and I am too lazy to reimplement psignifit in c# atm
        string file = "" + header;

        for (int i = 0; i < simulationIds.Count; i++)
        {
            file = file + $"{this.simulationIds[i]},{this.simulationRuns[i]},{this.trials[i]},{this.responses[i]},{this.stimuli[i]},{this.mus[i]},{this.sigmas[i]},{this.saturations[i]},{this.gammas[i]},{this.lambdas[i]}\n";
        }
        if (path == "")
        {
            path = Application.persistentDataPath;
        }

        File.WriteAllText(path + "/" + fileName + ".csv", file);
        Debug.Log("Constant Stimuli Simulation results saved to: " + path + "/" + fileName);
    }
}
#endif


class SimulationResultsQuestPlus
{
    public List<int> simulationIds;
    public List<int> simulationRuns;
    public List<int> trials;
    public List<bool> responses;
    public List<double> stimuli;

    public List<float> mus;
    public List<float> sigmas;
    public List<float> saturations;
    public List<float> gammas;
    public List<float> lambdas;

    public List<double> QpMus;
    public List<double> QpSigmas;
    public List<double> QpSaturations;
    public List<double> QpGammas;
    public List<double> QpLambdas;

    public SimulationResultsQuestPlus()
    {
        this.simulationIds = new List<int>();
        this.simulationRuns = new List<int>();
        this.trials = new List<int>();
        this.responses = new List<bool>();
        this.stimuli = new List<double>();

        this.mus = new List<float>();
        this.sigmas = new List<float>();
        this.saturations = new List<float>();
        this.gammas = new List<float>();
        this.lambdas = new List<float>();

        this.QpMus = new List<double>();
        this.QpSigmas = new List<double>();
        this.QpSaturations = new List<double>();
        this.QpGammas = new List<double>();
        this.QpLambdas = new List<double>();
    }

    /// <summary>
    /// This class is used to add Quest+ results.
    /// </summary>
    /// <param name="simulationId">Current simulation id.</param>
    /// <param name="simulationRun">Current simulation run.</param>
    /// <param name="trial">Current Trial.</param>
    /// <param name="response">Response of this trial.</param>
    /// <param name="stimulus">Stimulus of this trial.</param>
    /// <param name="mu">True underlying mu value.</param>
    /// <param name="currentMu">estimated mu value of this trial.</param>
    /// <param name="sigma">True underlying sigma value</param>
    /// <param name="currentSigma">estimated sigma value of this trial.</param>
    /// <param name="saturation">True underlying saturation value (defaults to 0).</param>
    /// <param name="currentSaturation">estimated saturation value of this trial.</param>
    /// <param name="gamma">True underlying gamma value (defaults to 0).</param>
    /// <param name="currentGamma">estimated gamma value of this trial (defaults to 0).</param>
    /// <param name="lambda">True underlying lambda value (defaults to 0).</param>
    /// <param name="currentLambda">estimated lambda value of this trial (defaults to 0).</param>
    public void AddResult(int simulationId,int simulationRun, int trial, bool response, double stimulus, float mu, double currentMu, float sigma, double currentSigma, float saturation = 0, double currentSaturation = 0, float gamma = 0, double currentGamma = 0, float lambda = 0, double currentLambda = 0)
    {
        simulationIds.Add(simulationId);
        simulationRuns.Add(simulationRun);
        trials.Add(trial);
        responses.Add(response);
        stimuli.Add(stimulus);

        mus.Add(mu);
        sigmas.Add(sigma);
        saturations.Add(saturation);
        gammas.Add(gamma);
        lambdas.Add(lambda);

        QpMus.Add(currentMu);
        QpSigmas.Add(currentSigma);
        QpSaturations.Add(currentSaturation);
        QpGammas.Add(currentGamma);
        QpLambdas.Add(currentLambda);
    }

    /// <summary>
    /// This class is used to save Quest+ results.
    /// </summary>
    /// <param name="fileName">name of the file will be [fileName].csv.</param>
    /// <param name="path">path to file (empty defaults to persistent) .</param>
    public void Save(string fileName, string path = "")
    {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        if (path.Equals(""))
        {
            path = Application.persistentDataPath;
        }

        string header = "simulationId,simulationRun,trial,response,stimulus,trueMu,trueSigma,trueSaturation,trueGamma,trueLambda,estimatedMu,estimatedSigma,estimatedSaturation,estimatedGamma,estimatedLambda\n";
        string file = "" + header;

        for (int i = 0; i < simulationIds.Count; i++)
        {
            file = file + $"{this.simulationIds[i]},{this.simulationRuns[i]},{this.trials[i]},{this.responses[i]},{this.stimuli[i]},{this.mus[i]},{this.sigmas[i]},{this.saturations[i]},{this.gammas[i]},{this.lambdas[i]},{this.QpMus[i]},{this.QpSigmas[i]},{this.QpSaturations[i]},{this.QpGammas[i]},{this.QpLambdas[i]}\n";
        }
        File.WriteAllText(path + "/" + fileName + ".csv", file);
        Debug.Log("QuestPlus Simulation results saved to: " + path + "/" + fileName);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(Simulator))]
[CanEditMultipleObjects]

public class SimulatorEditor : Editor
{
    float[] se_all_values;
    SerializedProperty se_values;

    float[] mu_all_values;
    SerializedProperty mu_values;

    float[] stim_all_values;
    SerializedProperty stim_values;

    float[] sigma_all_values;
    SerializedProperty sigma_values;

    float[] gamma_all_values;
    SerializedProperty gamma_values;

    float[] lambda_all_values;
    SerializedProperty lambda_values;

    float[] saturation_all_values;
    SerializedProperty saturation_values;

    float[] response_all_values;
    SerializedProperty response_values;

    public float grid_height = 0.5f;
    public int grid_width = 10;
    public int height = 200;
    public int width;
    float ratio_x;
    public SerializedProperty grid_height_s;
    public SerializedProperty grid_width_s;
    public SerializedProperty height_s;
    public SerializedProperty width_s;
    public SerializedProperty marker_index;


    private void OnEnable()
    {
        mu_values = serializedObject.FindProperty("mu_all_estimates");
        sigma_values = serializedObject.FindProperty("sigma_all_estimates");

        saturation_values = serializedObject.FindProperty("saturation_all_values");

        stim_values = serializedObject.FindProperty("stimuli");

        gamma_values = serializedObject.FindProperty("gamma_all_values");
        lambda_values = serializedObject.FindProperty("lambda_all_values");

        se_values = serializedObject.FindProperty("se_all_estimates");
        response_values = serializedObject.FindProperty("responses");


        grid_height_s = serializedObject.FindProperty("grid_height");
        grid_width_s = serializedObject.FindProperty("grid_width");
        height_s = serializedObject.FindProperty("height");
        width_s = serializedObject.FindProperty("width");
        marker_index = serializedObject.FindProperty("marker_index");

    }
    private void OnDisable()
    {
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();

        ratio_x = 1;

        grid_height = grid_height_s.floatValue;
        if (grid_height == 0)
        {
            grid_height = 1;
        }
        grid_width = grid_width_s.intValue;
        height = height_s.intValue;
        width = width_s.intValue;

        EditorGUILayout.LabelField("Sigma: ", "scaled from min to max");
        Rect rect = GUILayoutUtility.GetRect(width, width, height, height);
        if (Event.current.type == EventType.Repaint)
        {
            GUI.BeginClip(rect);

            double min = 0;
            double range = 100;

            if (sigma_values.arraySize > 0)
            {
                ratio_x = width / (float)sigma_values.arraySize;
                sigma_all_values = new float[sigma_values.arraySize];
                for (int i = 0; i < sigma_all_values.Length; i++)
                {
                    sigma_all_values[i] = sigma_values.GetArrayElementAtIndex(i).floatValue;
                }
                double max = sigma_all_values.Max();
                min = sigma_all_values.Min();
                range = System.Math.Abs(max - min);

                Vector3[] all_values_vector = new Vector3[sigma_all_values.Length];
                for (int i = 0; i < all_values_vector.Length; i++)
                {
                    all_values_vector[i] = new Vector3(i * ratio_x, (float)(height - ((sigma_all_values[i] - min) / range) * height));
                    //Debug.Log(all_values_vector[i]);
                }
                Handles.color = Color.red;
                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);
            }


            //Gitter
            for (int w = 0; w < width; w++)
            {
                if (w % grid_width == 0)
                {
                    Vector3[] vline = new Vector3[2];
                    vline[0] = new Vector3(w * ratio_x, 0);
                    vline[1] = new Vector3(w * ratio_x, height);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
                }
                if (w == marker_index.intValue)
                {

                    Vector3[] vline = new Vector3[2];
                    vline[0] = new Vector3(w * ratio_x, 0);
                    vline[1] = new Vector3(w * ratio_x, height);

                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

                }
            }


            int h_0 = (int)(Mathf.Round((float)(((0 + (1 - (min % 1))) / range) * height)));

            int h_1 = (int)(Mathf.Round((((float)(grid_height / range) * height))));


            for (int h = 1; h < height; h++)
            {
                if ((h - h_0) % h_1 == 0)
                {
                    Vector3[] hline = new Vector3[2];
                    hline[0] = new Vector3(0, height - h);
                    hline[1] = new Vector3(width * ratio_x, height - h);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
                }
            }

            GUI.EndClip();

        }


        EditorGUILayout.LabelField("Mu & Stimuli: ", "scaled from min to max");
        Rect rect_2 = GUILayoutUtility.GetRect(width, width, height, height);

        if (Event.current.type == EventType.Repaint)
        {
            GUI.BeginClip(rect_2);

            float min_both = 0;
            float range_both = 100;
            if (mu_values.arraySize > 0)
            {
                mu_all_values = new float[mu_values.arraySize];
                stim_all_values = new float[stim_values.arraySize];
                response_all_values = new float[stim_values.arraySize];

                for (int i = 0; i < mu_all_values.Length; i++)
                {
                    mu_all_values[i] = mu_values.GetArrayElementAtIndex(i).floatValue;
                    stim_all_values[i] = stim_values.GetArrayElementAtIndex(i).floatValue;
                    response_all_values[i] = response_values.GetArrayElementAtIndex(i).floatValue;
                }
                float max_mu = mu_all_values.Max();
                float min_mu = mu_all_values.Min();

                float max_stim = stim_all_values.Max();
                float min_stim = stim_all_values.Min();

                min_both = Mathf.Min(min_mu, min_stim);
                float max_both = Mathf.Max(max_mu, max_stim);
                range_both = Mathf.Abs(max_both - min_both);

                Vector3[] all_values_vector = new Vector3[mu_all_values.Length];
                for (int i = 0; i < all_values_vector.Length; i++)
                {
                    all_values_vector[i] = new Vector3(i * ratio_x, height - ((mu_all_values[i] - min_both) / range_both) * height);

                    //Debug.Log(all_values_vector[i]);
                }
                Handles.color = Color.red;
                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);

                Color[] colors = new Color[response_all_values.Length];
                all_values_vector = new Vector3[mu_all_values.Length];
                for (int i = 0; i < all_values_vector.Length; i++)
                {
                    all_values_vector[i] = new Vector3(i * ratio_x, height - ((stim_all_values[i] - min_both) / range_both) * height);
                    if (response_all_values[i] == 1)
                    {
                        colors[i] = Color.blue;
                    }
                    else
                    {
                        colors[i] = Color.yellow;
                    }
                }
                Handles.color = Color.yellow;
                Handles.DrawAAPolyLine(width: 3, colors: colors, all_values_vector);


            }

            //Gitter
            for (int w = 0; w < width; w++)
            {
                if (w % grid_width == 0)
                {
                    Vector3[] vline = new Vector3[2];
                    vline[0] = new Vector3(w * ratio_x, 0);
                    vline[1] = new Vector3(w * ratio_x, height);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
                }
                if (w == marker_index.intValue)
                {
                    Vector3[] vline = new Vector3[2];
                    vline[0] = new Vector3(w * ratio_x, 0);
                    vline[1] = new Vector3(w * ratio_x, height);

                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

                }
            }

            int h_0 = (int)(Mathf.Round((((0 + (1 - (min_both % 1))) / range_both) * height)));

            int h_1 = (int)(Mathf.Round(((grid_height / range_both) * height)));


            for (int h = 1; h < height; h++)
            {
                if ((h - h_0) % h_1 == 0)
                {
                    Vector3[] hline = new Vector3[2];
                    hline[0] = new Vector3(0, height - h);
                    hline[1] = new Vector3(width * ratio_x, height - h);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
                }
            }

            GUI.EndClip();

        }
    }
}
#endif