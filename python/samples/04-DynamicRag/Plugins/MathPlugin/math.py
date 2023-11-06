from python.src.plugins import (
    sk_function,
    sk_function_parameter,
)


class Math:
    def __init__(self):
        pass

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
