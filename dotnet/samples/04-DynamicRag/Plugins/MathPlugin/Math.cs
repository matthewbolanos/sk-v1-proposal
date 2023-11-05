// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Handlebars;


public class Math
{

    public Math()
    {
    }

    [SKFunction]
    [Description("Uses functions from the Math plugin to solve math problems.")]
    [SKOutputDescription("The answer to the math problem.")]
    [SKSample(
        inputs: "{\"math_problem\",\"If I started with $120 in the stock market, how much would I have after 10 years if the growth rate was 5%?\"}",
        output:"After 10 years, starting with $120, and with a growth rate of 5%, you would have $195.47 in the stock market."
    )]
    public static async Task<string> PerformMath(
        IKernel kernel,
        [Description("A description of a math problem; use the GenerateMathProblem function to create one.")] string math_problem
    )
    {
        int maxTries = 1;
        HandlebarsPlan? lastPlan = null;
        Exception? lastError = null;

        while (maxTries >= 0)
        {
            // Create the planner
            var planner = new HandlebarsPlanner(kernel, new HandlebarsPlannerConfiguration(){
                IncludedPlugins = new () { "Math" },
                ExcludedFunctions = new () { "Math.PerformMath", "Math.GenerateMathProblem" },
                LastPlan = lastPlan, // Pass in the last plan in case we want to try again
                LastError = lastError?.Message // Pass in the last error to avoid trying the same thing again
            });
            var plan = await planner.CreatePlanAsync("Solve the following math problem.\n\n" + math_problem);
            lastPlan = plan;


            // Console.ForegroundColor = ConsoleColor.Blue;
            // Console.WriteLine("[Plan]");
            // Console.ForegroundColor = ConsoleColor.DarkBlue;
            // Console.WriteLine(lastPlan.ToString().Trim());
            // Console.ResetColor();
            
            // Run the plan
            try {
                var result = await plan.InvokeAsync(kernel, new Dictionary<string, object?>());

                // Console.ForegroundColor = ConsoleColor.Green;
                // Console.WriteLine("[Result]");
                // Console.ForegroundColor = ConsoleColor.DarkGreen;
                // Console.WriteLine(result.ToString().Trim());
                // Console.ResetColor();

                return result.GetValue<string>()!;
            } catch (Exception e) {
                // If we get an error, try again
                lastError = e;

                // Console.ForegroundColor = ConsoleColor.Red;
                // Console.WriteLine("[Error]");
                // Console.ForegroundColor = ConsoleColor.DarkRed;
                // Console.WriteLine(e.Message.ToString().Trim());
                // Console.ResetColor();
            }
            maxTries--;
        }

        // If we tried too many times, throw an exception
        throw lastError!;
    }
    

    [SKFunction]
    [Description("Adds two numbers.")]
    [SKOutputDescription("The summation of the numbers.")]
    [SKSample(
        inputs: "{\"number1\":1, \"number2\":2}",
        output:"3"
    )]
    public static double Add(
        [Description("The first number to add")] double number1,
        [Description("The second number to add")] double number2
    )
    {
        return number1 + number2;
    }

    [SKFunction]
    [Description("Subtracts two numbers.")]
    [SKOutputDescription("The difference between the minuend and subtrahend.")]
    [SKSample(
        inputs: "{\"number1\":5, \"number2\":2}",
        output:"3"
    )]
    public static double Subtract(
        [Description("The minuend")] double number1,
        [Description("The subtrahend")] double number2
    )
    {
        return number1 - number2;
    }

    [SKFunction]
    [Description("Multiplies two numbers.")]
    [SKOutputDescription("The product of the numbers.")]
    [SKSample(
        inputs: "{\"number1\":5, \"number2\":2}",
        output:"10"
    )]
    public static double Multiply(
        [Description("The first number to multiply")] double number1,
        [Description("The second number to multiply")] double number2
    )
    {
        return number1 * number2;
    }

    [SKFunction]
    [Description("Divides two numbers.")]
    [SKOutputDescription("The quotient of the dividend and divisor.")]
    [SKSample(
        inputs: "{\"number1\":10, \"number2\":2}",
        output:"5"
    )]
    public static double Divide(
        [Description("The dividend")] double number1,
        [Description("The divisor")] double number2
    )
    {
        return number1 / number2;
    }

    [SKFunction]
    [Description("Gets the remainder of two numbers.")]
    [SKOutputDescription("The remainder of the dividend and divisor.")]
    [SKSample(
        inputs: "{\"number1\":10, \"number2\":3}",
        output:"1"
    )]
    public static double Modulo(
        [Description("The dividend")] double number1,
        [Description("The divisor")] double number2
    )
    {
        return number1 % number2;
    }

