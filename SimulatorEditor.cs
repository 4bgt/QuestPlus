

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
        if (grid_height ==0)
        {
            grid_height =1;
        }
        grid_width = grid_width_s.intValue;
        height= height_s.intValue;

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
                    all_values_vector[i] = new Vector3(i, height- ((se_all_values[i] - min) / range) * height);
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
                range_mu = Mathf.Abs( max - min_mu);
                ratio_mu = ((1 / (range_mu) ) * height);

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

            int h_0 = (int)(Mathf.Round((((0 + (1 - (min_mu % 1))) / range_mu) * height)) );

            int h_1 = (int)(Mathf.Round(((grid_height / range_mu) * height)) );


            for (int h = 1; h < height; h++)
            {
                if ( (h-h_0) % h_1  ==0 )
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

