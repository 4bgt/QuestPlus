using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Purchasing;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Input;

public class Simulator : MonoBehaviour
{
    public List<Properties> QPProperties;
    public double[] mu_all_estimates;
    public double[] sigma_all_estimates;
    public double[] gamma_all_estimates;
    public double[] lambda_all_estimates;
    public double[] saturation_all_estimates;
    public float[] se_all_estimates;
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
        QPProperties = new List<Properties>();
        float stimDomainMin = -Mathf.PI / 15;
        float stimDomainMax = Mathf.PI / 15;
        const int splitValue = 50;
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
        float maxNTrials = 30; // if the stop rule "maxtrials" is used, maxNTrials is used to determine the maximum amount of trials

        float mu_start = -Mathf.PI / 45; // when estimating the true mu value, a starting point for this mu has to be given
        float mu_end = Mathf.PI / 45; // also, an end point has to be given, to form the range of possible mu values
        int mu_steps = 30; // the sensitivity of the mu measurement, i.e. how many steps are in the mu range
        float sigma_start = 0; // the deviation value for the density-functions used in this program
        float sigma_end = 3;
        int sigma_steps = 10;

        float gamma_start = 0;
        float gamma_end = 0.5f;
        int gamma_steps = 10;

        float lambda_start = 0;
        float lambda_end = 0.02f;
        int lambda_steps = 3;

        float saturation_start = 0;
        float saturation_end = 0.05f;
        int saturation_steps = 5;

        ParamDomain paramDomain = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, saturation_start, saturation_end, saturation_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu
        ParamDomain paramDomain_ = new ParamDomain(mu_start, mu_end, mu_steps, sigma_start, sigma_end, sigma_steps, gamma_start, gamma_end, gamma_steps, lambda_start, lambda_end, lambda_steps); // parameter domain // includes all values that go into the cumulative distribution function used to estimate mu


        QPProperties.Add(new Properties(stimDomain, paramDomain_, respDomain, stopRule, stopCriterion, minNTrials, maxNTrials)); // builds a new prop object containing the parameters set above

        QPProperties.Last().Init(); // used to initialise the mu measurement pipeline by creating likelihood and prior probabilities

        TargetStimulus currentStimulus = new TargetStimulus();
        //currentStimulus = QPProperties.Last().getTargetStim();

        bool isFinished = false;
        bool response;
        float skill = Mathf.PI / 60;
        int counter = 0;
        Debug.Log(QPProperties.Last().current_estimate_mu + "," + QPProperties.Last().current_estimate_sigma + "," + QPProperties.Last().current_estimate_gamma + "," + QPProperties.Last().current_estimate_lambda);
        while (counter < 100 & !isFinished)
        {
            currentStimulus = QPProperties.Last().getTargetStim();
            counter++;
            if (currentStimulus.value >= skill)
            {
                response = true; //rechts
                Debug.Log("Stimulus Value: " + currentStimulus.value + " Index: " + currentStimulus.index + ": rechts");
            }
            else
            {
                response = false; //links
                Debug.Log("Stimulus Value: " + currentStimulus.value + " Index: " + currentStimulus.index + ": links");
            }
            Debug.Log(QPProperties.Last().current_estimate_mu + "," + QPProperties.Last().current_estimate_sigma + "," + QPProperties.Last().current_estimate_gamma + "," + QPProperties.Last().current_estimate_lambda);
            Debug.Log(counter + " counter ");
            isFinished = QPProperties.Last().UpdateEverything(response);

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
            se_all_estimates = QPProperties.Last().history_se.ToArray();
            Debug.Log("is finished? " + isFinished);

        }
        QPProperties.Last().History("Testdaten");
        
    }
    private void Update()
    {
        marker_index = (int)(((marker) / 100.0f) * mu_all_estimates.Length);
        if (marker_index >= mu_all_estimates.Length - 1)
        {
            marker_index = mu_all_estimates.Length - 1;
        }
        marker_sigma = (float)sigma_all_estimates[marker_index];
        marker_mu = (float)mu_all_estimates[marker_index];
        marker_stimuli = (float)stimuli[marker_index];
        InputDevice eyeTrackingDevice = default(InputDevice);
        if (!eyeTrackingDevice.isValid)
        {
            List<InputDevice> InputDeviceList = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, InputDeviceList);
            if (InputDeviceList.Count > 0)
            {
                eyeTrackingDevice = InputDeviceList[0];
                Debug.Log("works " + eyeTrackingDevice.name);
            }

            if (!eyeTrackingDevice.isValid)
            {
                Debug.LogWarning($"Unable to acquire eye tracking device. Have permissions been granted?");
                return;
            }
        }
        bool hasData = eyeTrackingDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked);
        hasData &= eyeTrackingDevice.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out Vector3 position);
        hasData &= eyeTrackingDevice.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out Quaternion rotation);

        if (isTracked && hasData)
        { 

            transform.localPosition = position + (rotation * Vector3.forward);
            transform.localRotation = rotation;
        }
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    marker_index = marker_index % se_all_estimates.Length;
    //    marker_se = se_all_estimates[marker_index];
    //    marker_mu = mu_all_estimates[marker_index];
}
//[CustomEditor(typeof(Simulator))]
//[CanEditMultipleObjects]
//public class SimulatorEditor : Editor
//{
//    public List<Properties> QPProperties;
//    float[] se_all_values;
//    SerializedProperty se_values;

