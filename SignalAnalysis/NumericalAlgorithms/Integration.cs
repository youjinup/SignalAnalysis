﻿using System.Numerics;

namespace SignalAnalysis;

public enum IntegrationMethod
{
    LeftPointRule,
    MidPointRule,
    RightPointRule,
    TrapezoidRule,
    SimpsonRule3,
    SimpsonRule8,
    SimpsonComposite,
    Romberg
}

public class Integration<T> where T : INumber<T>
{
    private IFunction<T> Function { get; set; }
    private IntegrationMethod Method { get; set; } = IntegrationMethod.TrapezoidRule;
    public double Step { get; set; } = 10e-6;
    /// <summary>
    /// True if the absolute value of the integral is computed. False if negative area values are considered
    /// </summary>
    public bool AbsoluteValue { get; set; } = true;

    public Integration(IFunction<T> func, double step, IntegrationMethod method = IntegrationMethod.TrapezoidRule, bool absolute = true)
    {
        this.Function = func;
        this.Method = method;
        this.Step = step;
        this.AbsoluteValue = absolute;
    }

    public double this[T arg] => Method switch
    {
        IntegrationMethod.MidPointRule => MidPointRule(arg),
        IntegrationMethod.TrapezoidRule => TrapezoidRule(arg),
        IntegrationMethod.SimpsonRule3 => SimpsonRule3(arg),
        IntegrationMethod.SimpsonRule8 => SimpsonRule8(arg),
        IntegrationMethod.SimpsonComposite => SimpsonComposite(arg),
        _ => throw new ArgumentOutOfRangeException(nameof(Method), $"Not expected integration method: {Method}"),
    };

    public double Integrate(Func<double, double> function, double lowerLimit = 0, double upperLimit = 1)
    {
        double result = -1;

        switch (Method)
        {
            case IntegrationMethod.MidPointRule:
                ;
                break;
            case IntegrationMethod.TrapezoidRule:
                ;
                break;
            case IntegrationMethod.SimpsonRule3:
                ;
                break;
            case IntegrationMethod.SimpsonRule8:
                ;
                break;
            case IntegrationMethod.SimpsonComposite:
                ;
                break;
            case IntegrationMethod.Romberg:
                result = Romberg(function, lowerLimit, upperLimit);
                break;
        }

        return result;
    }

    public double Integrate(double[] array, double lowerIndex = 0, double upperIndex = 1, double samplingFrequency = 1)
    {
        //upperIndex -= upperIndex - lowerIndex % 2;

        //return Integrate(Function, lowerIndex/samplingFrequency, upperIndex/samplingFrequency);

        double result = Integrate(Function, lowerIndex, upperIndex) / samplingFrequency;
        return result;

        double Function(double index)
        {
            return array[(int)(index)];
        }
    }


    public double IntegrateArray(double[] array)
    {
        int indexStart = 0, indexEnd = array.Length - 1, loopIncrement = 1;
        double result = 0;

        switch (Method)
        {
            case IntegrationMethod.MidPointRule:
                indexStart = 1;
                indexEnd = array.Length;
                break;
            case IntegrationMethod.TrapezoidRule:
                indexStart = 0;
                indexEnd = array.Length - 1;
                break;
            case IntegrationMethod.SimpsonRule3:
                indexStart = 1;
                indexEnd = array.Length - 1;
                loopIncrement = 2;
                break;
            case IntegrationMethod.SimpsonRule8:
                indexStart = 3;
                indexEnd = array.Length - 1;
                loopIncrement = 3;
                break;
            case IntegrationMethod.SimpsonComposite:
                indexStart = 3;
                indexEnd = array.Length - 1;
                loopIncrement = 3;
                break;
            case IntegrationMethod.Romberg:
                //Romberg(array, 0, array.Length - 1);
                break;
        }

        //for (int i = indexStart; i < indexEnd; i += loopIncrement)
        //    result += this[T.CreateChecked(i)];

        return result;
    }

