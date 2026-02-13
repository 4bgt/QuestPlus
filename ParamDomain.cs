using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public ParamDomain(float mu, float mu_end, int mu_steps, float sigma = 1, float sigma_end = 1, int sigma_steps = 1, float gamma = 0, float gamma_end = 0, int gamma_steps = 1, float lambda = 0, float lambda_end = 0, int lambda_steps = 1) 
    {
        (this.mu_start, this.mu_end, this.mu_steps, this.mu) = FillArray(mu, mu_end, mu_steps);

        (this.sigma_start, this.sigma_end, this.sigma_steps, this.sigma) = FillArray(sigma, sigma_end, sigma_steps);

        (this.gamma_start, this.gamma_end, this.gamma_steps, this.gamma) = FillArray(gamma, gamma_end, gamma_steps);

        (this.lambda_start, this.lambda_end, this.lambda_steps, this.lambda) = FillArray(lambda, lambda_end, lambda_steps);

        this.saturation = new double[0] { };
        this.Length = this.mu.Length * this.sigma.Length  * this.gamma.Length * this.lambda.Length;
    }

    public ParamDomain(float mu, float mu_end, int mu_steps, float sigma = 1, float sigma_end = 1, int sigma_steps = 1, float saturation = 1, float saturation_end = 0, int saturation_steps = 1)
    {
        (this.mu_start, this.mu_end, this.mu_steps, this.mu) = FillArray(mu, mu_end, mu_steps);

        (this.sigma_start, this.sigma_end, this.sigma_steps, this.sigma) = FillArray(sigma, sigma_end, sigma_steps);

        (this.saturation_start, this.saturation_end, this.saturation_steps, this.saturation) = FillArray(saturation, saturation_end, saturation_steps);

        this.gamma = new double[0] {  };
        this.lambda = new double[0] { };
        this.Length = this.mu.Length * this.sigma.Length * this.saturation.Length;
    }

    public (float, float, int, double[]) FillArray(float start, float end, int steps)
    {
        double[] res = new double[steps];
        float step_size = 0;

        if (steps == 1)
        {
            step_size = 1;
        }
        else
        {
            step_size = (end - start) / (steps - 1);
            if (step_size < 0)
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
}