using MathNet.Numerics.Distributions;
using System.Collections.Generic;
using System.Linq;
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
        
        string[] stopRule = new string[1] { "stdev" }; // stop rule used to force the end of the presentation-update-cycle that estimates the subjects probable mu-value. Currently only the standard error as a stop rule is supported
        float stopCriterion = Mathf.PI / 120; // The value corresponding to the aforementioned stop rule 

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

        //ParamDomain paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, saturation_start, saturation_end, saturation_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu
        ParamDomain paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, gamma_start, gamma_end, gamma_steps, lambda_start, lambda_end, lambda_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu

        QPProperties.Add(new Properties(stimDomain, paramDomain, respDomain, stopRule, stopCriterion, minNTrials, maxNTrials)); // builds a new prop object containing the parameters set above

        QPProperties.Last().Init(); // used to initialise the mu measurement pipeline by creating likelihood and prior probabilities

        TargetStimulus currentStimulus = new TargetStimulus();
        //currentStimulus = QPProperties.Last().getTargetStim();

        bool isFinished = false;
        bool response;
        int counter = 0;

        //simulating quest run
        while (counter < 500 & !isFinished)
        {
            //get stimulus
            currentStimulus = QPProperties.Last().getTargetStim();

            //calculate response based on true cummulative distribution
            MathNet.Numerics.Distributions.Normal normal_dist = new MathNet.Numerics.Distributions.Normal(true_mu, true_sigma);
            double res = true_gamma + (1 - true_gamma - true_lambda) * (double)normal_dist.CumulativeDistribution(currentStimulus.value);
            //double res = true_saturation + (1 - true_saturation - true_saturation) * (double)normal_dist.CumulativeDistribution(currentStimulus.value);

            response = Random.Range(0, 1f) <= res;
            Debug.Log(currentStimulus.value + "," + res + " current stimulus + res");

            //answer to QuestPlus and Update
            isFinished = QPProperties.Last().UpdateEverything(response);

            //updating estimated parameters
            mu_all_estimates = QPProperties.Last().history_estimate_mu.ToArray();
            sigma_all_estimates = QPProperties.Last().history_estimate_sigma.ToArray();
            stimuli = QPProperties.Last().history_stim.ToArray();
            if (QPProperties.Last().paramDomain.saturation.Length > 0)
            {
                saturation_all_estimates = QPProperties.Last().history_estimate_saturation.ToArray();
            }
            else
            {
                gamma_all_estimates = QPProperties.Last().history_estimate_gamma.ToArray();
                lambda_all_estimates = QPProperties.Last().history_estimate_lambda.ToArray();
            }
            responses = QPProperties.Last().history_resp.ToArray();

            counter++;
        }

        //save data to csv
        QPProperties.Last().History("Testdaten_77", "1");

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
                    if (response_all_values[i]==1)
                    {
                        colors[i] = Color.blue;
                    }
                    else
                    {
                        colors[i] = Color.yellow;
                    }
                }
                Handles.color = Color.yellow;
                Handles.DrawAAPolyLine(width:3,colors: colors, all_values_vector);


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