//    double[] mu_all_values;
//    SerializedProperty mu_values;

//    double[] sigma_all_values;
//    SerializedProperty sigma_values;

//    double[] gamma_all_values;
//    SerializedProperty gamma_values;

//    double[] lambda_all_values;
//    SerializedProperty lambda_values;

//    double[] saturation_all_values;
//    SerializedProperty saturation_values;


//    float grid_height = 0.5f;
//    int grid_width = 10;
//    int height = 200;
//    SerializedProperty grid_height_s;
//    SerializedProperty grid_width_s;
//    SerializedProperty height_s;


//    SerializedProperty marker_index;


//    private void OnEnable()
//    {
//        if (EditorApplication.isPlaying)
//        {
//            mu_values = serializedObject.FindProperty("mu_all_estimates");
//            sigma_values = serializedObject.FindProperty("sigma_all_estimates");

//            saturation_values = serializedObject.FindProperty("saturation_all_values");



//            gamma_values = serializedObject.FindProperty("gamma_all_values");
//            lambda_values = serializedObject.FindProperty("lambda_all_values");

//            se_values = serializedObject.FindProperty("se_all_estimates");

//            grid_height_s = serializedObject.FindProperty("grid_height");
//            grid_width_s = serializedObject.FindProperty("grid_width");
//            height_s = serializedObject.FindProperty("height");
//            marker_index = serializedObject.FindProperty("marker_index");
//        }
//        else
//        {
//            mu_values = null;
//            sigma_values = null;

//            saturation_values = null;


//            gamma_values = null;
//            lambda_values = null;

//            se_values = null;

//            grid_height_s = null;
//            grid_width_s = null;
//            height_s = null;
//            marker_index = null;
//        }

//    }
//    private void OnDisable()
//    {
//    }

//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();
//        serializedObject.Update();

//        int width = se_values.arraySize;

//        grid_height = grid_height_s.floatValue;
//        if (grid_height == 0)
//        {
//            grid_height = 1;
//        }
//        grid_width = grid_width_s.intValue;
//        height = height_s.intValue;

//        EditorGUILayout.LabelField("SE: ", "scaled from min to max");
//        Rect rect = GUILayoutUtility.GetRect(width, width, height, height);
//        if (Event.current.type == EventType.Repaint)
//        {
//            GUI.BeginClip(rect);

//            float ratio = 50;
//            float min = 0;
//            float range = 100;
//            if (se_values.arraySize > 0)
//            {
//                se_all_values = new float[se_values.arraySize];
//                for (int i = 0; i < se_all_values.Length; i++)
//                {
//                    se_all_values[i] = se_values.GetArrayElementAtIndex(i).floatValue;
//                }
//                float max = se_all_values.Max();
//                min = se_all_values.Min();
//                range = Mathf.Abs(max - min);
//                ratio = ((1 / (range)) * height);

