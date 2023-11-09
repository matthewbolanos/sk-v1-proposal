"""Class to hold chat messages."""
from typing import Optional

from semantic_kernel.connectors.ai.open_ai.models.chat.function_call import (
    FunctionCall,
)

from ....models.chat.chat_message import ChatMessage


class OpenAIChatMessage(ChatMessage):
    """Class to hold openai chat messages, which might include name and function_call fields."""

    name: Optional[str] = None
    function_call: Optional[FunctionCall] = None
    tool_calls: Optional[list[FunctionCall]] = None

    @property
    def Name(self) -> str:
        return self.name

    @property
    def FunctionCall(self) -> FunctionCall:
        return self.function_call

    @property
    def ToolCalls(self) -> list[FunctionCall]:
        return self.tool_calls
