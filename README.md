# QuestPlus
This repository contains an implementation of the Quest+ Algorithm.
It is basically a re-implementation of this Matlab repo:
https://github.com/petejonze/QuestPlus
to be able to use Quest+ in Unity for psychphysical VR experiments.



# Quick Start & Requirements
1) Download the repository and import it into your Unity project.
2) Install nuget unitypackage either from this repo or from: https://github.com/GlitchEnzo/NuGetForUnity/releases
3) Restart Unity
4) In the menu go to nuget -> manage nuget packages and install the following packages: MathNet.Numerics
5) Use the Simulator Prefab or attach Simulator.cs to an empty GameObject in your scene.
6) Run the scene and observe the results in the console and the interactive plots in the editor. The Simulation is run in the Start function, so don't worry if Unity seems to not respond when you chose a lot of parameters. It will respond again as soon as the simulation is done. 
7) Modify the parameters of the Simulator.cs script to test different scenarios, such as changing the true parameters of the simulated participant, or changing the parameter domain.
8) Once you are comfortable with the algorithm, you can modify the Simulator.cs script to use real participant responses instead of simulated ones, and run your experiment.


# How to use it
Build a new Properties object
      
    Properties QP = new Properties(stimulusDomain, parameterDomain, responseDomain, stopRule, stopCriterion, minNTrials, maxNTrials)
Initialize it
    
    QP.Init()
Get current stimulus value

    QP.getTargetStim().value
Insert the response to this stimulus (bool) and go back to previous step to get the next stimulus

    bool _isFinished = QP.UpdateEverything(response)
If finished, save results

    QP.History(fileName, path)
Reset Paramdomain for new QuestRun

    ResetParamDomain()

## Simulator
The Simulator class is hopefully a useful example class to understand how to use this package.
It contains all the necessary functions to run the algorithm, such as updating the parameter domain, calculating the next stimulus, and updating the posterior distribution based on the participant's response.
At the same time it provides a simulation of a participants responses based on a predefined psychometric function, so you can test the algorithm without needing a real participant.
This can be very helpful if you are new to the Quest+ Algorithm and want to understand how it works, or if you want to test your implementation before running a real experiment.

If you inspect this script in the unity editor, you can also see interactive plots of the currently estimated parameters.
### Simulator.cs
All variables that contain "true" in their name are the predefined parameters of the simulated participant, that are used to generate the response (e.g.true_mu, true_sigma, true_gamma, true_lambda).


## ParamDomain
The Quest+ Algorithm needs a parameter domain to work. 
This is a list of all possible combinations of parameters that the algorithm can choose from. 
For example, if you have three parameters, each with three possible values, your parameter domain would consist of 9 combinations (3x3).

### ParamDomain.cs
This is a script that gives you a structure to define your parameter domain.
The current Version is based on a psychometric function with 3 or 4 parameters:
Mu: the mean of the psychometric function, which represents the point of subjective equality (PSE), that can be shifted to the left or right
Sigma: the standard deviation of the psychometric function, which represents the slope of the function, that can be steeper or flatter
Gamma: the guess rate, which represents the probability of a correct response when the stimulus is at the lowest level of the parameter domain, that can be higher or lower
Lambda: the lapse rate, which represents the probability of an incorrect response when the stimulus is at the highest level of the parameter
We also implemented a three parameter version in which saturation represents one parameter for both gamma and lambda.

If you want to estimate less parameters you can fix them by only defining one value for them in the constructor, and setting the number of steps to 1.

The script contains three types of constructors: 
    
    public ParamDomain(float mu, float mu_end, int mu_steps, float sigma = 1, float sigma_end = 1, int sigma_steps = 1, float gamma = 0, float gamma_end = 0, int gamma_steps = 1, float lambda = 0, float lambda_end = 0, int lambda_steps = 1) 
This constructor allows you to define a parameter domain by specifying the start and end values for each parameter, as well as the number of steps for each parameter.

    public ParamDomain(float mu, float mu_end, int mu_steps, float sigma = 1, float sigma_end = 1, int sigma_steps = 1, float saturation = 1, float saturation_end = 0, int saturation_steps = 1)
This constructor allows you to define a parameter domain by specifying the start and end values for each parameter, as well as the number of steps for each parameter, but with saturation instead of gamma and lambda.

    public ParamDomain()
This is a default constructor for an empty Paramdomain object, which can be filled later.

Helper functions:

    public (float, float, int, double[]) FillArray(float start, float end, int steps)
is a helper function that fills an array with values from start to end, with a specified number of steps.

##Target Stimulus
This class defines the currently used target stimulus. Each Stimulus has a value (that defines for example the brightness of the stimulus, if you are estimating the perception threshold) and an index (that describes where this stimulus value is in the pre-defined parameter domain).

### TargetStimulus.cs
There are two constructors for this class:

    public TargetStimulus(float value, int index)


    public TargetStimulus(float value, float[] stimDomain) 
This one automatically finds the index of the stimulus in the given stimDomain array (its just a forward loop, no fancy search algorithm)

## Properties