//                Vector3[] all_values_vector = new Vector3[se_all_values.Length];
//                for (int i = 0; i < all_values_vector.Length; i++)
//                {
//                    all_values_vector[i] = new Vector3(i, height - ((se_all_values[i] - min) / range) * height);
//                    //Debug.Log(all_values_vector[i]);
//                }
//                Handles.color = Color.red;
//                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);
//            }


//            //Gitter
//            for (int w = 0; w < width; w++)
//            {
//                if (w % grid_width == 0)
//                {
//                    Vector3[] vline = new Vector3[2];
//                    vline[0] = new Vector3(w, 0);
//                    vline[1] = new Vector3(w, height);

//                    Handles.color = Color.grey;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
//                }
//                if (w == marker_index.intValue)
//                {

//                    Vector3[] vline = new Vector3[2];
//                    vline[0] = new Vector3(w, 0);
//                    vline[1] = new Vector3(w, height);

//                    Handles.color = Color.green;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

//                }
//            }


//            int h_0 = (int)(Mathf.Round((((0 + (1 - (min % 1))) / range) * height)));

//            int h_1 = (int)(Mathf.Round(((grid_height / range) * height)));


//            for (int h = 1; h < height; h++)
//            {
//                if ((h - h_0) % h_1 == 0)
//                {
//                    Vector3[] hline = new Vector3[2];
//                    hline[0] = new Vector3(0, height - h);
//                    hline[1] = new Vector3(width, height - h);

//                    Handles.color = Color.grey;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
//                }
//            }

//            GUI.EndClip();

//        }


//        EditorGUILayout.LabelField("Mu: ", "scaled from min to max");
//        Rect rect_2 = GUILayoutUtility.GetRect(width, width, height, height);

//        if (Event.current.type == EventType.Repaint)
//        {
//            GUI.BeginClip(rect_2);

//            double ratio_mu = 50;
//            double min_mu = 0;
//            double range_mu = 100;
//            if (mu_values.arraySize > 0)
//            {
//                mu_all_values = new double[mu_values.arraySize];
//                for (int i = 0; i < mu_all_values.Length; i++)
//                {
//                    mu_all_values[i] = mu_values.GetArrayElementAtIndex(i).floatValue;
//                }
//                double max = mu_all_values.Max();
//                min_mu = mu_all_values.Min();
//                range_mu = Mathf.Abs((float)(max - min_mu));
//                ratio_mu = ((1 / (range_mu)) * height);

//                Vector3[] all_values_vector = new Vector3[mu_all_values.Length];
//                for (int i = 0; i < all_values_vector.Length; i++)
//                {
//                    all_values_vector[i] = new Vector3(i, (float)(height - ((mu_all_values[i] - min_mu) / range_mu) * height));
//                    //Debug.Log(all_values_vector[i]);
//                }
//                Handles.color = Color.blue;
//                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);

//            }

//            //Gitter
//            for (int w = 0; w < width; w++)
//            {
//                if (w % grid_width == 0)
//                {
//                    Vector3[] vline = new Vector3[2];
//                    vline[0] = new Vector3(w, 0);
//                    vline[1] = new Vector3(w, height);

//                    Handles.color = Color.grey;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
//                }
//                if (w == marker_index.intValue)
//                {
//                    Vector3[] vline = new Vector3[2];
//                    vline[0] = new Vector3(w, 0);
//                    vline[1] = new Vector3(w, height);

//                    Handles.color = Color.green;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

//                }
//            }

//            int h_0 = ((int)Mathf.Round((float)(((0 + (1 - (min_mu % 1))) / range_mu) * height)));

//            int h_1 = ((int)Mathf.Round((float)((grid_height / range_mu) * height)));


//            for (int h = 1; h < height; h++)
//            {
//                if ((h - h_0) % h_1 == 0)
//                {
//                    Vector3[] hline = new Vector3[2];
//                    hline[0] = new Vector3(0, height - h);
//                    hline[1] = new Vector3(width, height - h);

//                    Handles.color = Color.grey;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
//                }
//            }

//            GUI.EndClip();

