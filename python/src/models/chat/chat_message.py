"""Class to hold chat messages."""
from typing import Dict, Optional

from semantic_kernel.sk_pydantic import SKBaseModel


class ChatMessage(SKBaseModel):
    """Class to hold chat messages."""

    role: Optional[str] = "assistant"
    content: Optional[str] = None

    def as_dict(self) -> Dict[str, str]:
        """Return the message as a dict.
        Make sure to call render_message_async first to embed the context in the content.
        """
        return self.dict(exclude_none=True, include={"role", "content"})

    @property
    def Role(self) -> str:
        return self.role

    @property
    def Content(self) -> str:
        return self.content
