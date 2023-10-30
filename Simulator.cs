using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Purchasing;
using UnityEngine;

public class Simulator : MonoBehaviour
{
    public List<Properties> QPProperties;
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
        QPProperties = new List<Properties>();
        float stimDomainMin = 0.1f;
        float stimDomainMax = 100;
        const int splitValue = 2;
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
        float maxNTrials = 63; // if the stop rule "maxtrials" is used, maxNTrials is used to determine the maximum amount of trials

        float mu_start = 10; // when estimating the true mu value, a starting point for this mu has to be given
        float mu_end = 20; // also, an end point has to be given, to form the range of possible mu values
        int mu_steps = 2; // the sensitivity of the mu measurement, i.e. how many steps are in the mu range
        float sigma_start = 0; // the deviation value for the density-functions used in this program
        float sigma_end = 1;
        int sigma_steps = 2;

        float gamma_start = 0;
        float gamma_end = 0.5f;
        int gamma_steps = 2;
        
        float lambda_start = 0;
        float lambda_end = 0.02f;
        int lambda_steps = 2;

        float saturation_start = 0;
        float saturation_end = 0.05f;
        int saturation_steps = 5;

        //ParamDomain paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, saturation_start, saturation_end, saturation_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu
        ParamDomain paramDomain_ = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, gamma_start, gamma_end, gamma_steps, lambda_start, lambda_end, lambda_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu


        QPProperties.Add(new Properties(stimDomain, paramDomain_, respDomain, stopRule, stopCriterion, minNTrials, maxNTrials)); // builds a new prop object containing the parameters set above

        QPProperties.Last().Init(); // used to initialise the mu measurement pipeline by creating likelihood and prior probabilities

        TargetStimulus currentStimulus = new TargetStimulus();


    //    bool isFinished = false;
    //    bool response;
    //    float skill = Mathf.PI/60;
    //    int counter = 0;
    //    while (counter < 1000 & !isFinished)
    //    {
    //        //currentStimulus = questPlus.GetTargetStim();
    //        counter++;
    //        if (currentStimulus.value >= skill)
    //        {
    //            response = true; //rechts
    //            Debug.Log("Stimulus Value: "+ currentStimulus.value +" Index: "+ currentStimulus.index+ ": rechts");
    //        }
    //        else
    //        {
    //            response = false; //links
    //            Debug.Log("Stimulus Value: " + currentStimulus.value + " Index: " + currentStimulus.index + ": links");
    //        }

    //        isFinished = QPProperties.Last().UpdateEverything(response);

    //        //mu_all_estimates = questPlus.history_estimate.ToArray();
    //        se_all_estimates = QPProperties.Last().history_se.ToArray();
    //        Debug.Log("is finished? " + isFinished);

    //    }
    //    QPProperties.Last().History();
    //}

    //// Update is called once per frame
    //void Update()
    //{
    //    marker_index = marker_index % se_all_estimates.Length;
    //    marker_se = se_all_estimates[marker_index];
    //    marker_mu = mu_all_estimates[marker_index];
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

    float grid_height = 0.5f;
    int grid_width = 10;
    int height = 200;
    SerializedProperty grid_height_s;
    SerializedProperty grid_width_s;
    SerializedProperty height_s;


    SerializedProperty marker_index;


