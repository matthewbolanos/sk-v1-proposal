// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Handlebars;


public class Math
{

    public Math()
    {
    }

    [SKFunction, Description("Uses functions from the Math plugin to solve math problems.")]
    public async Task<string> PerformMath(
        IKernel kernel,
        [Description("A description of a math problem; use the GenerateMathProblem function to create one.")] string math_problem
    )
    {
        // Create a plan
        var planner = new HandlebarsPlanner(kernel, new HandlebarsPlannerConfiguration(){
            IncludedPlugins = new () { "Math" },
            ExcludedFunctions = new () { "Math.PerformMath", "Math.GenerateMathProblem" }
        });
        var plan = await planner.CreatePlanAsync("Solve the following math problem.\n\n" + math_problem);

        // Run the plan
        var result = await plan.InvokeAsync(kernel, kernel.CreateNewContext(), new Dictionary<string, object>());
        return result.GetValue<string>()!;
    }

    [SKFunction, Description("Add two numbers. For example {{Math_Add number1=1 number2=2}} will return 3.")]
    public double Add(
        [Description("The first number to add")] double number1,
        [Description("The second number to add")] double number2
    )
    {
        return number1 + number2;
    }

    [SKFunction, Description("Subtract two numbers. For example {{Math_Subtract minuend=5 subtrahend=2}} will return 3.")]
    public double Subtract(
        [Description("The minuend")] double minuend,
        [Description("The subtrahend")] double subtrahend
    )
    {
        return minuend - subtrahend;
    }

    [SKFunction, Description("Multiply two numbers. For example {{Math_Multiply number1=5 number2=2}} will return 10.")]
    public double Multiply(
        [Description("The first number to multiply")] double number1,
        [Description("The second number to multiply")] double number2
    )
    {
        return number1 * number2;
    }

    [SKFunction, Description("Divide two numbers. For example {{Math_Divide dividend=10 divisor=2}} will return 5.")]
    public double Divide(
        [Description("The dividend")] double dividend,
        [Description("The divisor")] double divisor
    )
    {
        return dividend / divisor;
    }

    [SKFunction, Description("Get the remainder of two numbers. For example {{Math_Modulo dividend=10 divisor=3}} will return 1.")]
    public double Modulo(
        [Description("The dividend")] double dividend,
        [Description("The divisor")] double divisor
    )
    {
        return dividend % divisor;
    }

    [SKFunction, Description("Get the absolute value of a number. For example {{Math_Abs number=-5}} will return 5.")]
    public double Abs(
        [Description("The number")] double number
    )
    {
        return System.Math.Abs(number);
    }

    [SKFunction, Description("Get the ceiling of a number. For example {{Math_Ceiling number=5.1}} will return 6.")]
    public double Ceiling(
        [Description("The number")] double number
    )
    {
        return System.Math.Ceiling(number);
    }

    [SKFunction, Description("Get the floor of a number. For example {{Math_Floor number=5.9}} will return 5.")]
    public double Floor(
        [Description("The number")] double number
    )
    {
        return System.Math.Floor(number);
    }

    [SKFunction, Description("Get the maximum of two numbers. For example {{Math_Max number1=5 number2=10}} will return 10.")]
    public double Max(
        [Description("The first number")] double number1,
        [Description("The second number")] double number2
    )
    {
        return System.Math.Max(number1, number2);
    }

    [SKFunction, Description("Get the minimum of two numbers. For example {{Math_Min number1=5 number2=10}} will return 5.")]
    public double Min(
        [Description("The first number")] double number1,
        [Description("The second number")] double number2
    )
    {
        return System.Math.Min(number1, number2);
    }

    [SKFunction, Description("Get the sign of a number. For example {{Math_Sign number=-5}} will return -1.")]
    public double Sign(
        [Description("The number")] double number
    )
    {
        return System.Math.Sign(number);
    }

    [SKFunction, Description("Get the square root of a number. For example {{Math_Sqrt number=25}} will return 5.")]
    public double Sqrt(
        [Description("The number")] double number
    )
    {
        return System.Math.Sqrt(number);
    }

    [SKFunction, Description("Get the sine of a number. For example {{Math_Sin number=0}} will return 0.")]
    public double Sin(
        [Description("The number")] double number
    )
    {
        return System.Math.Sin(number);
    }

    [SKFunction, Description("Get the cosine of a number. For example {{Math_Cos number=0}} will return 1.")]
    public double Cos(
        [Description("The number")] double number
    )
    {
        return System.Math.Cos(number);
    }

    [SKFunction, Description("Get the tangent of a number. For example {{Math_Tan number=0}} will return 0.")]
    public double Tan(
        [Description("The number")] double number
    )
    {
        return System.Math.Tan(number);
    }

    [SKFunction, Description("Raise a number to a power. For example {{Math_Pow number1=5 number2=2}} will return 25.")]
    public double Pow(
        [Description("The number")] double number1,
        [Description("The power")] double number2
    )
    {
        return System.Math.Pow(number1, number2);
    }

    [SKFunction, Description("Get a rounded number. For example {{Math_Round number=5.5}} will return 6.")]
    public double Round(
        [Description("The number")] double number
    )
    {
        return System.Math.Round(number);
    }
}
