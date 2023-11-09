import math
from enum import Enum
from typing import TYPE_CHECKING

from python.src.planners.handlebars_planner import (
    HandleBarsPlanner,
    HandleBarsPlannerConfig,
)
from python.src.plugins import (
    sk_function,
    sk_function_parameter,
)

if TYPE_CHECKING:
    from python.src.kernel import NewKernel


class console_colors(str, Enum):
    OKBLUE = "\033[94m"
    OKGREEN = "\033[92m"
    FAIL = "\033[91m"
    ENDC = "\033[0m"


class Math:
    def __init__(self):
        self.kernel = None

    @sk_function(
        description="Uses functions from the Math plugin to solve math problems.",
        name="PerformMath",
    )
    @sk_function_parameter(
        name="kernel",
        description="The kernel to use for planning.",
        required=True,
        type="kernel",
    )
    @sk_function_parameter(
        name="math_problem",
        description="A description of a math problem; use the GenerateMathProblem function to create one.",
        required=True,
        type="string",
    )
    @sk_function_parameter(
        name="result",
        direction="output",
        description="The answer to the math problem.",
        type="number",
    )
    async def perform_math(self, variables, **kwargs):
        kernel: "NewKernel" = kwargs.get("kernel")
        assert kernel
        math_problem: str = variables["math_problem"]
        max_tries = 1
        last_plan = None
        last_error = None
        # TODO: new config that also has include plugin
        while max_tries >= 0:
            config = HandleBarsPlannerConfig(
                excluded_plugins=["Intent"],
                excluded_functions=[
                    "Math_GenerateMathProblem",
                    "Math_PerformMath",
                ],
                last_plan=last_plan,
                last_error=last_error,
            )
            planner = HandleBarsPlanner(kernel=kernel, configuration=config)
            plan = await planner.create_plan(
                goal=f"Solve the following math problem: {math_problem}"
            )
            last_plan = str(plan)
            print(f"{console_colors.OKBLUE}[Plan]: {plan}\n{console_colors.ENDC}")
            try:
                result = await kernel.run_async(plan, variables, **kwargs)
                # result = await plan.run_async(variables)
                print(
                    f"{console_colors.OKGREEN}[Result]: {result['result']}\n{console_colors.ENDC}"
                )
                return str(result["result"])
            except Exception as exc:
                print(f"{console_colors.FAIL}[Error]: {exc}\n{console_colors.ENDC}")
                last_error = str(exc)
                max_tries -= 1

    def _get_variables(self, args, kwargs, function):
        if "variables" in kwargs and kwargs["variables"]:
            return kwargs["variables"]
        variables = {}
        idx = 0
        for parameter in function.__sk_function_context_parameters__:
            if parameter["direction"] == "output":
                continue
            if parameter["name"] in kwargs:
                variables[parameter["name"]] = kwargs[parameter["name"]]
            elif args:
                variables[parameter["name"]] = args[idx]
            if (
                variables[parameter["name"]] is None
                and parameter["default_value"] is not None
            ):
                variables[parameter["name"]] = parameter["default_value"]
            idx += 1
        return variables

    @sk_function(
        description="Adds two numbers",
        name="Add",
    )
    @sk_function_parameter(
        name="number1",
        description="The first number to add",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The second number to add",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="sum",
        direction="output",
        description="The addition result",
        type="number",
        required=True,
    )
    def add(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.add)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return float(variables["number1"]) + float(variables["number2"])

    @sk_function(
        description="Subtracts two numbers",
        name="Subtract",
    )
    @sk_function_parameter(
        name="number1",
        description="The minuend",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The subtrahend",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="difference",
        direction="output",
        description="The difference between the minuend and subtrahend.",
        type="number",
        required=True,
    )
    def subtract(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.subtract)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return float(variables["number1"]) - float(variables["number2"])

    @sk_function(
        description="Multiplies two numbers",
        name="Multiply",
    )
    @sk_function_parameter(
        name="number1",
        description="The first number to multiply",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The second number to multiply",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="product",
        direction="output",
        description="The product of the numbers.",
        type="number",
        required=True,
    )
    def multiply(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.multiply)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return float(variables["number1"]) * float(variables["number2"])

    @sk_function(
        description="Divides two numbers",
        name="Divide",
    )
    @sk_function_parameter(
        name="number1",
        description="The dividend",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The divisor",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="quotient",
        direction="output",
        description="The quotient of the dividend and divisor.",
        type="number",
        required=True,
    )
    def divide(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.divide)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return float(variables["number1"]) / float(variables["number2"])

    @sk_function(
        description="Finds the remainder of two numbers",
        name="Modulo",
    )
    @sk_function_parameter(
        name="number1",
        description="The dividend",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The divisor",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="remainder",
        direction="output",
        description="The remainder of the dividend and divisor.",
        type="number",
        required=True,
    )
    def modulo(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.modulo)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return float(variables["number1"]) % float(variables["number2"])

    @sk_function(
        description="Gets the absolute value of a number",
        name="Absolute",
    )
    @sk_function_parameter(
        name="number",
        description="The number to get the absolute value of",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="absolute",
        direction="output",
        description="The absolute value of the number.",
        type="number",
        required=True,
    )
    def absolute(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.absolute)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return abs(float(variables["number"]))

    @sk_function(name="Ceil", description="Gets the ceiling of a single number.")
    @sk_function_parameter(
        name="number",
        description="The number to get the ceiling of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="ceiling",
        direction="output",
        description="The ceiling of the number.",
        type="number",
        required=True,
    )
    def ceil(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.ceil)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return math.ceil(float(variables["number"]))

    @sk_function(name="Floor", description="Gets the floor of a single number.")
    @sk_function_parameter(
        name="number",
        description="The number to get the floor of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="floor",
        direction="output",
        description="The floor of the number.",
        type="number",
        required=True,
    )
    def floor(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.floor)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return math.floor(float(variables["number"]))

    @sk_function(
        name="Max",
        description="Gets the maximum value of two numbers.",
    )
    @sk_function_parameter(
        name="number1",
        description="The first number to compare.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The second number to compare.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="max",
        direction="output",
        description="The maximum value of the two numbers.",
        type="number",
        required=True,
    )
    def max(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.max)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return max(float(variables["number1"]), float(variables["number2"]))

    @sk_function(
        name="Min",
        description="Gets the minimum value of two numbers.",
    )
    @sk_function_parameter(
        name="number1",
        description="The first number to compare.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The second number to compare.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="min",
        direction="output",
        description="The minimum value of the two numbers.",
        type="number",
        required=True,
    )
    def min(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.min)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return min(float(variables["number1"]), float(variables["number2"]))

    @sk_function(
        name="Sign",
        description="Gets the sign of a number.",
    )
    @sk_function_parameter(
        name="number",
        description="The number to get the sign of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="sign",
        direction="output",
        description="The sign of the number.",
        type="number",
        required=True,
    )
    def sign(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.sign)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return math.copysign(1.0, float(variables["number"]))

    @sk_function(
        name="Sqrt",
        description="Gets the square root of a number.",
    )
    @sk_function_parameter(
        name="number",
        description="The number to get the square root of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="sqrt",
        direction="output",
        description="The square root of the number.",
        type="number",
        required=True,
    )
    def sqrt(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.sqrt)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return math.sqrt(float(variables["number"]))

    @sk_function(
        name="Sin",
        description="Gets the sine of a number.",
    )
    @sk_function_parameter(
        name="number",
        description="The number to get the sine of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="sin",
        direction="output",
        description="The sine of the number.",
        type="number",
        required=True,
    )
    def sin(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.sin)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return math.sin(float(variables["number"]))

    @sk_function(
        name="Cos",
        description="Gets the cosine of a number.",
    )
    @sk_function_parameter(
        name="number",
        description="The number to get the cosine of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="cos",
        direction="output",
        description="The cosine of the number.",
        type="number",
        required=True,
    )
    def cos(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.cos)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return math.cos(float(variables["number"]))

    @sk_function(
        name="Tan",
        description="Gets the tangent of a number.",
    )
    @sk_function_parameter(
        name="number",
        description="The number to get the tangent of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="tan",
        direction="output",
        description="The tangent of the number.",
        type="number",
        required=True,
    )
    def tan(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.tan)
        if "number" not in variables:
            raise ValueError("Missing number in both args and kwargs")
        return math.tan(float(variables["number"]))

    @sk_function(
        name="Pow",
        description="Gets the power of a number.",
    )
    @sk_function_parameter(
        name="number",
        description="The number to get the power of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="power",
        description="The power of the number.",
        type="number",
        required=True,
    )
    @sk_function_parameter(
        name="pow",
        direction="output",
        description="The power of the number.",
        type="number",
        required=True,
    )
    def pow(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.pow)
        if "number" not in variables or "power" not in variables:
            raise ValueError("Missing number or power in both args and kwargs")
        return math.pow(float(variables["number"]), float(variables["power"]))

    @sk_function(
        name="Log",
        description="Gets the logarithm of a number.",
    )
    @sk_function_parameter(
        name="number1",
        description="The number to get the logarithm of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The base of the logarithm.",
        type="number",
        default_value=10,
    )
    @sk_function_parameter(
        name="log",
        direction="output",
        description="The logarithm of the number.",
        type="number",
        required=True,
    )
    def log(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.log)
        if "number1" not in variables and "number2" not in variables:
            raise ValueError("Missing number1 in both args and kwargs")
        return math.log(float(variables["number1"]), float(variables["number2"]))

    @sk_function(
        name="Round",
        description="Gets the rounded value of a number.",
    )
    @sk_function_parameter(
        name="number1",
        description="The number to get the rounded value of.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="number2",
        description="The number of digits to round to.",
        required=True,
        type="number",
    )
    @sk_function_parameter(
        name="round",
        direction="output",
        description="The rounded value of the number.",
        type="number",
        required=True,
    )
    def round(self, *args, **kwargs):
        variables = self._get_variables(args, kwargs, self.round)
        if "number1" not in variables or "number2" not in variables:
            raise ValueError("Missing number1 or number2 in both args and kwargs.")
        return round(float(variables["number1"]), int(variables["number2"]))
