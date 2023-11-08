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
                result = await plan.run_async(variables)
                print(
                    f"{console_colors.OKGREEN}[Result]: {result}\n{console_colors.ENDC}"
                )
                return str(result)
            except Exception as exc:
                print(f"{console_colors.FAIL}[Error]: {exc}\n{console_colors.ENDC}")
                last_error = str(exc)
                max_tries -= 1

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
    def add(self, variables, **kwargs):
        return int(variables["number1"]) + int(variables["number2"])

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
    def subtract(self, variables, **kwargs):
        return int(variables["number1"]) - int(variables["number2"])

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
    def multiply(self, variables, **kwargs):
        return int(variables["number1"]) * int(variables["number2"])

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
    def divide(self, variables, **kwargs):
        return int(variables["number1"]) / int(variables["number2"])

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
    def modulo(self, variables, **kwargs):
        return int(variables["number1"]) % int(variables["number2"])

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
    def absolute(self, variables, **kwargs):
        return abs(int(variables["number"]))