    private void OnEnable()
    {
        mu_values = serializedObject.FindProperty("mu_all_estimates");
        se_values = serializedObject.FindProperty("se_all_estimates");

        grid_height_s = serializedObject.FindProperty("grid_height");
        grid_width_s = serializedObject.FindProperty("grid_width");
        height_s = serializedObject.FindProperty("height");
        marker_index = serializedObject.FindProperty("marker_index");
    }
    private void OnDisable()
    {
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();

        int width = se_values.arraySize;

        grid_height = grid_height_s.floatValue;
        if (grid_height == 0)
        {
            grid_height = 1;
        }
        grid_width = grid_width_s.intValue;
        height = height_s.intValue;

        EditorGUILayout.LabelField("SE: ", "scaled from min to max");
        Rect rect = GUILayoutUtility.GetRect(width, width, height, height);
        if (Event.current.type == EventType.Repaint)
        {
            GUI.BeginClip(rect);

            float ratio = 50;
            float min = 0;
            float range = 100;
            if (se_values.arraySize > 0)
            {
                se_all_values = new float[se_values.arraySize];
                for (int i = 0; i < se_all_values.Length; i++)
                {
                    se_all_values[i] = se_values.GetArrayElementAtIndex(i).floatValue;
                }
                float max = se_all_values.Max();
                min = se_all_values.Min();
                range = Mathf.Abs(max - min);
                ratio = ((1 / (range)) * height);

                Vector3[] all_values_vector = new Vector3[se_all_values.Length];
                for (int i = 0; i < all_values_vector.Length; i++)
                {
                    all_values_vector[i] = new Vector3(i, height - ((se_all_values[i] - min) / range) * height);
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
                    vline[0] = new Vector3(w, 0);
                    vline[1] = new Vector3(w, height);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
                }
                if (w == marker_index.intValue)
                {

                    Vector3[] vline = new Vector3[2];
                    vline[0] = new Vector3(w, 0);
                    vline[1] = new Vector3(w, height);

                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

                }
            }


            int h_0 = (int)(Mathf.Round((((0 + (1 - (min % 1))) / range) * height)));

            int h_1 = (int)(Mathf.Round(((grid_height / range) * height)));


            for (int h = 1; h < height; h++)
            {
                if ((h - h_0) % h_1 == 0)
                {
                    Vector3[] hline = new Vector3[2];
                    hline[0] = new Vector3(0, height - h);
                    hline[1] = new Vector3(width, height - h);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
                }
            }

            GUI.EndClip();

        }


        EditorGUILayout.LabelField("Mu: ", "scaled from min to max");
        Rect rect_2 = GUILayoutUtility.GetRect(width, width, height, height);

        if (Event.current.type == EventType.Repaint)
        {
            GUI.BeginClip(rect_2);

            float ratio_mu = 50;
            float min_mu = 0;
            float range_mu = 100;
            if (mu_values.arraySize > 0)
            {
                mu_all_values = new float[mu_values.arraySize];
                for (int i = 0; i < mu_all_values.Length; i++)
                {
                    mu_all_values[i] = mu_values.GetArrayElementAtIndex(i).floatValue;
                }
                float max = mu_all_values.Max();
                min_mu = mu_all_values.Min();
                range_mu = Mathf.Abs(max - min_mu);
                ratio_mu = ((1 / (range_mu)) * height);

                Vector3[] all_values_vector = new Vector3[mu_all_values.Length];
                for (int i = 0; i < all_values_vector.Length; i++)
                {
                    all_values_vector[i] = new Vector3(i, height - ((mu_all_values[i] - min_mu) / range_mu) * height);
                    //Debug.Log(all_values_vector[i]);
                }
                Handles.color = Color.blue;
                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);

            }

            //Gitter
            for (int w = 0; w < width; w++)
            {
                if (w % grid_width == 0)
                {
                    Vector3[] vline = new Vector3[2];
                    vline[0] = new Vector3(w, 0);
                    vline[1] = new Vector3(w, height);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
                }
                if (w == marker_index.intValue)
                {
                    Vector3[] vline = new Vector3[2];
                    vline[0] = new Vector3(w, 0);
                    vline[1] = new Vector3(w, height);

                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

                }
            }

            int h_0 = (int)(Mathf.Round((((0 + (1 - (min_mu % 1))) / range_mu) * height)));

            int h_1 = (int)(Mathf.Round(((grid_height / range_mu) * height)));


            for (int h = 1; h < height; h++)
            {
                if ((h - h_0) % h_1 == 0)
                {
                    Vector3[] hline = new Vector3[2];
                    hline[0] = new Vector3(0, height - h);
                    hline[1] = new Vector3(width, height - h);

                    Handles.color = Color.grey;
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
                }
            }

            GUI.EndClip();

        }
    }
}