//        }
//        EditorGUILayout.LabelField("Sigma: ", "scaled from min to max");
//        Rect rect_3 = GUILayoutUtility.GetRect(width, width, height, height);

//        if (Event.current.type == EventType.Repaint)
//        {
//            GUI.BeginClip(rect_3);

//            double ratio_sigma = 50;
//            double min_sigma = 0;
//            double range_sigma = 100;
//            if (sigma_values.arraySize > 0)
//            {
//                sigma_all_values = new double[sigma_values.arraySize];
//                for (int i = 0; i < sigma_all_values.Length; i++)
//                {
//                    sigma_all_values[i] = sigma_values.GetArrayElementAtIndex(i).floatValue;
//                }
//                double max = sigma_all_values.Max();
//                min_sigma = sigma_all_values.Min();
//                range_sigma = Mathf.Abs((float)(max - min_sigma));
//                ratio_sigma = ((1 / (range_sigma)) * height);

//                Vector3[] all_values_vector = new Vector3[sigma_all_values.Length];
//                for (int i = 0; i < all_values_vector.Length; i++)
//                {
//                    all_values_vector[i] = new Vector3(i, (float)(height - ((sigma_all_values[i] - min_sigma) / range_sigma) * height));
//                    //Debug.Log(all_values_vector[i]);
//                }
//                Handles.color = Color.blue;
//                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);

//            }

//            //Gitter
//            for (int w = 0; w < width; w++)
//            {
//                if (w % grid_width == 0)
//                {
//                    Vector3[] vline = new Vector3[2];
//                    vline[0] = new Vector3(w, 0);
//                    vline[1] = new Vector3(w, height);

//                    Handles.color = Color.grey;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
//                }
//                if (w == marker_index.intValue)
//                {
//                    Vector3[] vline = new Vector3[2];
//                    vline[0] = new Vector3(w, 0);
//                    vline[1] = new Vector3(w, height);

//                    Handles.color = Color.green;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

//                }
//            }

//            int h_0 = ((int)Mathf.Round((float)(((0 + (1 - (min_sigma % 1))) / range_sigma) * height)));

//            int h_1 = ((int)Mathf.Round((float)((grid_height / range_sigma) * height)));


//            for (int h = 1; h < height; h++)
//            {
//                if ((h - h_0) % h_1 == 0)
//                {
//                    Vector3[] hline = new Vector3[2];
//                    hline[0] = new Vector3(0, height - h);
//                    hline[1] = new Vector3(width, height - h);

//                    Handles.color = Color.grey;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
//                }
//            }

//            GUI.EndClip();

//        }

//        if (QPProperties.Last().paramDomain.saturation.Length > 0)
//        {
//            EditorGUILayout.LabelField("Saturation: ", "scaled from min to max");
//            Rect rect_6 = GUILayoutUtility.GetRect(width, width, height, height);

//            if (Event.current.type == EventType.Repaint)
//            {
//                GUI.BeginClip(rect_6);

//                double ratio_saturation = 50;
//                double min_saturation = 0;
//                double range_saturation = 100;
//                if (saturation_values.arraySize > 0)
//                {
//                    saturation_all_values = new double[saturation_values.arraySize];
//                    for (int i = 0; i < saturation_all_values.Length; i++)
//                    {
//                        saturation_all_values[i] = saturation_values.GetArrayElementAtIndex(i).floatValue;
//                    }
//                    double max = saturation_all_values.Max();
//                    min_saturation = saturation_all_values.Min();
//                    range_saturation = Mathf.Abs((float)(max - min_saturation));
//                    ratio_saturation = ((1 / (range_saturation)) * height);

//                    Vector3[] all_values_vector = new Vector3[saturation_all_values.Length];
//                    for (int i = 0; i < all_values_vector.Length; i++)
//                    {
//                        all_values_vector[i] = new Vector3(i, (float)(height - ((saturation_all_values[i] - min_saturation) / range_saturation) * height));
//                        //Debug.Log(all_values_vector[i]);
//                    }
//                    Handles.color = Color.blue;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);

//                }

