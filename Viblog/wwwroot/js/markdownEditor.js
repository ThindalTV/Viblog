/**
 * Inserts text at the current cursor position (or selection) inside the first
 * textarea found within `container`, then returns the full updated value so
 * Blazor can sync its own state without relying on a DOM event dispatch.
 *
 * @param {HTMLElement} container - The scoped element that contains the textarea.
 * @param {string} text - The markdown snippet to insert.
 * @returns {string|null} The new textarea value, or null if no textarea was found.
 */
export function insertAtCursor(container, text) {
    const textarea = container.querySelector('textarea');
    if (!textarea) return null;

    const start = textarea.selectionStart ?? textarea.value.length;
    const end = textarea.selectionEnd ?? textarea.value.length;

    textarea.value =
        textarea.value.substring(0, start) +
        text +
        textarea.value.substring(end);

    // Restore focus and place caret right after the inserted snippet
    textarea.selectionStart = textarea.selectionEnd = start + text.length;
    textarea.focus();

    return textarea.value;
}