    private double MidPointRule(T arg)
    {
        //return  (func[arg - h] + func[arg]) / (h);
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Int32 => Step * (Function[arg - T.CreateChecked(1)] + Function[arg]) / (double)2,
            _ => Step * (Function[arg - T.CreateChecked(Step)] - Function[arg]) / (double)2
        };
    }

    private double TrapezoidRule(T arg)
    {
        //return  (func[arg - h] + func[arg]) / (h);
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Int32 => Step * (Function[arg - T.CreateChecked(1)] + Function[arg]) / (double)2,
            _ => Step * (Function[arg - T.CreateChecked(Step)] - Function[arg]) / (double)2
        };
    }

    private double SimpsonRule3(T arg)
    {
        //return  (func[arg + h] - func[arg - h]) / (h * 2);
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Int32 => Step * (Function[arg - T.CreateChecked(1)] + 4 * Function[arg] + Function[arg + T.CreateChecked(1)]) / (double)3,
            _ => Step * (Function[arg - T.CreateChecked(Step)] + 4 * Function[arg] + Function[arg + T.CreateChecked(Step)]) / (double)3
        };
    }

    // https://en.wikipedia.org/wiki/Simpson%27s_rule
    private double SimpsonRule8(T arg)
    {
        //return  (func[arg + h] - func[arg - h]) / (h * 2);
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Int32 => Step * 3* (Function[arg - T.CreateChecked(3)] + 3 * Function[arg - T.CreateChecked(2)] + 3 * Function[arg - T.CreateChecked(1)] + Function[arg]) / (double)8,
            _ => Step * 3 * (Function[arg - T.CreateChecked(Step)] + 3 * Function[arg - T.CreateChecked(Step)] + 3 * Function[arg - T.CreateChecked(Step)] + Function[arg]) / (double)8
        };
    }

    // https://en.wikipedia.org/wiki/Simpson%27s_rule
    // http://mathforcollege.com/nm/mws/gen/07int/mws_gen_int_txt_simpson3by8.pdf
    private double SimpsonComposite(T arg)
    {
        //return  (func[arg + h] - func[arg - h]) / (h * 2);
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Int32 => Step * (Function[arg - T.CreateChecked(3)] + 3 * Function[arg - T.CreateChecked(2)] + 3 * Function[arg - T.CreateChecked(1)] + Function[arg]) / (double)48,
            _ => Step * (Function[arg - T.CreateChecked(Step)] + 3 * Function[arg - T.CreateChecked(Step)] + 3 * Function[arg - T.CreateChecked(Step)] + Function[arg]) / (double)48
        };
    }


    /// <summary>
    /// https://en.wikipedia.org/wiki/Romberg%27s_method
    /// </summary>
    /// <param name=""></param>
    /// <param name=""></param>
    /// <param name="">pointer to the function to be integrated</param>
    /// <param name="lowerLimit">lower limit</param>
    /// <param name="upperLimit">upper limit</param>
    /// <param name="maxSteps">maximum steps of the procedure</param>
    /// <param name="epsilon">desired accuracy</param>
    /// <returns>Approximate value of the integral of the function f for x in [a,b] with accuracy 'acc' and steps 'max_steps'</returns>
    private double Romberg(Func<double, double> function, double lowerLimit, double upperLimit, int maxSteps = 10, double epsilon = 1E-3)
    {
        double[] R1 = new double[maxSteps]; // buffer previous row
        double[] R2 = new double[maxSteps];   // buffer current row
        double h = upperLimit - lowerLimit; //step size
    
        // First trapezoidal step
        R1[0] = (function(lowerLimit) + function(upperLimit)) * h * 0.5;

        for (int i = 1; i < maxSteps; ++i)
        {
            h /= 2;
            double c = 0;
            int ep = 1 << (i - 1); //2^(n-1)
            for (int j = 1; j <= ep; ++j)
                c += function(lowerLimit + (2 * j - 1) * h);
            R2[0] = h * c + .5 * R1[0]; // R(i,0)

            for (int j = 1; j <= i; ++j)
            {
                double n_k = Math.Pow(4, j);
                R2[j] = (n_k * R2[j - 1] - R1[j - 1]) / (n_k - 1); // compute R(i,j)
            }

            if (i > 1 && Math.Abs(R1[i - 1] - R2[i]) < epsilon)
                return R2[i];

            // swap Rn and Rc as we only need the last row
            Array.Copy(R2, R1, i);
            Array.Clear(R2, 0, i+1);
        }
        return R1[maxSteps - 1]; // return our best guess
    }

}
