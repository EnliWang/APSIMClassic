﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CSGeneral;

/// <summary>
/// Returns the value of a nominated external APSIM numerical variable.
/// Note: This should be merged with the variable function when naming convention
/// to refer to internal and external variable is standardized. FIXME
/// </summary>

public class ExternalVariable : Function
{
    [Param]
    private string VariableName = "";

    [Link]
    ModelEnvironment ModelEnvironment = null;

    [Output]
    public override double Value
    {
        get
        {
            double val;

            if (ModelEnvironment.Get(VariableName, out val))
                 return Convert.ToDouble(val);
            else 
                 throw new Exception(Name + ": External value for " + VariableName.Trim() + " not found");
        }
    }

}