    [SKFunction]
    [Description("Gets the absolute value of a number.")]
    [SKOutputDescription("The absolute value of the number.")]
    [SKSample(
        inputs: "{\"number1\":-10}",
        output:"5"
    )]
    public static double Abs(
        [Description("The number")] double number1
    )
    {
        return System.Math.Abs(number1);
    }

    [SKFunction]
    [Description("Gets the ceiling of a single number.")]
    [SKOutputDescription("The ceiling of the number.")]
    [SKSample(
        inputs: "{\"number1\":5.1}",
        output:"6"
    )]
    public static double Ceil(
        [Description("The number")] double number1
    )
    {
        return System.Math.Ceiling(number1);
    }

    [SKFunction]
    [Description("Gets the floor of a single number.")]
    [SKOutputDescription("The floor of the number.")]
    [SKSample(
        inputs: "{\"number1\":5.9}",
        output:"5"
    )]
    public static double Floor(
        [Description("The number")] double number1
    )
    {
        return System.Math.Floor(number1);
    }

    [SKFunction]
    [Description("Gets the maximum of two numbers.")]
    [SKOutputDescription("The maximum of the two numbers.")]
    [SKSample(
        inputs: "{\"number1\":5, \"number2\":10}",
        output:"10"
    )]
    public static double Max(
        [Description("The first number")] double number1,
        [Description("The second number")] double number2
    )
    {
        return System.Math.Max(number1, number2);
    }

    [SKFunction]
    [Description("Gets the minimum of two numbers.")]
    [SKOutputDescription("The minimum of the two numbers.")]
    [SKSample(
        inputs: "{\"number1\":5, \"number2\":10}",
        output:"5"
    )]
    public static double Min(
        [Description("The first number")] double number1,
        [Description("The second number")] double number2
    )
    {
        return System.Math.Min(number1, number2);
    }

    [SKFunction]
    [Description("Gets the sign of a number.")]
    [SKOutputDescription("The sign of the number.")]
    [SKSample(
        inputs: "{\"number1\":-10}",
        output:"-1"
    )]
    public static double Sign(
        [Description("The number")] double number1
    )
    {
        return System.Math.Sign(number1);
    }

    [SKFunction]
    [Description("Gets the square root of a number.")]
    [SKOutputDescription("The square root of the number.")]
    [SKSample(
        inputs: "{\"number1\":25}",
        output:"5"
    )]
    public static double Sqrt(
        [Description("The number")] double number1
    )
    {
        return System.Math.Sqrt(number1);
    }

    [SKFunction]
    [Description("Gets the sine of a number.")]
    [SKOutputDescription("The sine of the number.")]
    [SKSample(
        inputs: "{\"number1\":0}",
        output:"0"
    )]
    public static double Sin(
        [Description("The number")] double number1
    )
    {
        return System.Math.Sin(number1);
    }

    [SKFunction]
    [Description("Gets the cosine of a number.")]
    [SKOutputDescription("The cosine of the number.")]
    [SKSample(
        inputs: "{\"number1\":0}",
        output:"1"
    )]
    public static double Cos(
        [Description("The number")] double number1
    )
    {
        return System.Math.Cos(number1);
    }

    [SKFunction]
    [Description("Gets the tangent of a number.")]
    [SKOutputDescription("The tangent of the number.")]
    [SKSample(
        inputs: "{\"number1\":0}",
        output:"0"
    )]
    public static double Tan(
        [Description("The number")] double number1
    )
    {
        return System.Math.Tan(number1);
    }

    [SKFunction]
    [Description("Raises a number to a power.")]
    [SKOutputDescription("The number raised to the power.")]
    [SKSample(
        inputs: "{\"number1\":5, \"number2\":2}",
        output:"25"
    )]
    public static double Pow(
        [Description("The number")] double number1,
        [Description("The power")] double number2
    )
    {
        return System.Math.Pow(number1, number2);
    }

    [SKFunction]
    [Description("Gets the natural logarithm of a number.")]
    [SKOutputDescription("The natural logarithm of the number.")]
    [SKSample(
        inputs: "{\"number1\":10, \"number2\":10}",
        output:"2.302585092994046"
    )]
    public static double Log(
        [Description("The number")] double number1,
        [Description("The base of the logarithm")] double number2 = 10
    )
    {
        return System.Math.Log(number1, number2);
    }

    [SKFunction]
    [Description("Gets a rounded number.")]
    [SKOutputDescription("The rounded number.")]
    [SKSample(
        inputs: "{\"number\":1.23456, \"digits\":2}",
        output:"1.23"
    )]
    public static double Round(
        [Description("The number")] double number1,
        [Description("The number of digits to round to")] int number2 = 0
    )
    {
        return System.Math.Round(number1, number2);
    }
}
