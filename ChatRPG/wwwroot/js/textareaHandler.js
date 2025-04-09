export function setupTextareaKeyHandler(textareaElement, dotNetHelper) {
  if (!textareaElement) return;

  textareaElement.addEventListener("keydown", function (e) {
    if ((e.code === "Enter" || e.code === "NumpadEnter") && !e.shiftKey) {
      e.preventDefault(); // Stop newline

      const currentValue = textareaElement.value;
      textareaElement.value = ""; // Clear the textarea after sending
      textareaElement.style.height = "auto"; // Reset height

      dotNetHelper.invokeMethodAsync("OnEnterPressed", currentValue);
    }
  });
}
