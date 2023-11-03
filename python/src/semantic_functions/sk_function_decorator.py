# Copyright (c) Microsoft. All rights reserved.


def sk_function(
    *,
    description: str = "",
    name: str = "",
):
    """
    Decorator for SK functions.

    Args:
        description -- The description of the function
        name -- The name of the function
        input_description -- The description of the input
        input_default_value -- The default value of the input
    """

    def decorator(func):
        func.__sk_function__ = True
        func.__sk_function_description__ = description or ""
        func.__sk_function_name__ = name or func.__name__
        return func

    return decorator