//                //Gitter
//                for (int w = 0; w < width; w++)
//                {
//                    if (w % grid_width == 0)
//                    {
//                        Vector3[] vline = new Vector3[2];
//                        vline[0] = new Vector3(w, 0);
//                        vline[1] = new Vector3(w, height);

//                        Handles.color = Color.grey;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
//                    }
//                    if (w == marker_index.intValue)
//                    {
//                        Vector3[] vline = new Vector3[2];
//                        vline[0] = new Vector3(w, 0);
//                        vline[1] = new Vector3(w, height);

//                        Handles.color = Color.green;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

//                    }
//                }

//                int h_0 = ((int)Mathf.Round((float)(((0 + (1 - (min_saturation % 1))) / range_saturation) * height)));

//                int h_1 = ((int)Mathf.Round((float)((grid_height / range_saturation) * height)));


//                for (int h = 1; h < height; h++)
//                {
//                    if ((h - h_0) % h_1 == 0)
//                    {
//                        Vector3[] hline = new Vector3[2];
//                        hline[0] = new Vector3(0, height - h);
//                        hline[1] = new Vector3(width, height - h);

//                        Handles.color = Color.grey;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
//                    }
//                }

//                GUI.EndClip();

//            }
//        }
//        else
//        {
//            EditorGUILayout.LabelField("Gamma: ", "scaled from min to max");
//            Rect rect_4 = GUILayoutUtility.GetRect(width, width, height, height);

//            if (Event.current.type == EventType.Repaint)
//            {
//                GUI.BeginClip(rect_4);

//                double ratio_gamma = 50;
//                double min_gamma = 0;
//                double range_gamma = 100;
//                if (gamma_values.arraySize > 0)
//                {
//                    gamma_all_values = new double[gamma_values.arraySize];
//                    for (int i = 0; i < gamma_all_values.Length; i++)
//                    {
//                        gamma_all_values[i] = gamma_values.GetArrayElementAtIndex(i).floatValue;
//                    }
//                    double max = gamma_all_values.Max();
//                    min_gamma = gamma_all_values.Min();
//                    range_gamma = Mathf.Abs((float)(max - min_gamma));
//                    ratio_gamma = ((1 / (range_gamma)) * height);

//                    Vector3[] all_values_vector = new Vector3[gamma_all_values.Length];
//                    for (int i = 0; i < all_values_vector.Length; i++)
//                    {
//                        all_values_vector[i] = new Vector3(i, (float)(height - ((sigma_all_values[i] - min_gamma) / range_gamma) * height));
//                        //Debug.Log(all_values_vector[i]);
//                    }
//                    Handles.color = Color.blue;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);

//                }

//                //Gitter
//                for (int w = 0; w < width; w++)
//                {
//                    if (w % grid_width == 0)
//                    {
//                        Vector3[] vline = new Vector3[2];
//                        vline[0] = new Vector3(w, 0);
//                        vline[1] = new Vector3(w, height);

//                        Handles.color = Color.grey;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
//                    }
//                    if (w == marker_index.intValue)
//                    {
//                        Vector3[] vline = new Vector3[2];
//                        vline[0] = new Vector3(w, 0);
//                        vline[1] = new Vector3(w, height);

//                        Handles.color = Color.green;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

//                    }
//                }

//                int h_0 = ((int)Mathf.Round((float)(((0 + (1 - (min_gamma % 1))) / range_gamma) * height)));

//                int h_1 = ((int)Mathf.Round((float)((grid_height / range_gamma) * height)));


//                for (int h = 1; h < height; h++)
//                {
//                    if ((h - h_0) % h_1 == 0)
//                    {
//                        Vector3[] hline = new Vector3[2];
//                        hline[0] = new Vector3(0, height - h);
//                        hline[1] = new Vector3(width, height - h);

//                        Handles.color = Color.grey;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
//                    }
//                }

//                GUI.EndClip();

//            }
//            EditorGUILayout.LabelField("Lambda: ", "scaled from min to max");
//            Rect rect_5 = GUILayoutUtility.GetRect(width, width, height, height);

//            if (Event.current.type == EventType.Repaint)
//            {
//                GUI.BeginClip(rect_5);

