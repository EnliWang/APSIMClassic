﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

/// <summary>
/// Calculate partitioning of daily growth based upon allometric relationship
/// </summary>

public class AllometricDemandFunction : Function
{
    [Link]
    Plant Plant = null;

    [Param]
    double Const = 0.0; 
    [Param]
    double Power = 0.0;
    [Param]
    string XProperty = null;
    [Param]
    string YProperty = null;


    [Output]
    public override double Value
    {
        get
        {
            double returnValue = 0.0;
            double XValue = 0.0;
            object Value = Plant.GetPlantVariable(XProperty);
            if (Value == null)
                throw new Exception("Could not find XProperty value for: " + Name);
            else if (Value is string)
                throw new Exception("Value for allometric XProperty needs to be a numeric value");
            else
                XValue = Convert.ToDouble(Value);

            double YValue = 0.0;
            Value = Plant.GetPlantVariable(YProperty);
            if (Value == null)
                throw new Exception("Could not find XProperty value for: " + Name);
            else if (Value is string)
                throw new Exception("Value for allometric YProperty needs to be a numeric value");
            else
                YValue = Convert.ToDouble(Value);

            double Target = Const * Math.Pow(XValue, Power);
            returnValue = Math.Max(0.0,Target - YValue);

            return returnValue;
        }
    }

}