//                double ratio_lambda = 50;
//                double min_lambda = 0;
//                double range_lambda = 100;
//                if (lambda_values.arraySize > 0)
//                {
//                    lambda_all_values = new double[lambda_values.arraySize];
//                    for (int i = 0; i < lambda_all_values.Length; i++)
//                    {
//                        lambda_all_values[i] = lambda_values.GetArrayElementAtIndex(i).floatValue;
//                    }
//                    double max = lambda_all_values.Max();
//                    min_lambda = lambda_all_values.Min();
//                    range_lambda = Mathf.Abs((float)(max - min_lambda));
//                    ratio_lambda = ((1 / (range_lambda)) * height);

//                    Vector3[] all_values_vector = new Vector3[lambda_all_values.Length];
//                    for (int i = 0; i < all_values_vector.Length; i++)
//                    {
//                        all_values_vector[i] = new Vector3(i, (float)(height - ((lambda_all_values[i] - min_lambda) / range_lambda) * height));
//                        //Debug.Log(all_values_vector[i]);
//                    }
//                    Handles.color = Color.blue;
//                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);

//                }

//                //Gitter
//                for (int w = 0; w < width; w++)
//                {
//                    if (w % grid_width == 0)
//                    {
//                        Vector3[] vline = new Vector3[2];
//                        vline[0] = new Vector3(w, 0);
//                        vline[1] = new Vector3(w, height);

//                        Handles.color = Color.grey;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);
//                    }
//                    if (w == marker_index.intValue)
//                    {
//                        Vector3[] vline = new Vector3[2];
//                        vline[0] = new Vector3(w, 0);
//                        vline[1] = new Vector3(w, height);

//                        Handles.color = Color.green;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, vline);

//                    }
//                }

//                int h_0 = ((int)Mathf.Round((float)(((0 + (1 - (min_lambda % 1))) / range_lambda) * height)));

//                int h_1 = ((int)Mathf.Round((float)((grid_height / range_lambda) * height)));


//                for (int h = 1; h < height; h++)
//                {
//                    if ((h - h_0) % h_1 == 0)
//                    {
//                        Vector3[] hline = new Vector3[2];
//                        hline[0] = new Vector3(0, height - h);
//                        hline[1] = new Vector3(width, height - h);

//                        Handles.color = Color.grey;
//                        Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, hline);
//                    }
//                }

//                GUI.EndClip();

//            }
//        }
//    }
//}
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
                    all_values_vector[i] = new Vector3(i * ratio_x,(float) (height - ((sigma_all_values[i] - min) / range) * height));
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
                for (int i = 0; i < mu_all_values.Length; i++)
                {
                    mu_all_values[i] = mu_values.GetArrayElementAtIndex(i).floatValue;
                }
                float max_mu = mu_all_values.Max();
                float min_mu = mu_all_values.Min();
                float range_mu = Mathf.Abs(max_mu - min_mu);


                stim_all_values = new float[stim_values.arraySize];
                for (int i = 0; i < stim_all_values.Length; i++)
                {
                    stim_all_values[i] = stim_values.GetArrayElementAtIndex(i).floatValue;
                }

                float max_stim = stim_all_values.Max();
                float min_stim = stim_all_values.Min();

                min_both = Mathf.Min(min_mu, min_stim);
                float max_both = Mathf.Max(max_mu, max_stim);
                range_both = Mathf.Abs(max_both - min_both);
                float ratio_both = ((1 / (range_both)) * height);

                Vector3[] all_values_vector = new Vector3[mu_all_values.Length];
                for (int i = 0; i < all_values_vector.Length; i++)
                {
                    all_values_vector[i] = new Vector3(i * ratio_x, height - ((mu_all_values[i] - min_both) / range_both) * height);

                    //Debug.Log(all_values_vector[i]);
                }
                Handles.color = Color.blue;
                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1, all_values_vector);


                all_values_vector = new Vector3[mu_all_values.Length];
                for (int i = 0; i < all_values_vector.Length; i++)
                {
                    all_values_vector[i] = new Vector3(i * ratio_x, height - ((stim_all_values[i] - min_both) / range_both) * height);
                }
                Handles.color = Color.yellow;
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


